using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEditor.VisualScripting.Model.VSPreferences;

namespace UnityEditor.VisualScripting.Editor
{
    static class EdgeReducers
    {
        const int k_Offset = 60;

        public static void Register(Store store)
        {
            store.Register<CreateEdgeAction>(CreateEdge);
            store.RegisterAsync<CreateEdgeFromSinglePortAction>(CreateEdgeFromSinglePort);
            store.Register<SplitEdgeAndInsertNodeAction>(SplitEdgeAndInsertNode);
            store.RegisterAsync<CreateNodeOnEdgeAction>(CreateNodeOnEdge);
        }

        static async Task<State> CreateNodeOnEdge(State previousState, CreateNodeOnEdgeAction action)
        {
            IEdgeModel edgeModel = action.EdgeModel;

            // Do not prompt searcher if it's a loop edge
            if (edgeModel.OutputPortModel.NodeModel is LoopNodeModel && edgeModel.InputPortModel.NodeModel is LoopStackModel)
                return previousState;

            // Invoke searcher
            Vector2 searcherPos = Event.current?.mousePosition == null ? action.MousePosition : Event.current.mousePosition;
            Func<GraphNodeCreationData, IGraphElementModel[]> createElements = await previousState.PromptEdgeSearcher(
                edgeModel,
                searcherPos,
                action.CancellationTokenSource.Token
            );

            // Instantiate node
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Vector2 position = action.GraphPosition - Vector2.up * k_Offset;
            IGraphElementModel[] elementModels = createElements?.Invoke(new GraphNodeCreationData(graphModel, position));

            if (elementModels?.Length == 0 || !(elementModels?[0] is INodeModel selectedNodeModel))
                return previousState;

            // Connect input port
            IPortModel inputPortModel = selectedNodeModel is FunctionCallNodeModel
                ? selectedNodeModel.InputPortModels.FirstOrDefault(p =>
                    p.DataType.Equals(edgeModel.OutputPortModel.DataType))
                : selectedNodeModel.InputPortModels.FirstOrDefault();

            if (inputPortModel != null)
                graphModel.CreateEdge(inputPortModel, edgeModel.OutputPortModel);

            // Find first matching output type and connect it
            IPortModel outputPortModel = GetFirstPortModelOfType(graphModel, edgeModel.InputPortModel.DataType,
                selectedNodeModel.OutputPortModels);

            if (outputPortModel != null)
                graphModel.CreateEdge(edgeModel.InputPortModel, outputPortModel);

            // Delete old edge
            graphModel.DeleteEdge(edgeModel);

            return previousState;
        }

        static State CreateEdge(State previousState, CreateEdgeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            IPortModel outputPortModel = action.OutputPortModel;
            IPortModel inputPortModel = action.InputPortModel;

            if (inputPortModel.NodeModel is LoopStackModel loopStackModel)
            {
                if (!loopStackModel.MatchingStackedNodeType.IsInstanceOfType(outputPortModel.NodeModel))
                    return previousState;
            }

            CreateItemizedNode(previousState, graphModel, inputPortModel, ref outputPortModel);
            graphModel.CreateEdge(inputPortModel, outputPortModel);

            if (action.EdgeModelsToDelete?.Count > 0)
                graphModel.DeleteEdges(action.EdgeModelsToDelete);

            return previousState;
        }

