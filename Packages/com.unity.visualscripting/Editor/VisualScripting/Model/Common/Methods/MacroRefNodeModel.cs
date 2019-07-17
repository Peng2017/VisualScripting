using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public class MacroRefNodeModel : NodeModel, IRenamableModel, IObjectReference, IExposeTitleProperty
    {
        public override string Title
        {
            get
            {
                if (m_Graph)
                {
                    return m_Graph.AssetModel.Name;
                }
                return "<unknown>";
            }
        }

        public override string IconTypeString => "typeMacro";

        [SerializeField]
        VSGraphModel m_Graph;

        public Dictionary<IPortModel, IVariableDeclarationModel> shadowPortModels;

        public VSGraphModel Macro
        {
            get => m_Graph;
            set => m_Graph = value;
        }

        public Object ReferencedObject => (Object)m_Graph.AssetModel;

        public string TitlePropertyName => "m_Name";

        bool m_PreventReentrant;

        public override IReadOnlyList<IPortModel> InputPortModels
        {
            get
            {
                RedefineNode();
                return base.InputPortModels;
            }
        }

        public override IReadOnlyList<IPortModel> OutputPortModels
        {
            get
            {
                RedefineNode();
                return base.OutputPortModels;
            }
        }

        void RedefineNode()
        {
            if (!m_PreventReentrant)
            {
                m_PreventReentrant = true;
                try
                {
                    DefineNode();
                }
                finally
                {
                    m_PreventReentrant = false;
                }
            }
        }

        protected override void OnDefineNode()
        {
            if (shadowPortModels != null)
                shadowPortModels.Clear();
            else
                shadowPortModels = new Dictionary<IPortModel, IVariableDeclarationModel>();

            if (!m_Graph)
                return;

            int inputIndex = 0;
            int outputIndex = 0;
            foreach (var declaration in m_Graph.VariableDeclarations)
            {
                switch (declaration.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddDataInput(declaration.VariableName, declaration.DataType);
                        shadowPortModels.Add(m_InputPortModels[inputIndex++], declaration);
                        break;
                    case ModifierFlags.WriteOnly:
                        AddDataOutputPort(declaration.VariableName, declaration.DataType);
                        shadowPortModels.Add(m_OutputPortModels[outputIndex++], declaration);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Variable {declaration.name} has modifiers '{declaration.Modifiers}'");
                }
            }
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                if (m_Graph)
                    hashCode = (hashCode * 777) ^ (m_Graph.GetHashCode());
                return hashCode;
            }
        }

        public void Rename(string newName)
        {
            Undo.RegisterCompleteObjectUndo(Macro.AssetModel as VSGraphAssetModel, "Rename Macro");
            var assetPath = AssetDatabase.GetAssetPath(Macro.AssetModel as VSGraphAssetModel);
            AssetDatabase.RenameAsset(assetPath, ((VSGraphModel)GraphModel).GetUniqueName(newName));

        }

        public override CapabilityFlags Capabilities => base.Capabilities | CapabilityFlags.Renamable;
    }
}
