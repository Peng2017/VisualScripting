using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, "Variable/Set Variable")]
    public class SetVariableNodeModel : NodeModel
    {
        const string k_Title = "Set Variable";

        public override string Title => k_Title;

        protected override void OnDefineNode()
        {
            AddInstanceInput<Unknown>();
            AddDataInput<Unknown>("Value");
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Index == 0)
            {
                TypeHandle t = otherConnectedPortModel?.DataType ?? TypeHandle.Unknown;
                m_InputPortModels[0].DataType = t;
                m_InputPortModels[1].DataType = t;
            }
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            OnConnection(selfConnectedPortModel, null);
        }
    }
}
