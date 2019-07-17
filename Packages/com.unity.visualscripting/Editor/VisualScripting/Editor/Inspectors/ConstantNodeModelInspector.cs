using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(ConstantNodeModel), true)]
    class ConstantNodeModelInspector : NodeModelInspector
    {
        GUIContent m_GUIContent;
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            if (m_GUIContent == null)
                m_GUIContent = new GUIContent("value");

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            var graph = (target as IGraphElementModel)?.GraphModel;
            if (graph != null)
                ConstantEditorGUI(serializedObject, m_GUIContent, graph.Stencil, ConstantEditorMode.AllEditors, refreshUI);

            serializedObject.ApplyModifiedProperties();
        }

        public enum ConstantEditorMode { ValueOnly, AllEditors }

        public static void ConstantEditorGUI(SerializedObject o, GUIContent label, Stencil stencil,
            ConstantEditorMode mode = ConstantEditorMode.ValueOnly, Action onChange = null)
        {
            if (mode != ConstantEditorMode.ValueOnly)
            {
                var enumModel = o.targetObject as EnumConstantNodeModel;
                if (enumModel != null)
                {
                    var filter = new SearcherFilter(SearcherContext.Type).WithEnums(stencil);
                    stencil.TypeEditor(enumModel.value.EnumType,
                        (type, index) =>
                        {
                            enumModel.value.EnumType = type;
                            onChange?.Invoke();
                        }, filter);
                }
            }

            var constantNodeModel = o.targetObject as ConstantNodeModel;

            if (constantNodeModel != null && constantNodeModel.IsLocked)
                return;

            var enumConstModel = o.targetObject as EnumConstantNodeModel;
            if (enumConstModel != null)
            {
                enumConstModel.value.Value = Convert.ToInt32(EditorGUILayout.EnumPopup("Value", enumConstModel.EnumValue));
            }
            else
            {
                EditorGUILayout.PropertyField(o.FindProperty("value"), label, true);
            }
        }
    }
}
