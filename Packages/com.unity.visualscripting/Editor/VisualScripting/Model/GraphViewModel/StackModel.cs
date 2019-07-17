using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class StackModel : NodeModel, IStackModel
    {
        [SerializeField]
        List<NodeModel> m_NodeModels;

        [CanBeNull]
        public override string Title => null;

        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
                                                        CapabilityFlags.Movable | CapabilityFlags.DeletableWhenEmpty;

        public virtual IFunctionModel OwningFunctionModel { get; private set; }

        public IEnumerable<INodeModel> NodeModels => m_NodeModels;

        public void SetOwningFunction(IFunctionModel function)
        {
            OwningFunctionModel = function;
        }

        public virtual bool AcceptNode(Type nodeType)
        {
            // Do not accept more than 1 branched node
            bool isBranchedNode = Attribute.IsDefined(nodeType, typeof(BranchedNodeAttribute));
            foreach (NodeModel child in m_NodeModels)
            {
                if (isBranchedNode && Attribute.IsDefined(child.GetType(), typeof(BranchedNodeAttribute)))
                {
                    return false;
                }
            }

            return true;
        }

        public override IReadOnlyList<IPortModel> OutputPortModels
        {
            get
            {
                return DelegatesOutputsToNode(out var last)
                    ? last.OutputPortModels
                        .Where(p => p.PortType == PortType.Execution || p.PortType == PortType.Loop).ToList()
                    : base.OutputPortModels;
            }
        }

        public bool DelegatesOutputsToNode(out INodeModel last)
        {
            last = m_NodeModels.LastOrDefault();

            return last!= null && last.IsBranchType && last.OutputPortModels.Count > 0;
        }

        public void CleanUp()
        {
            m_NodeModels.RemoveAll(n => n == null);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_NodeModels == null)
                m_NodeModels = new List<NodeModel>();
        }

        public TNodeType CreateStackedNode<TNodeType>(string nodeName, int index, Action<TNodeType> setup = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateStackedNode(typeof(TNodeType), nodeName, index, n => setup?.Invoke((TNodeType)n));
            return node;
        }

        public INodeModel CreateStackedNode(Type nodeTypeToCreate, string nodeName, int index, Action<NodeModel> preDefineSetup = null)
        {
            var nodeModel = (NodeModel)CreateOrphanStackedNode(nodeTypeToCreate, nodeName, preDefineSetup);
            Undo.RegisterCreatedObjectUndo(nodeModel, "Create Node");
            AddStackedNode(nodeModel, index);

            return nodeModel;
        }

        public TNodeType CreateOrphanStackedNode<TNodeType>(string nodeName, Action<TNodeType> setup = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateOrphanStackedNode(typeof(TNodeType), nodeName, n => setup?.Invoke((TNodeType)n));
            return node;
        }

        public INodeModel CreateOrphanStackedNode(Type nodeTypeToCreate, string nodeName, Action<NodeModel> preDefineSetup = null)
        {
            var nodeModel = (NodeModel)CreateInstance(nodeTypeToCreate);
            nodeModel.name = nodeName ?? nodeTypeToCreate.Name;
            nodeModel.Position = Vector2.zero;
            nodeModel.GraphModel = GraphModel;
            nodeModel.ParentStackModel = this;
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.DefineNode();

            return nodeModel;
        }

        public void MoveStackedNodes(IReadOnlyCollection<INodeModel> nodesToMove, int actionNewIndex, bool deleteWhenEmpty = true)
        {
            if (nodesToMove == null)
                return;

            int i = 0;
            foreach (var nodeModel in nodesToMove)
            {
                var parentStack = (StackModel)nodeModel.ParentStackModel;
                if (parentStack != null)
                {
                    parentStack.RemoveStackedNode(nodeModel);
                    if (deleteWhenEmpty && parentStack.Capabilities.HasFlag(CapabilityFlags.DeletableWhenEmpty) &&
                        parentStack != this &&
                        !parentStack.GetConnectedNodes().Any() &&
                        !parentStack.NodeModels.Any())
                        ((VSGraphModel)GraphModel).DeleteNode(parentStack, GraphViewModel.GraphModel.DeleteConnections.True);
                }
            }

            // We need to do it in two passes to allow for same stack move of multiple nodes.
            foreach (var nodeModel in nodesToMove)
                AddStackedNode(nodeModel, actionNewIndex == -1 ? -1 : actionNewIndex + i++);

        }

        public void AddStackedNode(INodeModel nodeModelInterface, int index)
        {
            if (!AcceptNode(nodeModelInterface.GetType()))
                return;

            var nodeModel = (NodeModel)nodeModelInterface;
            Utility.SaveAssetIntoObject(nodeModel, (Object)AssetModel);

            Undo.RegisterCompleteObjectUndo(this, "Add Node");

            nodeModel.GraphModel = GraphModel;
            nodeModel.ParentStackModel = this;
            if (index == -1)
                m_NodeModels.Add(nodeModel);
            else
                m_NodeModels.Insert(index, nodeModel);

            VSGraphModel vsGraphModel = (VSGraphModel)GraphModel;
            vsGraphModel.LastChanges.ChangedElements.Add(nodeModel);

            EditorUtility.SetDirty(this);
        }

        public void RemoveStackedNode(INodeModel nodeModel)
        {
            Undo.RegisterCompleteObjectUndo(this, "RemoveNode");
            Undo.RegisterCompleteObjectUndo((Object)nodeModel, "Unparent Node");
            ((NodeModel)nodeModel).ParentStackModel = null;
            m_NodeModels.Remove((NodeModel)nodeModel);

            VSGraphModel vsGraphModel = (VSGraphModel)GraphModel;
            vsGraphModel.LastChanges.DeletedElements++;

            EditorUtility.SetDirty(this);
        }

        protected override void OnDefineNode()
        {
            AddInputExecutionPort(null);
            AddExecutionOutputPort(null);
        }

        public void ClearNodes()
        {
            m_NodeModels.Clear();
        }
    }
}
