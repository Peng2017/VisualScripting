using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public class FunctionRefCallNodeModel : NodeModel, IObjectReference, IExposeTitleProperty
    {
        public override string Title
        {
            get
            {
                if (m_Function)
                {
                    return (m_Function.GraphModel != GraphModel ? m_Function.GraphModel.Name + "." : string.Empty) +
                        m_Function.Title;
                }
                return "<unknown>";
            }
        }

        [SerializeField]
        FunctionModel m_Function;

        public Object ReferencedObject => m_Function;

        public string TitlePropertyName => "m_Name";

        public override IReadOnlyList<IPortModel> InputPortModels
        {
            get
            {
                DefineNode();
                return base.InputPortModels;
            }
        }

        public override IReadOnlyList<IPortModel> OutputPortModels
        {
            get
            {
                DefineNode();
                return base.OutputPortModels;
            }
        }

        public FunctionModel Function
        {
            get => m_Function;
            set => m_Function = value;
        }

        protected override void OnDefineNode()
        {
            UpdatePorts();
        }

        void UpdatePorts()
        {
            if (!m_Function)
            {
                return;
            }

            if (m_Function.IsInstanceMethod)
                AddInstanceInput(((VSGraphModel)m_Function.GraphModel).GenerateTypeHandle(Stencil));

            foreach (var parameter in m_Function.FunctionParameterModels)
                AddDataInput(parameter.Name, parameter.DataType);

            var v = typeof(void).GenerateTypeHandle(Stencil);

            if (m_Function.ReturnType != v)
                AddDataOutputPort("result", m_Function.ReturnType);
        }
    }
}
