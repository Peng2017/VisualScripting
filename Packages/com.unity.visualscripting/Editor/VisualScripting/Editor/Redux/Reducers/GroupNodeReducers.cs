using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    static class GroupNodeReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateGroupNodeAction>(CreateGroupNode);
            store.Register<AddToGroupNodeAction>(AddToGroupNode);
            store.Register<RemoveFromGroupNodeAction>(RemoveFromGroupNode);
            store.Register<RenameGroupNodeAction>(RenameGroupNode);
            store.RegisterAsync<CreateGroupedNodeFromSearcherAction>(CreateGroupedNodeFromSearcher);
        }

        static async Task<State> CreateGroupedNodeFromSearcher(State previousState,
            CreateGroupedNodeFromSearcherAction action)
        {
            Func<GraphNodeCreationData, IGraphElementModel[]> createElements =
                await previousState.PromptGroupSearcher(action.Position, action.CancellationTokenSource.Token);

            IGraphElementModel[] elementModels = createElements?.Invoke(
                new GraphNodeCreationData(action.GraphModel, action.Position));

            if (elementModels == null || elementModels.Length == 0)
                return previousState;

            foreach (IGraphElementModel graphElementModel in elementModels)
            {
                if (!(graphElementModel is NodeModel nodeModel))
                    continue;

                if (!nodeModel.IsStacked)
                {
                    ((GroupNodeModel)action.GroupNodeModel).AddNode(nodeModel);
                }
            }

            return previousState;
        }

        static State CreateGroupNode(State previousState, CreateGroupNodeAction action)
        {
            GroupNodeModel groupNodeModel = ((VSGraphModel)previousState.CurrentGraphModel).CreateNode<GroupNodeModel>(action.Name, action.Position);
            groupNodeModel.AddNodes(action.NodeModels);
            return previousState;
        }

        static State AddToGroupNode(State previousState, AddToGroupNodeAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            //TODO: Clean previous group
            // Get only the items that are not in the "new" group
            List<INodeModel> nodesToGroup = action.NodeModels.Where(m => m.GroupNodeModel != action.GroupNodeModel).ToList();

            // And remove the nodes from their previous group
            RemoveFromGroupNode(nodesToGroup);

            var nodesToStack = new List<INodeModel>();

            foreach (NodeModel nodeModel in nodesToGroup.OfType<NodeModel>().Where(n => n.IsStacked))
            {
                nodeModel.GroupNodeModel = null;
                nodesToStack.Add(nodeModel);
            }

            if (nodesToStack.Any())
            {
                StackModel stack = graphModel.CreateStack(string.Empty, action.NewStackPosition);
                stack.MoveStackedNodes(nodesToStack, 0);
                nodesToGroup.Add(stack);
            }

            // Exclude all the nodes that were put into the new stack from the list of nodes to add.
            ((GroupNodeModel)action.GroupNodeModel).AddNodes(nodesToGroup.Except(nodesToStack));
            return previousState;
        }

        static void RemoveFromGroupNode(IEnumerable<INodeModel> nodeModels)
        {
            foreach (IGrouping<GroupNodeModel,INodeModel> nodeModelsGrouping in nodeModels
                .GroupBy(m => m.GroupNodeModel as GroupNodeModel)
                .Where(g => g.Key != null))
            {
                nodeModelsGrouping.Key.RemoveNodes(nodeModelsGrouping);
                VSGraphModel graphModel = ((VSGraphModel) nodeModelsGrouping.Key.GraphModel);
                graphModel.LastChanges.ChangedElements.Add(nodeModelsGrouping.Key);
            }
        }

        static State RemoveFromGroupNode(State previousState, RemoveFromGroupNodeAction action)
        {
            if (action.NodeModels.Length > 0)
            {
                RemoveFromGroupNode(action.NodeModels);
            }

            return previousState;
        }

        static State RenameGroupNode(State previousState, RenameGroupNodeAction action)
        {
            ((GroupNodeModel)action.GroupNodeModel).Rename(action.NewName);
            return previousState;
        }
    }
}
