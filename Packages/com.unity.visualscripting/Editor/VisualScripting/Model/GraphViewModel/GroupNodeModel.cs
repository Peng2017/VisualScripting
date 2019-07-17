using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class GroupNodeModel : NodeModel, IGroupNodeModel
    {
        [SerializeField]
        protected List<NodeModel> m_NodeModels;

        public override string Title => name;
        public IEnumerable<INodeModel> NodeModels => m_NodeModels;
        // Nested group are not supported, so we pretend that we are already grouped.
        public override bool IsGrouped => true;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_NodeModels == null)
                m_NodeModels = new List<NodeModel>();
        }

        public void MoveDelta(Vector2 delta)
        {
            Undo.RegisterCompleteObjectUndo(this, "Move");
            Position = Position + delta;
        }

        public void AddNodes(IEnumerable<INodeModel> models)
        {
            foreach (var model in models)
            {
                AddNode(model);
            }
        }

        public void AddNode(INodeModel nodeModel)
        {
            var model = (NodeModel)nodeModel;
            Undo.RegisterCompleteObjectUndo(this, "Add node(s) to group");
            Undo.RegisterCompleteObjectUndo(model, "Add node(s) to group");
            model.GroupNodeModel = this;
            m_NodeModels.Add(model);
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
        }

        public void RemoveNodes(IEnumerable<INodeModel> models)
        {
            foreach (var model in models)
            {
                RemoveNode(model);
            }
        }

        public static void Ungroup(IEnumerable<INodeModel> nodeModels)
        {
            foreach (IGrouping<GroupNodeModel,INodeModel> nodeModelsGrouping in nodeModels
                .GroupBy(m => m.GroupNodeModel as GroupNodeModel)
                .Where(g => g.Key != null))
            {
                nodeModelsGrouping.Key.RemoveNodes(nodeModelsGrouping);
            }
        }

        public void RemoveNode(INodeModel nodeModel)
        {
            var model = (NodeModel)nodeModel;
            Undo.RegisterCompleteObjectUndo(this, "Remove Node(s) from group");
            Undo.RegisterCompleteObjectUndo(model, "Remove Node(s) from group");
            model.GroupNodeModel = null;
            m_NodeModels.Remove(model);
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
        }

        public void Rename(string newName)
        {
            Undo.RegisterCompleteObjectUndo(this, "Rename group");
            name = newName;
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
        }

        public override void Destroy()
        {
            foreach (var nodeModel in NodeModels.OfType<NodeModel>())
            {
                nodeModel.GroupNodeModel = null;
            }

            base.Destroy();
        }
    }
}
