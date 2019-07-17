using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class StackReducers
    {
        static readonly Vector2 k_StackedTestNodesTokenOffset = new Vector2(100, 0);

        public static void Register(Store store)
        {
            store.Register<CreateStacksForNodesAction>(CreateStacksForNodes);
            store.Register<CreateStackedTestNodeForDebugAction>(CreateStackedTestNodeForDebug);
            store.Register<MoveStackedNodesAction>(MoveStackedNodes);
            store.Register<SplitStackAction>(SplitStack);
            store.Register<MergeStackAction>(MergeStack);
            store.RegisterAsync<ChangeStackedNodeAction>(ChangeStackedNode);
            store.RegisterAsync<CreateStackedNodeFromSearcherAction>(CreateStackedNodeFromSearcher);
        }

        static async Task<State> CreateStackedNodeFromSearcher(State previousState,
            CreateStackedNodeFromSearcherAction action)
        {
            Func<StackNodeCreationData, IGraphElementModel[]> createElements = await previousState.PromptStackSearcher(
                action.StackModel,
                action.Position,
                action.CancellationTokenSource.Token
            );
            IEnumerable<IGraphElementModel> createdStackModels = createElements?.Invoke(new StackNodeCreationData(action.StackModel, action.Index));

            if (createdStackModels?.FirstOrDefault() is INodeModel node)
                AnalyticsHelper.Instance.SetLastNodeCreated(((Object)node).GetInstanceID(), node.Title);

            return previousState;
        }

        static State CreateStacksForNodes(State previousState, CreateStacksForNodesAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            foreach (var stackOptions in action.Stacks)
            {
                StackModel stack = graphModel.CreateStack(string.Empty, stackOptions.Position);
                if (stackOptions.NodeModels != null)
                    stack.MoveStackedNodes(stackOptions.NodeModels, 0);
            }
            return previousState;
        }

        static State CreateStackedTestNodeForDebug(State previousState, CreateStackedTestNodeForDebugAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            StackModel stackModel = (StackModel)action.StackModel;
            MethodInfo methodInfo = TypeSystem.GetMethod(action.NodeTypeHandle.Resolve(graphModel.Stencil), action.MethodName, action.IsStatic);
            INodeModel stackedNode = stackModel.CreateStackedNode<FunctionCallNodeModel>(action.MethodName, -1, n => n.MethodInfo = methodInfo);

            IConstantNodeModel constantNode = graphModel.CreateConstantNode("", typeof(string).GenerateTypeHandle(graphModel.Stencil), stackedNode.Position - k_StackedTestNodesTokenOffset);
            ((ConstantNodeModel<string>)constantNode).value = $"{graphModel.NodeModels.Count}";

            Assert.IsTrue(stackedNode.InputPortModels.Count > 0, $"node {stackedNode.GetType().Name} doesn't have an input port to connect to");
            graphModel.CreateEdge(stackedNode.InputPortModels[0], constantNode.OutputPortModels[0]);

            return previousState;
        }

        static async Task<State> ChangeStackedNode(State previousState, ChangeStackedNodeAction action)
        {
            Func<StackNodeCreationData, IGraphElementModel[]> createElements = await previousState.PromptStackSearcher(
                action.StackModel,
                action.Position,
                action.CancellationTokenSource.Token,
                new SimpleSearcherAdapter("Change this stack node")
            );

            if (createElements == null)
                return previousState;

            var graphModel = ((VSGraphModel)previousState.CurrentGraphModel);

            // Remove old node
            int index = -1;
            if (action.OldNodeModel != null)
                index = action.StackModel.NodeModels.IndexOf(action.OldNodeModel);

            // Add new node
            createElements.Invoke(new StackNodeCreationData(action.StackModel, index));


            // Reconnect edges
            INodeModel newNodeModel = action.StackModel.NodeModels.ElementAt(index);
            if (action.OldNodeModel != null)
            {
                Undo.RegisterCompleteObjectUndo(graphModel, "Change Stacked Node");
                for (var i = 0; i < action.OldNodeModel.InputPortModels.Count; ++i)
                {
                    IPortModel oldInputPort = action.OldNodeModel.InputPortModels[i];
                    if (i < newNodeModel.InputPortModels.Count)
                    {
                        IPortModel newInputPort = newNodeModel.InputPortModels[i];

                        foreach (var edge in graphModel.GetEdgesConnections(oldInputPort).Cast<EdgeModel>())
                        {
                            edge.SetFromPortModels(newInputPort, edge.OutputPortModel);
                        }
                    }
                    else
                    {
                        IEnumerable<IEdgeModel> edges = graphModel.GetEdgesConnections(oldInputPort);
                        graphModel.DeleteEdges(edges);
                        break;
                    }
                }

                // delete after edge patching or undo/redo will fail
                var parentStack = (StackModel)action.StackModel;
                graphModel.DeleteNode(action.OldNodeModel, GraphModel.DeleteConnections.False);
                if (parentStack.Capabilities.HasFlag(CapabilityFlags.DeletableWhenEmpty) &&
                    parentStack != (StackModel) action.StackModel &&
                    !parentStack.NodeModels.Any())
                    graphModel.DeleteNode(parentStack, GraphModel.DeleteConnections.True);
            }
            return previousState;
        }

        static State MoveStackedNodes(State previousState, MoveStackedNodesAction action)
        {
            ((StackModel)action.StackModel).MoveStackedNodes(action.NodeModels, action.Index);
            return previousState;
        }

        static State SplitStack(State previousState, SplitStackAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            if (action.SplitIndex > 0 && action.SplitIndex < action.StackModel.NodeModels.Count())
            {
                // Get the list of nodes to move to another stack.
                var nodeModels = action.StackModel.NodeModels.Skip(action.SplitIndex).ToList();
                if (nodeModels.Any())
                {
                    // Get old stack (stack A)
                    var stackA = action.StackModel;

                    // Create new stack (stack B).
                    var stackB = graphModel.CreateStack(((NodeModel)stackA).name + "_split", stackA.Position + Vector2.up * 300);

                    // Move the list of nodes to this new stack.
                    stackB.MoveStackedNodes(nodeModels, 0);

                    // if the stack had a condition node or anything providing the actual port models, we need to move
                    // the nodes BEFORE fetching the port models, as stack.portModels will actually return the condition
                    // port models
                    var stackAOutputPortModel = stackA.OutputPortModels.First();
                    var stackBInputPortModel = stackB.InputPortModels.First();
                    var stackBOutputPortModel = stackB.OutputPortModels.First();

                    // Connect the edges that were connected to the old stack to the new one.
                    var previousEdgeConnections = graphModel.GetEdgesConnections(stackAOutputPortModel).ToList();
                    foreach (var edge in previousEdgeConnections)
                    {
                        graphModel.CreateEdge(edge.InputPortModel, stackBOutputPortModel);
                        graphModel.DeleteEdge(edge);
                    }

                    // Connect the new stack with the old one.
                    IEdgeModel newEdge = graphModel.CreateEdge(stackBInputPortModel, stackAOutputPortModel);

                    graphModel.LastChanges.ChangedElements.Add(stackA);
                    graphModel.LastChanges.ModelsToAutoAlign.Add(newEdge);
                }
            }

            return previousState;
        }

        static State MergeStack(State previousState, MergeStackAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            var stackModelA = (StackModel)action.StackModelA;
            var stackModelB = (StackModel)action.StackModelB;

            // Move all nodes from stackB to stackA
            stackModelA.MoveStackedNodes(stackModelB.NodeModels.ToList(), -1, false);

            // Move output connections of stackB to stackA
            var previousEdgeConnections = graphModel.GetEdgesConnections(stackModelB.OutputPortModels.First()).ToList();
            foreach (var edge in previousEdgeConnections)
            {
                graphModel.CreateEdge(edge.InputPortModel, stackModelA.OutputPortModels.First());
            }

            // Delete stackB
            graphModel.DeleteNode(stackModelB, GraphModel.DeleteConnections.True);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);

            return previousState;
        }
    }
}
