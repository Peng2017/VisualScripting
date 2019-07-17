using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class PortModel : IPortModel
    {
        TypeHandle m_DataType;

        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;
        public IGraphModel GraphModel => NodeModel?.GraphModel;

        string m_Name;

        public string Name
        {
            get => m_Name;
            set
            {
                if (value == m_Name)
                    return;
                m_Name = value;
                OnValueChanged?.Invoke();
            }
        }

        INodeModel m_NodeModel;

        public INodeModel NodeModel
        {
            get => m_NodeModel;
            set
            {
                if (value == m_NodeModel)
                    return;

                m_NodeModel = value;
                OnValueChanged?.Invoke();
            }
        }

        public ConstantNodeModel EmbeddedValue
        {
            get {
                if (NodeModel is NodeModel node && node.InputConstants.Count > Index)
                {
                    return node.InputConstants[Index] as ConstantNodeModel;
                }

                return null;
            }
        }

        public IConstantNodeModel GetModelToWatch()
        {
            return EmbeddedValue;
        }

        public IEnumerable<IPortModel> ConnectionPortModels
        {
            get { Assert.IsNotNull(GraphModel, $"portModel {Name} has a null GraphModel reference"); return GraphModel.GetConnections(this); }
        }

        PortType m_PortType;

        public PortType PortType
        {
            get => m_PortType;
            set
            {
                if (value == m_PortType)
                    return;

                m_PortType = value;
                OnValueChanged?.Invoke();
            }
        }

        Direction m_Direction;

        public Direction Direction
        {
            get => m_Direction;
            set
            {
                if (value == m_Direction)
                    return;

                m_Direction = value;
                OnValueChanged?.Invoke();
            }
        }

        // Give node model priority over self
        public Port.Capacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

        int m_Index;

        public int Index
        {
            get => m_Index;
            set
            {
                if (value == m_Index)
                    return;

                m_Index = value;
                OnValueChanged?.Invoke();
            }
        }

        public bool Connected => ConnectionPortModels.Any();

        public Action OnValueChanged { get; set; }

        // Capabilities
        public CapabilityFlags Capabilities => 0;

        public TypeHandle DataType
        {
            get
            {
                if (PortType == PortType.Execution || PortType == PortType.Loop)
                    return TypeHandle.ExecutionFlow;
                Assert.IsTrue(PortType == PortType.Data || PortType == PortType.Instance);
                return m_DataType;
            }
            set
            {
                Assert.IsTrue(PortType != PortType.Data || value != null);

                if (value == m_DataType)
                    return;

                m_DataType = value;
                OnValueChanged?.Invoke();
            }
        }

        public override string ToString()
        {
            return $"Port {NodeModel}@{Index}: {PortType} {Name}";
        }

        public Port.Capacity GetDefaultCapacity()
        {
            return (PortType == PortType.Data || PortType == PortType.Instance) ?
                Direction == Direction.Input ?
                Port.Capacity.Single :
                Port.Capacity.Multi :
                (PortType == PortType.Execution || PortType == PortType.Loop) ?
                Direction == Direction.Output ?
                Port.Capacity.Single :
                Port.Capacity.Multi :
                Port.Capacity.Multi;
        }

        public string GetId()
        {
            return string.Empty;
        }

        public string IconTypeString
        {
            get
            {
                Stencil stencil = NodeModel.GraphModel.Stencil;

                if (DataType.IsVsArrayType(stencil))
                    return "typeArray";

                // TODO: should TypHandle.Resolve do this for us?
                // @THEOR SAID HE WOULD THINK ABOUT IT (written on CAPS DAY 2018)
                if (NodeModel is EnumConstantNodeModel enumConst)
                {
                    Type t = enumConst.EnumType.Resolve(stencil);
                    return "type" + t.Name;
                }

                Type thisPortType = DataType.Resolve(stencil);

                if (thisPortType.IsSubclassOf(typeof(Component)))
                    return "typeComponent";
                if (thisPortType.IsSubclassOf(typeof(GameObject)))
                    return "typeGameObject";
                if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                    return "typeRigidBody";
                if (thisPortType.IsSubclassOf(typeof(Transform)))
                    return "typeTransform";
                if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                    return "typeTexture2D";
                if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                    return "typeKeycode";
                if (thisPortType.IsSubclassOf(typeof(Material)))
                    return "typeMaterial";
                if (thisPortType == typeof(Object))
                    return "typeObject";
                return "type" + thisPortType.Name;
            }
        }
    }
}