        static async Task<State> CreateEdgeFromSinglePort(State previousState, CreateEdgeFromSinglePortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Vector2 searcherPos = Event.current?.mousePosition == null ? action.Position : Event.current.mousePosition;
            var existingPort = action.PortModel;

            void AddToGroupNode(IEnumerable<INodeModel> createdNodes)
            {
                IEnumerable<INodeModel> nodeModels = createdNodes.ToList();
                if (action.TargetGroup != null && nodeModels.Any())
                    ((GroupNodeModel)action.TargetGroup).AddNodes(nodeModels);
            }

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            var stackPosition = action.Position - Vector2.right * 120;
            switch (existingPort.PortType)
            {
                case PortType.Data:
                case PortType.Instance:

                    IEdgeModel newEdge = null;

                    switch (existingPort.Direction)
                    {
                        case Direction.Output when action.TargetStack != null:
                        {
                            if (existingPort.DataType == TypeHandle.Unknown)
                                return previousState;

                            Func<StackNodeCreationData, IGraphElementModel[]> createElements =
                                await previousState.PromptOutputToStackSearcher(
                                    action.TargetStack,
                                    existingPort,
                                    searcherPos,
                                    action.CancellationTokenSource.Token
                                );

                            if(action.EdgeModelsToDelete?.Count > 0)
                            {
                                if (createElements != null)
                                    graphModel.DeleteEdges(action.EdgeModelsToDelete);
                                else // cancelled ? partial rebuild the temporary deleted edge
                                    graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);
                            }

                            IGraphElementModel[] elementModels = createElements?.Invoke(
                                new StackNodeCreationData(action.TargetStack, action.TargetIndex));

                            if (elementModels?.Length == 0 || !(elementModels?[0] is INodeModel selectedNodeModel))
                                return previousState;

                            AddToGroupNode(elementModels.OfType<INodeModel>());

                            IPortModel outputPortModel = action.PortModel;
                            CreateItemizedNode(previousState, graphModel, selectedNodeModel.InputPortModels[0], ref outputPortModel);
                            newEdge = graphModel.CreateEdge(selectedNodeModel.InputPortModels[0], outputPortModel);
                            break;
                        }

                        case Direction.Output:
                        {
                            Func<GraphNodeCreationData, IGraphElementModel[]> createElements =
                                await previousState.PromptOutputToGraphSearcher(
                                    existingPort,
                                    searcherPos,
                                    action.CancellationTokenSource.Token
                                );

                            if(action.EdgeModelsToDelete?.Count > 0)
                            {
                                if (createElements != null)
                                    graphModel.DeleteEdges(action.EdgeModelsToDelete);
                                else // cancelled ? partial rebuild the temporary deleted edge
                                    graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);
                            }

                            Vector2 position = action.Position - Vector2.up * k_Offset;
                            IGraphElementModel[] elementModels = createElements?.Invoke(
                                new GraphNodeCreationData(graphModel, position));

                            if (elementModels?.Length == 0 || !(elementModels?[0] is INodeModel selectedNodeModel))
                                return previousState;

                            AddToGroupNode(elementModels.OfType<INodeModel>());

                            IPortModel inputPortModel = selectedNodeModel is FunctionCallNodeModel
                                ? GetFirstPortModelOfType(graphModel, action.PortModel.DataType, selectedNodeModel.InputPortModels)
                                : selectedNodeModel.InputPortModels.FirstOrDefault();

                            if (inputPortModel == null)
                                return previousState;

                            IPortModel outputPortModel = action.PortModel;

                            CreateItemizedNode(previousState, graphModel, inputPortModel, ref outputPortModel);
                            newEdge = graphModel.CreateEdge(inputPortModel, outputPortModel);
                            break;
                        }

                        default:
                        {
                            Func<GraphNodeCreationData, IGraphElementModel[]> createElements =
                                await previousState.PromptInputToGraphSearcher(
                                    existingPort,
                                    searcherPos,
                                    action.CancellationTokenSource.Token
                                );

                            if(action.EdgeModelsToDelete?.Count > 0)
                            {
                                if (createElements != null)
                                    graphModel.DeleteEdges(action.EdgeModelsToDelete);
                                else // cancelled ? partial rebuild the temporary deleted edge
                                    graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);
                            }

                            IGraphElementModel[] elementModels = createElements?.Invoke(
                                new GraphNodeCreationData(graphModel, action.Position));

                            if (elementModels?.Length == 0 || !(elementModels?[0] is INodeModel selectedNodeModel))
                                return previousState;

                            AddToGroupNode(elementModels.OfType<INodeModel>());

                            IPortModel outputPortModel = action.PortModel.DataType == TypeHandle.Unknown
                                ? selectedNodeModel.OutputPortModels.FirstOrDefault()
                                : GetFirstPortModelOfType(
                                    graphModel,
                                    action.PortModel.DataType,
                                    selectedNodeModel.OutputPortModels
                                );

                            if (outputPortModel != null)
                                newEdge = graphModel.CreateEdge(action.PortModel, outputPortModel);

                            break;
                        }
                    }

                    if (newEdge != null && previousState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                    {
                        graphModel.LastChanges?.ModelsToAutoAlign.Add(newEdge);
                    }

                    break;

                case PortType.Execution:
                    if (action.EdgeModelsToDelete?.Count > 0)
                        graphModel.DeleteEdges(action.EdgeModelsToDelete);

                    // create an Insert Loop Node in the target stack
                    if (existingPort.NodeModel is LoopStackModel loopStackModel && action.PortModel.Direction == Direction.Input)
                    {
                        if (action.TargetStack != null)
                        {
                            var loopNode = ((StackModel)action.TargetStack).CreateStackedNode(loopStackModel.MatchingStackedNodeType, "", action.TargetIndex);
                            AddToGroupNode(new[] { loopNode });
                            graphModel.CreateEdge(action.PortModel, loopNode.OutputPortModels[0]);
                        }
                    }
                    else // create a new stack
                    {
                        StackModel stack = graphModel.CreateStack(string.Empty, stackPosition);
                        AddToGroupNode(new[] { stack });
                        if (existingPort.Direction == Direction.Output)
                            graphModel.CreateEdge(stack.InputPortModels[0], action.PortModel);
                        else
                            graphModel.CreateEdge(action.PortModel, stack.OutputPortModels[0]);
                    }
                    break;

                case PortType.Loop:
                    if (action.EdgeModelsToDelete?.Count > 0)
                        graphModel.DeleteEdges(action.EdgeModelsToDelete);

                    if (existingPort.NodeModel is LoopNodeModel loopNodeModel)
                    {
                        Type loopStackType = loopNodeModel.MatchingStackType;

                        LoopStackModel loopStack = graphModel.CreateLoopStack(loopStackType, stackPosition);
                        AddToGroupNode(new[] { loopStack });
                        graphModel.CreateEdge(loopStack.InputPortModels[0], action.PortModel);
                    }
                    else
                    {
                        StackModel stack = graphModel.CreateStack(null, stackPosition);
                        graphModel.CreateEdge(stack.InputPortModels[0], action.PortModel);
                    }
                    break;
            }

