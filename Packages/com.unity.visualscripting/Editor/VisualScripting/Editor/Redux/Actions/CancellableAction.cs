using System;
using System.Threading;
using UnityEditor.EditorCommon.Redux;

namespace UnityEditor.VisualScripting.Editor
{
    public abstract class CancellableAction : IAction
    {
        CancellationTokenSource m_Cts;

        internal CancellationTokenSource CancellationTokenSource => m_Cts ?? (m_Cts = new CancellationTokenSource());
    }
}
