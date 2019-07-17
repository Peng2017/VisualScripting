using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateGroupNodeAction : IAction
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly INodeModel[] NodeModels;

        public CreateGroupNodeAction(string name, Vector2 position, params INodeModel[] nodeModels)
        {
            Name = name;
            Position = position;
            NodeModels = nodeModels;
        }
    }

    public class AddToGroupNodeAction : IAction
    {
        public readonly IGroupNodeModel GroupNodeModel;
        public readonly INodeModel[] NodeModels;
        public readonly Vector2 NewStackPosition; // Position to use if we have to create a new stack with the given elements.

        public AddToGroupNodeAction(IGroupNodeModel groupNodeModel, params INodeModel[] nodeModels) :
            this(groupNodeModel, Vector2.negativeInfinity, nodeModels)
        { }

        public AddToGroupNodeAction(IGroupNodeModel groupNodeModel, Vector2 newStackPosition, params INodeModel[] nodeModels)
        {
            GroupNodeModel = groupNodeModel;
            NewStackPosition = newStackPosition;
            NodeModels = nodeModels;
        }
    }

    public class RemoveFromGroupNodeAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public RemoveFromGroupNodeAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class RenameGroupNodeAction : IAction
    {
        public readonly IGroupNodeModel GroupNodeModel;
        public readonly string NewName;

        public RenameGroupNodeAction(IGroupNodeModel groupNodeModel, string newName)
        {
            GroupNodeModel = groupNodeModel;
            NewName = newName;
        }
    }

    public class CreateGroupedNodeFromSearcherAction : CancellableAction
    {
        public readonly IGraphModel GraphModel;
        public readonly IGroupNodeModel GroupNodeModel;
        public readonly Vector2 Position;

        public CreateGroupedNodeFromSearcherAction(IGraphModel graphModel, IGroupNodeModel groupNodeModel,
            Vector2 position)
        {
            GraphModel = graphModel;
            GroupNodeModel = groupNodeModel;
            Position = position;
        }
    }
}
