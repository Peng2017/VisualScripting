using System;
using System.Threading.Tasks;

namespace UnityEditor.EditorCommon.Redux
{
    public delegate TState Reducer<TState, in TAction>(TState previousState, TAction action) where TAction : IAction;
    public delegate Task<TState> AsyncReducer<TState, in TAction>(TState previousState, TAction action) where TAction : IAction;
}
