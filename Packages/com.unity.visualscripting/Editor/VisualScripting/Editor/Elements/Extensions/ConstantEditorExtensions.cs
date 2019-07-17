using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    namespace ConstantEditor
    {
        [PublicAPI]
        public static class VisualElementExtensions
        {
            public static VisualElement CreateEditorForNodeModel(this VisualElement element, IConstantNodeModel model, Action<IChangeEvent> onValueChanged)
            {
                VisualElement editorElement;
                if (model is EnumConstantNodeModel enumConstant)
                {
                    var enumEditor = new Button{text = enumConstant.EnumValue.ToString()}; // TODO use a bindable element
                    enumEditor.clickable.clickedWithEventInfo += e =>
                    {
                        SearcherService.ShowEnumValues("Pick a value", enumConstant.EnumType.Resolve(model.GraphModel.Stencil), e.originalMousePosition, (v,i) =>
                        {
                            enumConstant.value.Value = Convert.ToInt32(v);
                            enumEditor.text = v.ToString();
                            onValueChanged?.Invoke(null);
                        });
                    };
                    enumEditor.SetEnabled(!enumConstant.IsLocked);
                    editorElement = enumEditor;
                }
                else if (model is IStringWrapperConstantModel icm)
                {
                    var enumEditor = new Button{text = icm.ObjectValue.ToString()}; // TODO use a bindable element
                    enumEditor.clickable.clickedWithEventInfo += e =>
                    {
                        List<string> allInputNames = icm.GetAllInputNames();
                        SearcherService.ShowValues("Pick a value", allInputNames, e.originalMousePosition, (v, pickedIndex) =>
                        {
                            icm.SetValueFromString(v);
                            enumEditor.text = v;
                            onValueChanged?.Invoke(null);
                        });
                    };
                    enumEditor.SetEnabled(!icm.IsLocked);
                    editorElement = enumEditor;
                }
                else if (model as Object)
                {
                    var constantNodeModel = (ConstantNodeModel)model;
                    var serializedObject = new SerializedObject(constantNodeModel);

                    SerializedProperty serializedProperty = serializedObject.FindProperty("value");
                    var propertyField = new PropertyField(serializedProperty);

                    editorElement = propertyField;
                    editorElement.SetEnabled(!constantNodeModel.IsLocked);

                    // delayed because the initial binding would cause an event otherwise, and then a compilation
                    propertyField.schedule.Execute(() =>
                    {
                        var onValueChangedEventCallback = new EventCallback<IChangeEvent>(onValueChanged);

                        // HERE BE DRAGONS
                        // there's no way atm to be notified that a PropertyField's value changed so we build a ChangeEvent<T>
                        // callback registration using reflection, but actually provide an Action<IChangeEvent>
                        Type type = constantNodeModel.Type;
                        Type eventType = typeof(ChangeEvent<>).MakeGenericType(type);
                        MethodInfo genericRegisterCallbackMethod = typeof(VisualElement).GetMethods().Single(m =>
                        {
                            var parameterInfos = m.GetParameters();
                            return m.Name == nameof(VisualElement.RegisterCallback) && parameterInfos.Length == 2 && parameterInfos[1].ParameterType == typeof(TrickleDown);
                        });
                        MethodInfo registerCallbackMethod = genericRegisterCallbackMethod.MakeGenericMethod(eventType);
                        registerCallbackMethod.Invoke(propertyField, new object[] { onValueChangedEventCallback, TrickleDown.NoTrickleDown });
                        foreach (var floatField in propertyField.Query<FloatField>().ToList())
                        {
                            floatField.isDelayed = true;
                            floatField.RegisterValueChangedCallback(_ =>
                            {
                                onValueChangedEventCallback.Invoke(null);
                                floatField.UnregisterValueChangedCallback(onValueChangedEventCallback);
                            });
                        }
                    }).ExecuteLater(1);
                }
                else
                {
                    editorElement = new Label("<Unknown>");
                }

                return editorElement;
            }
        }
    }
}

