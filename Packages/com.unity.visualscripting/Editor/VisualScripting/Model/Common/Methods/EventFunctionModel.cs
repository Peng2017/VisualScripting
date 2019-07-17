using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    public class EventFunctionModel : FunctionModel, IEventFunctionModel
    {
        [SerializeField]
        TypeHandle m_EventType;

        public override bool IsInstanceMethod => true;

        public TypeHandle EventType
        {
            get => m_EventType;
            set
            {
                if (m_EventType == value)
                    return;

                m_EventType = value;

                MethodInfo methodInfo = m_EventType.Resolve(Stencil).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).LastOrDefault(n => n.Name == name);
                if (methodInfo != null)
                {
                    foreach (ParameterInfo paramInfo in methodInfo.GetParameters())
                    {
                        UpdateOrCreateFunctionParameterDeclaration(paramInfo.Name, paramInfo.ParameterType.GenerateTypeHandle(Stencil));
                    }
                }
            }
        }

        public override bool AllowMultipleInstances => false;

        public override IFunctionModel OwningFunctionModel => this;

        public override string IconTypeString => "typeEventFunction";
    }
}
