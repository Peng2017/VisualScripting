using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public struct StackCreationOptions
    {
        public Vector2 Position;
        public List<INodeModel> NodeModels;

        public StackCreationOptions(Vector2 position, List<INodeModel> nodeModels = null) : this()
        {
            Position = position;
            NodeModels = nodeModels;
        }
    }

    public class CreateStacksForNodesAction : IAction
    {
        public readonly List<StackCreationOptions> Stacks;

        public CreateStacksForNodesAction(List<StackCreationOptions> stacks)
        {
            Stacks = stacks;
        }

        public CreateStacksForNodesAction(StackCreationOptions stackOptions)
        {
            Stacks = new List<StackCreationOptions>
            {
                new StackCreationOptions {Position = stackOptions.Position, NodeModels = stackOptions.NodeModels }
            };
        }
    }

    public class ChangeStackedNodeAction : CancellableAction
    {
        public readonly INodeModel OldNodeModel;
        public readonly IStackModel StackModel;
        public readonly Vector2 Position;

        public ChangeStackedNodeAction(INodeModel oldNodeModel, IStackModel stackModel, Vector2 position)
        {
            OldNodeModel = oldNodeModel;
            StackModel = stackModel;
            Position = position;
        }
    }

    public class MoveStackedNodesAction : IAction
    {
        public readonly IReadOnlyCollection<INodeModel> NodeModels;
        public readonly IStackModel StackModel;
        public readonly int Index;

        public MoveStackedNodesAction(IReadOnlyCollection<INodeModel> nodeModels, IStackModel stackModel, int index)
        {
            NodeModels = nodeModels;
            StackModel = stackModel;
            Index = index;
        }
    }

    public class SplitStackAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly int SplitIndex;

        public SplitStackAction(IStackModel stackModel, int splitIndex)
        {
            StackModel = stackModel;
            SplitIndex = splitIndex;
        }
    }

    public class MergeStackAction : IAction
    {
        public readonly IStackModel StackModelA;
        public readonly IStackModel StackModelB;

        public MergeStackAction(IStackModel stackModelA, IStackModel stackModelB)
        {
            StackModelA = stackModelA;
            StackModelB = stackModelB;
        }
    }

    public class CreateStackedNodeFromSearcherAction : CancellableAction
    {
        public readonly IStackModel StackModel;
        public readonly int Index;
        public readonly Vector2 Position;

        public CreateStackedNodeFromSearcherAction(IStackModel stackModel, int index, Vector2 position)
        {
            StackModel = stackModel;
            Index = index;
            Position = position;
        }
    }
}
