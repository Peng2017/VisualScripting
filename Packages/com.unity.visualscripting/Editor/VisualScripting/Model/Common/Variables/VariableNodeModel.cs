using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public class VariableNodeModel : NodeModel, IVariableModel, IRenamableModel, IObjectReference, IExposeTitleProperty
    {
        [SerializeField]
        VariableDeclarationModel m_DeclarationModel;

        public VariableType VariableType => DeclarationModel.VariableType;

        public TypeHandle DataType => DeclarationModel?.DataType ?? TypeHandle.Unknown;

        public override string Title => m_DeclarationModel.Title;

        public IVariableDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = (VariableDeclarationModel)value;
        }

        public Object ReferencedObject => m_DeclarationModel;
        public string TitlePropertyName => "m_Name";

        public void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null)
                m_OutputPortModels[0].DataType = DeclarationModel.DataType;

            // update connected nodes' ports colors/types
            foreach (IPortModel connectedPortModel in m_OutputPortModels[0].ConnectionPortModels)
                connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_OutputPortModels[0]);
        }

        protected override void OnDefineNode()
        {
            // used by macro outputs
            if(m_DeclarationModel != null /* this node */ && m_DeclarationModel.Modifiers.HasFlag(ModifierFlags.WriteOnly))
                AddDataInput(null, DataType);
            else
                AddDataOutputPort(null, DataType);
        }

        public void Rename(string newName)
        {
            ((VariableDeclarationModel)DeclarationModel)?.SetNameFromUserName(newName);
        }

        public override CapabilityFlags Capabilities => base.Capabilities | CapabilityFlags.Renamable;
    }
}
