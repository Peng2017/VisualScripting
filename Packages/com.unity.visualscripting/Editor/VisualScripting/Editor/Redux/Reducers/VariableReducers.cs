using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    static class VariableReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateVariableNodesAction>(CreateVariableNodes);
            store.Register<CreateFunctionVariableDeclarationAction>(CreateFunctionVariableDeclaration);
            store.Register<CreateFunctionParameterDeclarationAction>(CreateFunctionParameterDeclaration);
            store.Register<DuplicateFunctionVariableDeclarationsAction>(DuplicateFunctionVariableDeclarations);
            store.Register<CreateGraphVariableDeclarationAction>(CreateGraphVariableDeclaration);
            store.Register<DuplicateGraphVariableDeclarationsAction>(DuplicateGraphVariableDeclarations);
            store.Register<CreateConstantNodeAction>(CreateConstantNode);
            store.Register<CreateSystemConstantNodeAction>(CreateSystemConstantNode);
            store.Register<ReorderGraphVariableDeclarationAction>(ReorderGraphVariableDeclaration);
            store.Register<ConvertVariableNodesToConstantNodesAction>(ConvertVariableNodesToConstantNodes);
            store.Register<ConvertConstantNodesToVariableNodesAction>(ConvertConstantNodesToVariableNodes);
            store.Register<MoveVariableDeclarationAction>(MoveVariableDeclaration);
            store.Register<ItemizeVariableNodeAction>(ItemizeVariableNode);
            store.Register<ItemizeConstantNodeAction>(ItemizeConstantNode);
            store.Register<ItemizeSystemConstantNodeAction>(ItemizeSystemConstantNode);
            store.Register<ToggleLockConstantNodeAction>(ToggleLockConstantNode);

            store.Register<UpdateTypeAction>(UpdateType);
            store.Register<UpdateExposedAction>(UpdateExposed);
            store.Register<UpdateTypeRankAction>(UpdateTypeRank);
            store.Register<UpdateTooltipAction>(UpdateTooltip);
        }

        static State CreateVariableNodes(State previousState, CreateVariableNodesAction action)
        {
            if (action.VariablesToCreate.Count > 0)
            {
                if (action.ConnectAfterCreation != null)
                {
                    // Delete previous connections
                    if (action.EdgeModelsToDelete.Any())
                    {
                        ((VSGraphModel)previousState.CurrentGraphModel).DeleteEdges(action.EdgeModelsToDelete);
                    }
                }

                bool needToGroup = action.GroupModel != null;
                List<INodeModel> nodesToGroup = needToGroup ? new List<INodeModel>(action.VariablesToCreate.Count) : null;

                foreach (Tuple<IVariableDeclarationModel, Vector2> tuple in action.VariablesToCreate)
                {
                    VSGraphModel vsGraphModel = ((VSGraphModel)previousState.CurrentGraphModel);
                    IVariableModel newVariable = vsGraphModel.CreateVariableNode(tuple.Item1, tuple.Item2);

                    if (action.ConnectAfterCreation != null)
                    {
                        var newEdge = ((VSGraphModel)previousState.CurrentGraphModel).CreateEdge(action.ConnectAfterCreation, newVariable.OutputPortModels.Single());
                        if (action.AutoAlign)
                        {
                            vsGraphModel.LastChanges.ModelsToAutoAlign.Add(newEdge);
                        }
                    }

                    if (needToGroup)
                    {
                        nodesToGroup.Add(newVariable);
                    }
                }

                if (needToGroup)
                {
                    GroupNodeModel.Ungroup(nodesToGroup);

                    ((GroupNodeModel)action.GroupModel).AddNodes(nodesToGroup);
                }
            }

            return previousState;
        }

        static State CreateFunctionVariableDeclaration(State previousState, CreateFunctionVariableDeclarationAction action)
        {
            VariableDeclarationModel variableDeclarationModel = ((FunctionModel)action.FunctionModel).CreateFunctionVariableDeclaration(action.Name, action.Type);
            previousState.EditorDataModel.ElementModelToRename = variableDeclarationModel;
            return previousState;
        }

        static State CreateFunctionParameterDeclaration(State previousState, CreateFunctionParameterDeclarationAction action)
        {
            VariableDeclarationModel variableDeclarationModel = ((FunctionModel)action.FunctionModel).CreateFunctionParameterDeclaration(action.Name, action.Type);
            previousState.EditorDataModel.ElementModelToRename = variableDeclarationModel;
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            return previousState;
        }

        static State DuplicateFunctionVariableDeclarations(State previousState, DuplicateFunctionVariableDeclarationsAction action)
        {
            List<VariableDeclarationModel> duplicatedModels = ((FunctionModel)action.FunctionModel).DuplicateFunctionVariableDeclarations(action.VariableDeclarationModels);
            previousState.EditorDataModel?.SelectElementsUponCreation(duplicatedModels, true);
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State DuplicateGraphVariableDeclarations(State previousState, DuplicateGraphVariableDeclarationsAction action)
        {
            List<VariableDeclarationModel> duplicatedModels = ((VSGraphModel)previousState.CurrentGraphModel).DuplicateGraphVariableDeclarations(action.VariableDeclarationModels);
            previousState.EditorDataModel?.SelectElementsUponCreation(duplicatedModels, true);
            return previousState;
        }

        static State CreateConstantNode(State previousState, CreateConstantNodeAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateConstantNode(action.Name, action.Type, action.Position);
            return previousState;
        }

        static State CreateSystemConstantNode(State previousState, CreateSystemConstantNodeAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateNode<SystemConstantNodeModel>(action.Name, action.Position,
                m =>
                {
                    m.ReturnType = action.ReturnType;
                    m.DeclaringType = action.DeclaringType;
                    m.Identifier = action.Identifier;
                });
            return previousState;
        }

        static State CreateGraphVariableDeclaration(State previousState, CreateGraphVariableDeclarationAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            VariableDeclarationModel variableDeclaration = graphModel.CreateGraphVariableDeclaration(action.Name, action.TypeHandle, action.IsExposed);
            variableDeclaration.Modifiers = action.ModifierFlags;
            previousState.EditorDataModel.ElementModelToRename = variableDeclaration;
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            return previousState;
        }

        static State ReorderGraphVariableDeclaration(State previousState, ReorderGraphVariableDeclarationAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).ReorderGraphVariableDeclaration(action.VariableDeclarationModel, action.Index);
            return previousState;
        }

        static State ConvertVariableNodesToConstantNodes(State previousState, ConvertVariableNodesToConstantNodesAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            int variableModelsCount = action.VariableModels.Length;
            previousState.EditorDataModel.ElementModelToRename = null;
            foreach (var iVariableModel in action.VariableModels)
            {
                var variableModel = (VariableNodeModel)iVariableModel;
                if (graphModel.Stencil.GetConstantNodeModelType(variableModel.DataType) == null)
                    continue;
                var constantNode = (ConstantNodeModel)graphModel.CreateConstantNode(variableModel.name, variableModel.DataType, variableModel.Position);
                // Rename converted item only if there are no other items to be converted
                if (variableModelsCount == 1)
                    previousState.EditorDataModel.ElementModelToRename = constantNode;
                constantNode.ObjectValue = variableModel.DeclarationModel?.InitializationModel?.ObjectValue;

                foreach (var edge in graphModel.GetEdgesConnections(variableModel.OutputPortModels[0]).ToList())
                {
                    graphModel.CreateEdge(edge.InputPortModel, constantNode.OutputPortModels[0]);
                    graphModel.DeleteEdge(edge);
                }

                graphModel.DeleteNode(variableModel, GraphModel.DeleteConnections.False);
            }
            return previousState;
        }

        static State ConvertConstantNodesToVariableNodes(State previousState, ConvertConstantNodesToVariableNodesAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            foreach (var iConstantModel in action.ConstantModels)
            {
                var constantModel = (ConstantNodeModel)iConstantModel;

                var declarationModel = graphModel.CreateGraphVariableDeclaration(TypeSystem.CodifyString(constantModel.Type.FriendlyName()), constantModel.Type.GenerateTypeHandle(graphModel.Stencil), true);
                declarationModel.UseDeclarationModelCopy(constantModel);
                var variableModel = graphModel.CreateVariableNode(declarationModel, constantModel.Position);

                foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPortModels[0]).ToList())
                {
                    graphModel.CreateEdge(edge.InputPortModel, variableModel.OutputPortModels[0]);
                    graphModel.DeleteEdge(edge);
                }

                graphModel.DeleteNode(constantModel, GraphModel.DeleteConnections.False);
            }

            return previousState;
        }

        static State MoveVariableDeclaration(State previousState, MoveVariableDeclarationAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).MoveVariableDeclaration(action.VariableDeclarationModel, action.Destination);
            return previousState;
        }

        static State ItemizeVariableNode(State previousState, ItemizeVariableNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            foreach (var iVariableModel in action.VariableModels)
            {
                var variableModel = (VariableNodeModel)iVariableModel;
                var edges = graphModel.GetEdgesConnections(variableModel.OutputPortModels[0]).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var newModel = graphModel.CreateVariableNode(variableModel.DeclarationModel, variableModel.Position + Vector2.up * i * 50);
                    graphModel.CreateEdge(edge.InputPortModel, newModel.OutputPortModels[0]);
                    graphModel.DeleteEdge(edge);
                }
            }

            return previousState;
        }

        static State ItemizeConstantNode(State previousState, ItemizeConstantNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            foreach (var iConstantModel in action.ConstantModels)
            {
                var constantModel = (ConstantNodeModel)iConstantModel;
                var edges = graphModel.GetEdgesConnections(constantModel.OutputPortModels[0]).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var newName = (string.IsNullOrEmpty(constantModel.name) ? "Temporary" : constantModel.name) + i;
                    var newModel = (ConstantNodeModel)graphModel.CreateConstantNode(newName, constantModel.Type.GenerateTypeHandle(graphModel.Stencil), constantModel.Position + Vector2.up * i * 50);
                    newModel.ObjectValue = constantModel.ObjectValue;
                    graphModel.CreateEdge(edge.InputPortModel, newModel.OutputPortModels[0]);
                    graphModel.DeleteEdge(edge);
                }
            }

            return previousState;
        }

        static State ItemizeSystemConstantNode(State previousState, ItemizeSystemConstantNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            foreach (var iConstantModel in action.ConstantModels)
            {
                var constantModel = (SystemConstantNodeModel)iConstantModel;
                var edges = graphModel.GetEdgesConnections(constantModel.OutputPortModels[0]).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var newModel = graphModel.CreateNode<SystemConstantNodeModel>(constantModel.name, constantModel.Position + Vector2.up * i * 50,
                            m =>
                        {
                            m.ReturnType = constantModel.ReturnType;
                            m.DeclaringType = constantModel.DeclaringType;
                            m.Identifier = constantModel.Identifier;
                        });
                    graphModel.CreateEdge(edge.InputPortModel, newModel.OutputPortModels[0]);
                    graphModel.DeleteEdge(edge);
                }
            }

            return previousState;
        }

        static State ToggleLockConstantNode(State previousState, ToggleLockConstantNodeAction action)
        {
            bool needUpdate = false;
            foreach (IConstantNodeModel constantNodeModel in action.ConstantNodeModels)
            {
                var model = constantNodeModel as ConstantNodeModel;
                if (model != null)
                {
                    model.IsLocked = !model.IsLocked;
                    needUpdate = true;
                }
            }

            if (needUpdate)
            {
                IEnumerable<IGraphElementModel> changedModels = action.ConstantNodeModels;
                ((VSGraphModel)previousState.CurrentGraphModel).LastChanges.ChangedElements.AddRange(changedModels);
            }

            return previousState;
        }

        static State UpdateType(State previousState, UpdateTypeAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)((GraphModel)previousState.CurrentGraphModel);

            if (action.Handle.IsValid)
            {
                Undo.RegisterCompleteObjectUndo(action.VariableDeclarationModel, "Update Type");

                if (action.VariableDeclarationModel.DataType != action.Handle)
                    action.VariableDeclarationModel.CreateInitializationValue();

                action.VariableDeclarationModel.DataType = action.Handle;

                foreach (var usage in graphModel.FindUsages(action.VariableDeclarationModel))
                    usage.UpdateTypeFromDeclaration();

                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            }

            return previousState;
        }

        static State UpdateExposed(State previousState, UpdateExposedAction action)
        {
            Undo.RegisterCompleteObjectUndo(action.VariableDeclarationModel, "Update Exposed");
            action.VariableDeclarationModel.IsExposed = action.Exposed;

            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State UpdateTypeRank(State previousState, UpdateTypeRankAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)((GraphModel)previousState.CurrentGraphModel);

            TypeHandle newType = action.IsArray
                ? action.VariableDeclarationModel.DataType.MakeVsArrayType(graphModel.Stencil)
                : action.VariableDeclarationModel.DataType.GetGenericArguments(graphModel.Stencil).Any()
                    ? action.VariableDeclarationModel.DataType.GetVsArrayElementType(graphModel.Stencil)
                    : action.VariableDeclarationModel.DataType;

            Undo.RegisterCompleteObjectUndo(action.VariableDeclarationModel, "Update Type Rank");
            action.VariableDeclarationModel.DataType = newType;

            foreach (var usage in graphModel.FindUsages(action.VariableDeclarationModel))
                usage.UpdateTypeFromDeclaration();

            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State UpdateTooltip(State previousState, UpdateTooltipAction action)
        {
            Undo.RegisterCompleteObjectUndo(action.VariableDeclarationModel, "Update Tooltip");
            action.VariableDeclarationModel.Tooltip = action.Tooltip;

            return previousState;
        }
    }
}
