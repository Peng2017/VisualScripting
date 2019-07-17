using System;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(GraphModel), true)]
    class GraphModelInspector : UnityEditor.Editor
    {
        ReorderableList m_ReorderableList;
        public override void OnInspectorGUI()
        {
            VSGraphModel graph = (VSGraphModel)target;

            EditorGUILayout.LabelField("Stencil Properties");

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            var stencilObject = new SerializedObject(graph.Stencil);
            foreach (var propertyName in graph.Stencil.PropertiesVisibleInGraphInspector())
                EditorGUILayout.PropertyField(stencilObject.FindProperty(propertyName));
            stencilObject.ApplyModifiedProperties();

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
                graph.Stencil.RecompilationRequested = true;

            if (graph.Stencil is IHasOrderedStacks orderedStencil)
            {
                if (m_ReorderableList == null)
                    m_ReorderableList = new ReorderableList(null, typeof(IOrderedStack))
                    {
                        displayAdd = false,
                        displayRemove = false,
                        drawHeaderCallback = rect => GUI.Label(rect, "Execution Order"),
                        drawElementCallback = (rect, index, active, focused) =>
                        {
                            var orderedStack = (IOrderedStack)m_ReorderableList.list[index];
                            GUI.Label(rect, orderedStack.Title);
                        },
                        onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                        {
                            for (int i = 0; i < m_ReorderableList.list.Count; i++)
                            {
                                var orderedStack = (IOrderedStack)m_ReorderableList.list[i];
                                orderedStack.Order = i;
                            }

                            graph.Stencil.RecompilationRequested = true;
                        }
                    };
                m_ReorderableList.list = RoslynTranslator.GetEntryPointStacks(graph).OfType<IOrderedStack>().OrderBy(x => x.Order).ToList();
                m_ReorderableList.DoLayoutList();
            }

            base.OnInspectorGUI();
        }
    }
}
