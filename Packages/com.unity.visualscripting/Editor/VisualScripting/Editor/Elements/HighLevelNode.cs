using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public class HighLevelNode : Node
    {
        const string k_ScriptPropertyName = "m_Script";
        const string k_GeneratorAssetPropertyName = "m_GeneratorAsset";

        static readonly CustomStyleProperty<float> k_LabelWidth = new CustomStyleProperty<float>("--unity-hl-node-label-width");
        static readonly CustomStyleProperty<float> k_FieldWidth = new CustomStyleProperty<float>("--unity-hl-node-field-width");

        const float k_DefaultLabelWidth = 150;
        const float k_DefaultFieldWidth = 120;

        static readonly HashSet<string> k_ExcludedPropertyNames =
            new HashSet<string>(
                typeof(NodeModel)
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(f => f.Name)
            )
            {
                k_ScriptPropertyName,
                k_GeneratorAssetPropertyName
            };

        public HighLevelNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView) { }

        protected override void UpdateFromModel()
        {
            base.UpdateFromModel();

            AddToClassList("highLevelNode");

            VisualElement topHorizontalDivider = this.MandatoryQ("divider", "horizontal");
            VisualElement topVerticalDivider = this.MandatoryQ("divider", "vertical");

            // GraphView automatically hides divider since there are no input ports
            topHorizontalDivider.RemoveFromClassList("hidden");
            topVerticalDivider.RemoveFromClassList("hidden");

            VisualElement output = this.MandatoryQ("output");
            output.AddToClassList("node-controls");

            var imguiContainer = CreateControls();

            imguiContainer.AddToClassList("node-controls");
            mainContainer.MandatoryQ("top").Insert(1, imguiContainer);
        }

        protected virtual VisualElement CreateControls()
        {
            var obj = new SerializedObject((Object)model);
            return new IMGUIContainer(() =>
            {
                EditorGUIUtility.labelWidth = customStyle.TryGetValue(k_LabelWidth, out var labelWidth) ? labelWidth : k_DefaultLabelWidth;
                EditorGUIUtility.fieldWidth = customStyle.TryGetValue(k_FieldWidth, out var fieldWidth) ? fieldWidth : k_DefaultFieldWidth;
                DrawInspector(obj, RedefineNode);
            });
        }

        void DrawInspector(SerializedObject obj, Action onChangedCallback)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();
            var iterator = obj.GetIterator();

            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (k_ExcludedPropertyNames.Contains(iterator.name))
                    continue;

                var label = new GUIContent(iterator.displayName);
                var field = obj.targetObject.GetType().GetField(
                    iterator.propertyPath,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var typeSearcherAttribute = field?.GetCustomAttribute<TypeSearcherAttribute>();

                if (typeSearcherAttribute != null)
                    TypeReferencePicker(iterator, typeSearcherAttribute, label, onChangedCallback);
                else
                    EditorGUILayout.PropertyField(iterator, label, true);
            }

            obj.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                onChangedCallback();
        }

        void TypeReferencePicker(SerializedProperty iterator, TypeSearcherAttribute attribute, GUIContent label, Action onChangedCallback)
        {
            //Fetch typename
            var typeHandleIdProperty = iterator.FindPropertyRelative(nameof(TypeHandle.Identification));
            var typeHandleAssetRefProperty = iterator.FindPropertyRelative(nameof(TypeHandle.GraphModelReference));

            var handle = new TypeHandle(typeHandleAssetRefProperty.objectReferenceValue as VSGraphModel, typeHandleIdProperty.stringValue);
            var stencil = model.GraphModel.Stencil;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            var friendlyName = handle.GetMetadata(stencil).FriendlyName;

            if (GUILayout.Button(friendlyName))
            {
                var mousePosition = mainContainer.LocalToWorld(Event.current.mousePosition);
                void Callback(TypeHandle type, int index)
                {
                    Assert.IsNotNull(typeHandleIdProperty);
                    Assert.IsNotNull(typeHandleAssetRefProperty);
                    typeHandleIdProperty.stringValue = type.Identification;
                    typeHandleAssetRefProperty.objectReferenceValue = type.GraphModelReference;
                    iterator.serializedObject.ApplyModifiedProperties();
                    onChangedCallback();
                }

                SearcherService.ShowTypes(stencil, mousePosition, Callback, attribute.Filter?.GetFilter(model));
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
