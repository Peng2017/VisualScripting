using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateEdgeAction : IAction
    {
        public readonly IPortModel InputPortModel;
        public readonly IPortModel OutputPortModel;
        public readonly List<IEdgeModel> EdgeModelsToDelete;

        public CreateEdgeAction(IPortModel inputPortModel, IPortModel outputPortModel, List<IEdgeModel> edgeModelsToDelete = null)
        {
            Assert.IsTrue(inputPortModel.Direction == Direction.Input);
            Assert.IsTrue(outputPortModel.Direction == Direction.Output);
            InputPortModel = inputPortModel;
            OutputPortModel = outputPortModel;
            EdgeModelsToDelete = edgeModelsToDelete;
        }
    }

    public class CreateEdgeFromSinglePortAction : CancellableAction
    {
        public readonly int TargetIndex;
        public readonly IPortModel PortModel;
        public readonly Vector2 Position;
        public readonly List<IEdgeModel> EdgeModelsToDelete;
        public readonly IStackModel TargetStack;
        public readonly IGroupNodeModel TargetGroup;

        public CreateEdgeFromSinglePortAction(IPortModel portModel, Vector2 position,
            List<IEdgeModel> edgeModelsToDelete = null, IStackModel targetStack = null, int targetIndex = -1,
            IGroupNodeModel targetGroup = null)
        {
            TargetIndex = targetIndex;
            PortModel = portModel;
            Position = position;
            EdgeModelsToDelete = edgeModelsToDelete;
            TargetStack = targetStack;
            TargetGroup = targetGroup;
        }
    }

    public class SplitEdgeAndInsertNodeAction : IAction
    {
        public readonly IEdgeModel EdgeModel;
        public readonly INodeModel NodeModel;

        public SplitEdgeAndInsertNodeAction(IEdgeModel edgeModel, INodeModel nodeModel)
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }
    }

    public class CreateNodeOnEdgeAction : CancellableAction
    {
        public readonly IEdgeModel EdgeModel;
        public readonly Vector2 MousePosition;
        public readonly Vector2 GraphPosition;

        public CreateNodeOnEdgeAction(IEdgeModel edgeModel, Vector2 mousePosition, Vector2 graphPosition)
        {
            EdgeModel = edgeModel;
            MousePosition = mousePosition;
            GraphPosition = graphPosition;
        }
    }
}
