using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public abstract class GraphTraversal
    {
        public void VisitGraph(VSGraphModel vsGraphModel)
        {
            HashSet<IStackModel> visitedStacks = new HashSet<IStackModel>();
            HashSet<INodeModel> visitedNodes = new HashSet<INodeModel>();
            foreach (var rootStack in RoslynTranslator.GetEntryPointStacks(vsGraphModel))
            {
                VisitStack(rootStack, visitedStacks, visitedNodes);
            }

            // floating stacks
            foreach (var stack in vsGraphModel.StackModels)
            {
                if (visitedStacks.Contains(stack))
                    continue;

                VisitStack(stack, visitedStacks, visitedNodes);
            }

            // floating nodes
            foreach (var node in vsGraphModel.NodeModels)
            {
                if (node == null || node is IStackModel || visitedNodes.Contains(node))
                    continue;

                VisitNode(node, visitedNodes);
            }

            foreach (var variableDeclaration in vsGraphModel.GraphVariableModels)
            {
                VisitVariableDeclaration(variableDeclaration);
            }
        }

        protected virtual void VisitStack(IStackModel stack, HashSet<IStackModel> visitedStacks, HashSet<INodeModel> visitedNodes)
        {
            visitedStacks.Add(stack);

            // instance/data ports on stacks
            foreach (var inputPortModel in stack.InputPortModels)
            {
                if (inputPortModel.PortType != PortType.Execution &&
                    inputPortModel.Connected)
                {
                    bool any = false;
                    foreach (var connectionPortModel in inputPortModel.ConnectionPortModels)
                    {
                        if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                            VisitNode(connectionPortModel.NodeModel, visitedNodes);

                        any = true;
                        stack.OnConnection(inputPortModel, connectionPortModel);
                    }

                    if (!any)
                        stack.OnConnection(inputPortModel, null);
                }
            }

            // Still not visiting variable parameters...
            if (stack is IHasVariableDeclaration hasVariableDeclaration)
            {
                foreach (var variableDeclaration in hasVariableDeclaration.VariableDeclarations)
                {
                    VisitVariableDeclaration(variableDeclaration);
                }
            }

            foreach (INodeModel nodeModel in stack.NodeModels)
            {
                VisitNode(nodeModel, visitedNodes);
            }

            foreach (StackModel connectedStack in RoslynTranslator.GetConnectedStacks(stack))
            {
                if (connectedStack == null || visitedStacks.Contains(connectedStack))
                    continue;
                VisitStack(connectedStack, visitedStacks, visitedNodes);
            }
        }

        protected virtual void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            if (nodeModel == null)
                return;

            visitedNodes.Add(nodeModel);

            if (nodeModel is IHasVariableDeclaration hasVariableDeclaration)
            {
                foreach (var variableDeclaration in hasVariableDeclaration.VariableDeclarations)
                {
                    VisitVariableDeclaration(variableDeclaration);
                }
            }

            foreach (var inputPortModel in nodeModel.InputPortModels)
            {
                if (inputPortModel.Connected)
                    foreach (var connectionPortModel in inputPortModel.ConnectionPortModels)
                    {
                        if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                            VisitNode(connectionPortModel.NodeModel, visitedNodes);
                    }
            }
        }

        protected virtual void VisitVariableDeclaration(IVariableDeclarationModel variableDeclarationModel) { }
    }
}
