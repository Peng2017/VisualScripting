using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public class EdgeModel : IEdgeModel
    {
        [Serializable]
        struct PortReference
        {
            [SerializeField]
            NodeModel m_NodeModel;

            INodeModel NodeModel
            {
                get => m_NodeModel;
                set => m_NodeModel = (NodeModel)value;
            }

            [SerializeField]
            public int Index;

            public void Assign(IPortModel portModel)
            {
                Assert.IsNotNull(portModel);
                NodeModel = portModel.NodeModel;
                Index = portModel.Index;
            }

            public IPortModel GetPortModel(Direction direction, ref IPortModel previousValue)
            {
                if (NodeModel == null)
                    return previousValue = null;

                // when removing a set property member, we patch the edges portIndex
                // the cached value needs to be invalidated
                if (previousValue != null && (previousValue.Index != Index || previousValue.NodeModel != NodeModel || previousValue.Direction != direction))
                    previousValue = null;

                if (previousValue != null)
                    return previousValue;

                var portModels = direction == Direction.Input ? NodeModel.InputPortModels : NodeModel.OutputPortModels;

                if (Index < 0 || portModels == null || Index >= portModels.Count)
                    return null;

                return previousValue = portModels[Index];
            }

            public override string ToString()
            {
                var ownerStr = NodeModel?.ToString() ?? "<null>";
                return $"{ownerStr}@{Index}";
            }
        }

        [SerializeField]
        GraphModel m_GraphModel;
        [SerializeField]
        PortReference m_InputPortReference;
        [SerializeField]
        PortReference m_OutputPortReference;

        IPortModel m_InputPortModel;
        IPortModel m_OutputPortModel;

        public EdgeModel(IGraphModel graphModel, IPortModel inputPort, IPortModel outputPort)
        {
            GraphModel = graphModel;
            SetFromPortModels(inputPort, outputPort);
        }

        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;

        public IGraphModel GraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        // Capabilities
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable;

        public void SetFromPortModels(IPortModel newInputPortModel, IPortModel newOutputPortModel)
        {
            m_InputPortReference.Assign(newInputPortModel);
            m_InputPortModel = newInputPortModel;

            m_OutputPortReference.Assign(newOutputPortModel);
            m_OutputPortModel = newOutputPortModel;
        }

        public IPortModel InputPortModel => m_InputPortReference.GetPortModel(Direction.Input, ref m_InputPortModel);
        public IPortModel OutputPortModel => m_OutputPortReference.GetPortModel(Direction.Output, ref m_OutputPortModel);

        public string GetId()
        {
            return String.Empty;
        }

        public int OutputIndex
        {
            get => m_OutputPortReference.Index;
            set
            {
                m_OutputPortReference.Index = value;
                m_OutputPortModel = null;
            }
        }

        public int InputIndex
        {
            get => m_InputPortReference.Index;
            set
            {
                m_InputPortReference.Index = value;
                m_InputPortModel = null;
            }
        }

        public override string ToString()
        {
            return $"{m_InputPortReference} -> {m_OutputPortReference}";
        }
    }
}