            return previousState;
        }

        static State SplitEdgeAndInsertNode(State previousState, SplitEdgeAndInsertNodeAction action)
        {
            Assert.IsTrue(action.NodeModel.InputPortModels.Count > 0);
            Assert.IsTrue(action.NodeModel.OutputPortModels.Count > 0);

            var graphModel = ((VSGraphModel)previousState.CurrentGraphModel);
            graphModel.CreateEdge(action.EdgeModel.InputPortModel, action.NodeModel.OutputPortModels[0]);
            graphModel.CreateEdge(action.NodeModel.InputPortModels[0], action.EdgeModel.OutputPortModel);
            graphModel.DeleteEdge(action.EdgeModel);

            return previousState;
        }

        [CanBeNull]
        static IPortModel GetFirstPortModelOfType(
            IGraphModel graphModel,
            TypeHandle typeHandle,
            IEnumerable<IPortModel> portModels
        )
        {
            Stencil stencil = graphModel.Stencil;
            IPortModel unknownPortModel = null;

            // Return the first matching Input portModel
            // If no match was found, return the first Unknown typed portModel
            // Else return null.
            foreach (IPortModel portModel in portModels)
            {
                if (portModel.DataType == TypeHandle.Unknown && unknownPortModel == null)
                {
                    unknownPortModel = portModel;
                }

                if (typeHandle.IsAssignableFrom(portModel.DataType, stencil))
                {
                    return portModel;
                }
            }

            return unknownPortModel;
        }

        static void CreateItemizedNode(State state, VSGraphModel graphModel, IPortModel inputPortModel, ref IPortModel outputPortModel)
        {
            bool wasItemized = false;
            ItemizeOptions currentItemizeOptions = state.Preferences.CurrentItemizeOptions;
            INodeModel nodeToConnect = null;
            // automatically itemize, i.e. duplicate variables as they get connected
            if (outputPortModel.Connected && currentItemizeOptions != ItemizeOptions.Nothing)
            {
                nodeToConnect = outputPortModel.NodeModel;
                var offset = Vector2.up * k_Offset;

                if (currentItemizeOptions.HasFlag(ItemizeOptions.Constants)
                    && nodeToConnect is ConstantNodeModel constantModel)
                {
                    string newName = string.IsNullOrEmpty(constantModel.name)
                        ? "Temporary"
                        : constantModel.name + "Copy";
                    nodeToConnect = graphModel.CreateConstantNode(
                        newName,
                        constantModel.Type.GenerateTypeHandle(graphModel.Stencil),
                        constantModel.Position + offset
                    );
                    ((ConstantNodeModel)nodeToConnect).ObjectValue = constantModel.ObjectValue;
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.Variables)
                    && nodeToConnect is VariableNodeModel variableModel)
                {
                    nodeToConnect = graphModel.CreateVariableNode(variableModel.DeclarationModel,
                        variableModel.Position + offset);
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.Variables)
                        && nodeToConnect is ThisNodeModel thisModel)
                {
                    nodeToConnect = graphModel.CreateNode<ThisNodeModel>("this", thisModel.Position + offset);
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.SystemConstants) &&
                    nodeToConnect is SystemConstantNodeModel sysConstModel)
                {
                    nodeToConnect = graphModel.CreateNode<SystemConstantNodeModel>(sysConstModel.name,
                        sysConstModel.Position + offset, m =>
                        {
                            m.ReturnType = sysConstModel.ReturnType;
                            m.DeclaringType = sysConstModel.DeclaringType;
                            m.Identifier = sysConstModel.Identifier;
                        });
                }

                wasItemized = nodeToConnect != outputPortModel.NodeModel;
                outputPortModel = nodeToConnect.OutputPortModels[outputPortModel.Index];
            }

            GroupNodeModel groupNodeModel = null;
            if (wasItemized)
            {
                if (inputPortModel.NodeModel.IsGrouped)
                    groupNodeModel = (GroupNodeModel)inputPortModel.NodeModel.GroupNodeModel;
                else if (inputPortModel.NodeModel.IsStacked && inputPortModel.NodeModel.ParentStackModel.IsGrouped)
                    groupNodeModel = (GroupNodeModel)inputPortModel.NodeModel.ParentStackModel.GroupNodeModel;
            }

            if (groupNodeModel != null)
                groupNodeModel.AddNode(nodeToConnect);
        }
    }
}
