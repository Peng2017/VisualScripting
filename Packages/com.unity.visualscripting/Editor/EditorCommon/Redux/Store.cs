using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;

namespace UnityEditor.EditorCommon.Redux
{
    public class Store<TState> : IStore<TState>, IDisposable
        where TState : IDisposable
    {
        readonly object m_SyncRoot = new object();
        protected readonly Dictionary<Type, object> m_Reducers = new Dictionary<Type, object>();
        readonly List<Action<IAction>> m_Observers = new List<Action<IAction>>();

        TState m_LastState;
        Action m_StateChanged;
        bool m_StateDirty;
        Task<TState> m_Task;
        CancellationTokenSource m_CancellationTokenSource;

        protected Store(TState initialState = default(TState))
        {
            m_LastState = initialState;
        }

        void DoRegister<TAction>(object reducer) where TAction : IAction
        {
            lock (m_SyncRoot)
            {
                Type actionType = typeof(TAction);

                if (m_Reducers.ContainsKey(actionType))
                    throw new InvalidOperationException("Redux: Cannot register two reducers for action " + actionType.Name);
                m_Reducers[actionType] = reducer;
            }
        }

        public void Register<TAction>(Reducer<TState, TAction> reducer) where TAction : IAction
        {
            DoRegister<TAction>(reducer);
        }

        public void RegisterAsync<TAction>(AsyncReducer<TState, TAction> reducer) where TAction : IAction
        {
            DoRegister<TAction>(reducer);
        }

        public void Unregister<TAction>() where TAction : IAction
        {
            lock (m_SyncRoot)
            {
                m_Reducers.Remove(typeof(TAction));
            }
        }

        public void Register(Action<IAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                    throw new InvalidOperationException("Redux: Cannot register the same observer twice.");
                m_Observers.Add(observer);
            }
        }

        public void Unregister(Action<IAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                {
                    m_Observers.Remove(observer);
                }
            }
        }

        public event Action StateChanged
        {
            add => m_StateChanged += value;
            // ReSharper disable once DelegateSubtraction
            remove => m_StateChanged -= value;
        }

        [Obsolete]
        public void DispatchDynamicSlow(IAction action)
        {
            lock (m_SyncRoot)
            {
                foreach (var observer in m_Observers)
                {
                    observer(action);
                }

                PreDispatchAction(action);

                Delegate reducer = (Delegate)m_Reducers[action.GetType()];
                m_LastState = (TState)reducer.DynamicInvoke(m_LastState, action);
            }

            m_StateDirty = true;
        }

        public virtual void Dispatch<TAction>(TAction action) where TAction : IAction
        {
            lock (m_SyncRoot)
            {
                if (m_Task != null)
                {
                    m_CancellationTokenSource?.Cancel();

                    if (m_Task.IsCanceled)
                    {
                        m_Task = null;
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot dispatch an action while a previous one is running");
                    }
                }

                foreach (Action<IAction> observer in m_Observers)
                {
                    observer(action);
                }

                PreDispatchAction(action);


                if (!m_Reducers.TryGetValue(action.GetType(), out var o))
                {
                    Debug.LogError($"No reducer for action type {action.GetType()}");
                    return;
                }
                if (o is Reducer<TState, TAction> reducer)
                {
                    m_LastState = reducer(m_LastState, action);
                }
                else
                {
                    if ((IAction)action is CancellableAction cancellableAction)
                        m_CancellationTokenSource = cancellableAction.CancellationTokenSource;

                    var asyncReducer = (AsyncReducer<TState, TAction>)o;
                    m_Task = asyncReducer(m_LastState, action);
                    return;
                }
            }

            m_StateDirty = true;
        }

        // Called once per frame
        public bool Update()
        {
            if (m_Task != null) // an async reducer is pending
            {
                if (m_Task.IsCompleted) // succeeded, cancelled or threw an exception
                {
                    if(m_Task.IsFaulted)
                        Debug.LogError(m_Task.Exception);
                    else if (!m_Task.IsCanceled)
                        m_LastState = m_Task.Result;

                    m_Task = null;
                    m_StateDirty = true;
                }
                else
                    return false;
            }

            if (m_StateDirty)
            {
                m_StateDirty = false;
                InvokeStateChanged();
            }

            return true;
        }

        public virtual void Dispose()
        {
            m_LastState?.Dispose();
            m_StateChanged = null;
        }

        protected void InvokeStateChanged()
        {
            PreStateChanged();
            m_StateChanged?.Invoke();
            PostStateChanged();
        }

        protected virtual void PreDispatchAction(IAction action)
        {
        }

        protected virtual void PreStateChanged()
        {
        }

        protected virtual void PostStateChanged()
        {
        }

        public TState GetState()
        {
            return m_LastState;
        }
    }
}
