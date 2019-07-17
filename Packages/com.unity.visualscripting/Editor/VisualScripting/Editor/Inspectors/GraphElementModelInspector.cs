using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(IGraphElementModel), true)]
    class GraphElementModelInspector : UnityEditor.Editor
    {
        protected virtual bool DoDefaultInspector => true;

        public sealed override void OnInspectorGUI()
        {
            var graph = (target as IGraphElementModel)?.GraphModel;

            if (DoDefaultInspector)
                base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            GraphElementInspectorGUI(() => RefreshMatchingWindows(graph));
            if (graph != null && (EditorGUI.EndChangeCheck()))
            {
                RefreshMatchingWindows(graph);
            }
        }

        protected virtual void GraphElementInspectorGUI(Action refreshUI)
        {
        }

        static void RefreshMatchingWindows(IGraphModel graph)
        {
            foreach (var window in VseWindow.GetWindowsWithGraph(graph))
            {
                window.RefreshUI(UpdateFlags.All);
            }
        }
    }
}
