using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public enum LoopConnectionType
    {
        None,
        Stack,
        LoopStack
    }

    public interface INodeModel : IGraphElementModel, IUndoRedoAware
    {
        IStackModel ParentStackModel { get; }
        IGroupNodeModel GroupNodeModel { get; }
        string Title { get; }
        Vector2 Position { get; set; }
        IReadOnlyList<IPortModel> InputPortModels { get; }
        IReadOnlyList<IPortModel> OutputPortModels { get; }
        bool IsStacked { get; }
        bool IsGrouped { get; }
        bool IsCondition { get; }
        bool IsInsertLoop { get; }
        LoopConnectionType LoopConnectionType { get; }
        bool IsBranchType { get; }
        Color Color { get; set; }
        bool HasUserColor { get; set; }
        int OriginalInstanceID { get; set; }

        void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);

        Port.Capacity GetPortCapacity(PortModel portModel);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class INodeModelExtensions
    {
        public static IEnumerable<IPortModel> GetPortModels(this INodeModel node)
        {
            return node.InputPortModels.Concat(node.OutputPortModels);
        }

        public static IEnumerable<INodeModel> GetConnectedNodes(this INodeModel nodeModel)
        {
            foreach (IPortModel portModel in nodeModel.GetPortModels())
            {
                foreach (IPortModel connectionPortModel in portModel.ConnectionPortModels)
                {
                    yield return connectionPortModel.NodeModel;
                }
            }
        }
    }
}
