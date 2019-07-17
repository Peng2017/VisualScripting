using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    sealed class UIPartialRebuilder: IDisposable
    {
        ILookup<IPortModel, IEdgeModel> m_EdgesByInput, m_EdgesByOutput;

        int m_NumDeleted, m_NumCreated;

        HashSet<INodeModel> m_NodesToRebuild;
        HashSet<IEdgeModel> m_EdgesToRebuild;
        HashSet<IEdgeModel> m_EdgesToDelete;
        HashSet<IGraphElementModel> m_OtherElementsToRebuild;
        HashSet<GraphElement> m_GraphElementsToDelete;

        readonly State m_CurrentState;
        readonly Func<IGraphElementModel, GraphElement> m_CreateElement;
        readonly Action<GraphElement> m_DeleteElement;

        public string DebugOutput => $"Graph UI: -{m_NumDeleted}/+{m_NumCreated} elements" + (BlackboardChanged?" (+full blackboard)":"");
        public bool AnyChangeMade => m_NumDeleted > 0 || m_NumCreated > 0;
        public bool BlackboardChanged { get; set; }

        public UIPartialRebuilder(State currentState, Func<IGraphElementModel, GraphElement> createElement, Action<GraphElement> deleteElement)
        {
            m_NumDeleted = 0;
            m_NumCreated = 0;
            BlackboardChanged = false;

            m_NodesToRebuild = new HashSet<INodeModel>();
            m_EdgesToRebuild = new HashSet<IEdgeModel>();
            m_EdgesToDelete = new HashSet<IEdgeModel>();
            m_OtherElementsToRebuild = new HashSet<IGraphElementModel>();
            m_GraphElementsToDelete = new HashSet<GraphElement>();

            IReadOnlyCollection<IEdgeModel> allEdges = currentState.CurrentGraphModel.EdgeModels;
            m_EdgesByInput = allEdges.ToLookup(e => e.InputPortModel);
            m_EdgesByOutput = allEdges.ToLookup(e => e.OutputPortModel);
            m_CurrentState = currentState;
            m_CreateElement = createElement;
            m_DeleteElement = deleteElement;
        }

        public void Dispose()
        {
            m_NodesToRebuild.Clear();
            m_EdgesToRebuild.Clear();
            m_EdgesToDelete.Clear();
            m_OtherElementsToRebuild.Clear();
            m_GraphElementsToDelete.Clear();
        }

        public void ComputeChanges(IGraphChangeList graphChangeList, Dictionary<IGraphElementModel, GraphElement> existingElements)
        {
            BlackboardChanged = graphChangeList.BlackBoardChanged;

            GetChangesFromChangelist(graphChangeList, existingElements.Keys);

            GatherDeletedElements(existingElements, graphChangeList);

            UpdateEdgesAndNodesForStacks(existingElements);

            UpdateEdgesToRebuildFromNodesToRebuild();

            RemoveDeletedModelsFromRebuildLists(existingElements);

            MarkEdgesToBeRebuiltToDelete(existingElements);
        }

        void MarkEdgesToBeRebuiltToDelete(Dictionary<IGraphElementModel, GraphElement> existingElements)
        {
            foreach (IEdgeModel edgeModel in m_EdgesToRebuild)
            {
                if (existingElements.ContainsKey(edgeModel))
                {
                    m_GraphElementsToDelete.Add(existingElements[edgeModel]);
                }
            }
        }

        void GetChangesFromChangelist(IGraphChangeList graphChanges, IReadOnlyCollection<IGraphElementModel> existingElementModels)
        {
            foreach (IGraphElementModel model in graphChanges.ChangedElements)
            {
                if (model is IEdgeModel edgeModel)
                {
                    m_EdgesToRebuild.Add(edgeModel);
                }
                else if (model is INodeModel nodeModel)
                {
                    m_NodesToRebuild.Add(nodeModel);
                    if (nodeModel is IVariableModel variableModel && variableModel.DeclarationModel == null)
                    {
                        // In particular, ThisNodeModel sometimes requires an update of the blackboard
                        BlackboardChanged = true;
                    }
                }
                else if (model is IVariableDeclarationModel decl)
                {
                    if (decl.Owner is IFunctionModel functionModel)
                    {
                        m_NodesToRebuild.Add(functionModel);
                        var refFuncCalls = existingElementModels.OfType<FunctionRefCallNodeModel>().Where(f => functionModel == (IFunctionModel)f.Function);
                        m_NodesToRebuild.AddRange(refFuncCalls);
                    }
                    else if (decl.Owner is IGraphModel)
                        BlackboardChanged = true;
                }
                else if (model is IStickyNoteModel)
                {
                    m_OtherElementsToRebuild.Add(model);
                }
                else
                {
                    Debug.LogWarning($"Unexpected model to rebuild: {model.GetType().Name}, make sure it is correctly supported by UI Partial Rebuild.");
                    m_OtherElementsToRebuild.Add(model);
                }
            }
        }

        void GatherDeletedElements(Dictionary<IGraphElementModel, GraphElement> existingElements, IGraphChangeList graphChangeList)
        {
            foreach (IGraphElementModel elementModel in existingElements.Keys)
            {
                if (elementModel is Object o && o == null) // if model is dead
                {
                    GraphElement graphElement = existingElements[elementModel];
                    if (graphElement is Experimental.GraphView.Node)
                    {
                        foreach (IEdgeModel edgeModel in GetEdgesInNode((INodeModel)elementModel))
                        {
                            m_EdgesToDelete.Add(edgeModel);
                        }
                    }

                    if (elementModel is IVariableDeclarationModel variableDeclarationModel)
                    {
                        if (variableDeclarationModel.Owner is IFunctionModel functionModel)
                        {
                            var refFuncCalls = existingElements.Keys.OfType<FunctionRefCallNodeModel>().Where(f => functionModel == (IFunctionModel)f.Function);
                            m_NodesToRebuild.AddRange(refFuncCalls);
                        }
                    }
                    m_GraphElementsToDelete.Add(graphElement);
                }
            }

            foreach (IEdgeModel edge in graphChangeList.DeleteEdgeModels)
            {
                if(existingElements.TryGetValue(edge, out GraphElement edgeGraphElement))
                    m_GraphElementsToDelete.Add(edgeGraphElement);
            }
        }

        public void DeleteEdgeModels()
        {
            VSGraphModel graphModel = (VSGraphModel)m_CurrentState?.CurrentGraphModel;
            if (graphModel != null)
            {
                foreach (var edgeModel1 in m_EdgesToDelete)
                {
                    var edgeModel = (EdgeModel)edgeModel1;
                    graphModel.DeleteEdge(edgeModel);
                }
            }
        }

        void UpdateEdgesToRebuildFromNodesToRebuild()
        {
            foreach (INodeModel nodeModel in m_NodesToRebuild)
            {
                if ((Object)nodeModel)
                {
                    foreach (IEdgeModel edgeModel in GetEdgesInNode(nodeModel))
                    {
                        m_EdgesToRebuild.Add(edgeModel);
                    }
                }
            }
        }

        void RemoveDeletedModelsFromRebuildLists(Dictionary<IGraphElementModel, GraphElement> existingElements)
        {
            m_EdgesToRebuild.RemoveWhere(e => existingElements.ContainsKey(e) && m_GraphElementsToDelete.Contains(existingElements[e]));
            m_NodesToRebuild.RemoveWhere(n => existingElements.ContainsKey(n) && m_GraphElementsToDelete.Contains(existingElements[n]));
        }

        IEnumerable<IEdgeModel> GetEdgesInNode(INodeModel node)
        {
            foreach (IPortModel portModel in node.InputPortModels)
            {
                foreach (IEdgeModel edgeModel in m_EdgesByInput[portModel])
                {
                    yield return edgeModel;
                }
            }
            foreach (IPortModel portModel in node.OutputPortModels)
            {
                foreach (IEdgeModel edgeModel in m_EdgesByOutput[portModel])
                {
                    yield return edgeModel;
                }
            }
        }

        void UpdateEdgesAndNodesForStacks(Dictionary<IGraphElementModel, GraphElement> existingElements)
        {
            List<INodeModel> nodesInStack = m_NodesToRebuild.Where(n => n.IsStacked).ToList();
            foreach (INodeModel nodeModel in nodesInStack)
            {
                // mark the stack for rebuild if needed
                m_NodesToRebuild.Add(nodeModel.ParentStackModel);
            }

            List<IStackModel> stacksToRebuild = m_NodesToRebuild.Where(n => n is IStackModel).Cast<IStackModel>().ToList();
            foreach (IStackModel stackModel in stacksToRebuild)
            {
                // remove any stack children from rebuild - rebuild any edge encountered
                foreach (INodeModel subModel in stackModel.NodeModels)
                {
                    m_NodesToRebuild.Remove(subModel);
                    foreach (IEdgeModel edgeModel in GetEdgesInNode(subModel))
                    {
                        m_EdgesToRebuild.Add(edgeModel);
                    }

                    if (existingElements.ContainsKey(subModel))
                    {
                        m_GraphElementsToDelete.Add(existingElements[subModel]);
                    }
                }
            }
        }

        public void DeleteGraphElements()
        {
            foreach (GraphElement graphElement in m_GraphElementsToDelete)
            {
                if (graphElement is Experimental.GraphView.Edge edge)
                {
                    edge.input?.Disconnect(edge);
                    edge.output?.Disconnect(edge);
                    edge.input = null;
                    edge.output = null;
                }
                m_DeleteElement(graphElement);
                m_NumDeleted++;
            }
        }

        public void RebuildNodes(Dictionary<IGraphElementModel, GraphElement> existingElements)
        {
            foreach (INodeModel nodeModel in m_NodesToRebuild)
            {
                Group parentGroup = nodeModel.GroupNodeModel != null ? existingElements[nodeModel.GroupNodeModel] as Group : null;

                // delete node if existing
                if (existingElements.TryGetValue(nodeModel, out var oldElement))
                {
                    parentGroup?.RemoveElement(oldElement);
                    m_DeleteElement(oldElement);
                    m_NumDeleted++;
                }

                // create node
                GraphElement element = m_CreateElement(nodeModel);
                existingElements[nodeModel] = element;
                if (element != null)
                {
                    m_NumCreated++;
                    parentGroup?.AddElementForRebuild(element);
                    if (element is Group group)
                    {
                        group.Populate();
                    }
                }
            }

            foreach (IGraphElementModel elementModel in m_OtherElementsToRebuild)
            {
                // delete node if existing
                if (existingElements.TryGetValue(elementModel, out var oldElement))
                {
                    m_DeleteElement(oldElement);
                    m_NumDeleted++;
                }

                // create node
                if (m_CreateElement(elementModel) != null)
                {
                    m_NumCreated++;
                }
            }
        }

        public void RebuildEdges(Action<IEdgeModel> rebuildEdge)
        {
            foreach (IEdgeModel edgeModel in m_EdgesToRebuild)
            {
                rebuildEdge(edgeModel);
                m_NumCreated++;
            }
        }
    }
}
