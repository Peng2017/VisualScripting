using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(StackModel))]
    class StackModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;
    }

    [CustomEditor(typeof(LoopStackModel), true)]
    class LoopStackModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;
    }

    [CustomEditor(typeof(FunctionModel), true)]
    class FunctionModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            var inv = target as FunctionModel;
            if (inv == null)
                return;

            this.NameEditor(inv);

            var graph = (VSGraphModel)((IGraphElementModel)target)?.GraphModel;
            graph.Stencil.TypeEditor(inv.ReturnType, (theType, i) =>
            {
                inv.ReturnType = theType;

                // TODO: update return nodes
//                foreach (return nodes)
//                    return.UpdateTypeFromDeclaration();
                refreshUI();
            });

            if (graph != null)
            {
                inv.EnableProfiling = EditorGUILayout.Toggle("Enable Profiling", inv.EnableProfiling);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NodeModels"), true);
        }
    }
}
