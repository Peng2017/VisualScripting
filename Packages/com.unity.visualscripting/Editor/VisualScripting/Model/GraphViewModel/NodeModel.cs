using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public abstract class NodeModel : ScriptableObject, INodeModel
    {
        [SerializeField]
        GraphModel m_GraphModel;
        [SerializeField]
        StackModel m_ParentStackModel;
        [SerializeField]
        GroupNodeModel m_GroupNodeModel;
        [SerializeField]
        Vector2 m_Position;
        [SerializeField, HideInInspector]
        Color m_Color = new Color(0.776f, 0.443f, 0, 0.5f);
        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField]
        protected List<ConstantNodeModel> m_InputConstants;

        protected List<PortModel> m_InputPortModels;
        protected List<PortModel> m_OutputPortModels;

        protected Stencil Stencil => m_GraphModel.Stencil;

        public virtual string IconTypeString => "typeNode";

        public virtual string DataTypeString
        {
            get
            {
                IVariableDeclarationModel declarationModel = (this as IVariableModel)?.DeclarationModel;
                return declarationModel?.DataType.GetMetadata(Stencil).FriendlyName ?? string.Empty;
            }
        }

        public virtual string VariableString
        {
            get
            {
                IVariableDeclarationModel declarationModel = (this as IVariableModel)?.DeclarationModel;
                return declarationModel == null ? string.Empty : declarationModel.IsExposed ? "Exposed variable" : "Variable";
            }
        }

        // Capabilities
        public virtual CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable | CapabilityFlags.Movable | CapabilityFlags.Droppable;

        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;

        public IGraphModel GraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        public IStackModel ParentStackModel
        {
            get => m_ParentStackModel;
            set => m_ParentStackModel = (StackModel)value;
        }

        public IGroupNodeModel GroupNodeModel
        {
            get => m_GroupNodeModel;
            set => m_GroupNodeModel = (GroupNodeModel)value;
        }

        public virtual string Title => name;

        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public virtual bool IsCondition => false;
        public virtual bool IsInsertLoop => false;
        public virtual LoopConnectionType LoopConnectionType => LoopConnectionType.None;

        public bool IsBranchType => GetType().GetCustomAttribute<BranchedNodeAttribute>() != null;

        public Color Color
        {
            get => m_HasUserColor ? m_Color : Color.clear;
            set => m_Color = value;
        }

        public virtual bool IsStacked => m_ParentStackModel != null;
        public virtual bool IsGrouped => m_GroupNodeModel != null;

        public IReadOnlyList<IConstantNodeModel> InputConstants => m_InputConstants;
        public virtual IReadOnlyList<IPortModel> InputPortModels => m_InputPortModels;
        public virtual IReadOnlyList<IPortModel> OutputPortModels => m_OutputPortModels;

        public bool HasUserColor
        {
            get => m_HasUserColor;
            set => m_HasUserColor = value;
        }

        public int OriginalInstanceID { get; set; }

        protected NodeModel()
        {
            OriginalInstanceID = GetInstanceID();
        }

        public virtual void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public virtual void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public void DefineNode()
        {
            m_NextInputIndex = 0;
            m_NextOutputIndex = 0;
            OnDefineNode();
            // delete unused leftover ports and constants
            for (int i = m_InputPortModels.Count - 1; i >= m_NextInputIndex; i--)
                DeleteInputPort(m_InputPortModels[i]);
            for (int i = m_OutputPortModels.Count - 1; i >= m_NextOutputIndex; i--)
                DeleteOutputPort(m_OutputPortModels[i]);
            if (m_InputConstants.Count > m_NextInputIndex)
                m_InputConstants.RemoveRange(m_NextInputIndex, m_InputConstants.Count - m_NextInputIndex);
        }

        protected virtual void OnDefineNode()
        {
        }

        public void UndoRedoPerformed()
        {
            Profiler.BeginSample("NodeModel_UndoRedo");
            DefineNode();
            Profiler.EndSample();
        }

        public virtual Port.Capacity GetPortCapacity(PortModel portModel)
        {
            return portModel?.GetDefaultCapacity() ?? Port.Capacity.Multi;
        }

        public string GetId()
        {
            return GetInstanceID().ToString();
        }

        public void Move(Vector2 position)
        {
            Undo.RecordObject(this, "Move");
            Position = position;
        }

        public void ChangeColor(Color color)
        {
            Undo.RecordObject(this, "Change Color");
            HasUserColor = true;
            m_Color = color;
        }

        protected void AddDataInput<TDataType>(string portName)
        {
            AddDataInput(portName, typeof(TDataType).GenerateTypeHandle(Stencil));
        }

        protected void AddDataInput(string portName, TypeHandle typeHandle)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Data,
                DataType = typeHandle,
                Name = portName,
            };

            AddInputPort(portModel);
        }

        protected void AddDataOutputPort<TDataType>(string portName)
        {
            AddDataOutputPort(portName, typeof(TDataType).GenerateTypeHandle(Stencil));
        }

        protected void AddDataOutputPort(string portName, TypeHandle typeHandle)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Data,
                DataType = typeHandle,
                Name = portName,
            };

            AddOutputPort(portModel);
        }

        protected void AddInstanceInput<TDataType>(string portName = null)
        {
            AddInstanceInput(typeof(TDataType).GenerateTypeHandle(Stencil), portName);
        }

        protected void AddInstanceInput(TypeHandle dataType, string portName = null)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Instance,
                DataType = dataType,
                Name = portName ?? "",
            };

            AddInputPort(portModel);
        }

        protected void AddInputExecutionPort(string portName)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Execution,
                DataType = TypeHandle.ExecutionFlow,
                Name = portName,
            };

            AddInputPort(portModel);
        }

        protected void AddExecutionOutputPort(string portName)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Execution,
                DataType = TypeHandle.ExecutionFlow,
                Name = portName,
            };

            AddOutputPort(portModel);
        }

        protected void AddLoopOutputPort(string portName)
        {
            var portModel = new PortModel
            {
                PortType = PortType.Loop,
                DataType = TypeHandle.ExecutionFlow,
                Name = portName
            };

            AddOutputPort(portModel);
        }

        int m_NextInputIndex;
        int m_NextOutputIndex;

        protected void AddInputPort(PortModel portModel)
        {
            Assert.IsTrue(m_InputPortModels.Count <= m_NextInputIndex || m_InputPortModels[m_NextInputIndex].Index == m_NextInputIndex);
            Type type = portModel.DataType.Resolve(Stencil);
            if (m_InputPortModels.Count <= m_NextInputIndex)
            {
                portModel.NodeModel = this;
                portModel.Direction = Direction.Input;
                portModel.Index = m_NextInputIndex;
                m_InputPortModels.Add(portModel);
            }
            else
            {
                var existing = m_InputPortModels[m_NextInputIndex];
                existing.DataType = portModel.DataType;
                existing.PortType = portModel.PortType;
                existing.Name = portModel.Name;
            }

            if (m_InputConstants.Count <= m_NextInputIndex)
            {
                m_InputConstants.Add(null);
            }
            Assert.IsTrue(m_NextInputIndex < m_InputConstants.Count);

            // Destroy existing constant if not compatible
            if (m_InputConstants[m_NextInputIndex] != null && m_InputConstants[m_NextInputIndex].Type != type)
            {
                m_InputConstants[m_NextInputIndex].Destroy();
                m_InputConstants[m_NextInputIndex] = null;
            }

            // Create new constant if needed
            // TODO : this could probably support more types
            if (m_InputConstants[m_NextInputIndex] == null
                && portModel.PortType == PortType.Data
                && portModel.DataType != TypeHandle.Unknown
                && Stencil.GetConstantNodeModelType(portModel.DataType) != null)
            {
                ConstantNodeModel embeddedConstant = (ConstantNodeModel)((VSGraphModel)GraphModel).CreateConstantNode(portModel.Name, portModel.DataType, Vector2.zero, NodeCreationMode.Orphan);
                Utility.SaveAssetIntoObject(embeddedConstant, (Object)AssetModel);

                m_InputConstants[m_NextInputIndex] = embeddedConstant;
            }

            m_NextInputIndex++;
        }

        internal void ReinstantiateInputConstants()
        {
            for (var i = 0; i < InputConstants.Count; i++)
            {
                ConstantNodeModel inputConstant = m_InputConstants[i];
                if (inputConstant != null)
                {
                    m_InputConstants[i] = (ConstantNodeModel)Instantiate((Object)inputConstant);
                }
            }
        }

        protected void AddOutputPort(PortModel portModel)
        {
            Assert.IsTrue(m_OutputPortModels.Count <= m_NextOutputIndex || m_OutputPortModels[m_NextOutputIndex].Index == m_NextOutputIndex);
            if (m_OutputPortModels.Count <= m_NextOutputIndex)
            {
                portModel.NodeModel = this;
                portModel.Direction = Direction.Output;
                portModel.Index = m_NextOutputIndex++;
                m_OutputPortModels.Add(portModel);
            }
            else
            {
                var existing = m_OutputPortModels[m_NextOutputIndex++];
                existing.DataType = portModel.DataType;
                existing.PortType = portModel.PortType;
                existing.Name = portModel.Name;
            }
        }

        protected void DeleteInputPort(PortModel portModel)
        {
            DeletePort(portModel);
            Assert.IsTrue(m_InputPortModels[portModel.Index] == portModel);
            m_InputPortModels.RemoveAt(portModel.Index);
        }

        protected void DeleteOutputPort(PortModel portModel)
        {
            DeletePort(portModel);
            m_OutputPortModels.Remove(portModel);
        }

        protected void DeletePort(PortModel portModel)
        {
            var edgeModels = GraphModel.GetEdgesConnections(portModel);
            ((GraphModel)GraphModel).DeleteEdges(edgeModels);
        }

        protected virtual void OnEnable()
        {
            if (m_InputPortModels == null)
                m_InputPortModels = new List<PortModel>();
            if (m_OutputPortModels == null)
                m_OutputPortModels = new List<PortModel>();
            if (m_InputConstants == null)
                m_InputConstants = new List<ConstantNodeModel>();
            if (GraphModel?.AssetModel != null)
                DefineNode();
        }

        public virtual void Destroy()
        {
            if (m_GroupNodeModel)
                m_GroupNodeModel.RemoveNode(this);
            Undo.DestroyObjectImmediate(this);
        }
    }

    public abstract class HighLevelNodeModel : NodeModel { }
}
