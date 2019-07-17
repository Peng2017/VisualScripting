using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class NodeReducers
    {
        public static void Register(Store store)
        {
            store.Register<DisconnectNodeAction>(DisconnectNode);
            store.Register<BypassNodeAction>(BypassNode);
            store.Register<ChangeNodeColorAction>(ChangeNodeColor);
            store.Register<ResetNodeColorAction>(ResetNodeColor);
            store.RegisterAsync<CreateNodeFromSearcherAction>(CreateNodeFromSearcher);
            store.Register<RefactorConvertToFunctionAction>(RefactorConvertToFunction);
            store.Register<RefactorExtractMacroAction>(RefactorExtractMacro);
            store.Register<RefactorExtractFunctionAction>(RefactorExtractFunction);
            store.Register<CreateMacroRefAction>(CreateMacroRefNode);
        }

        static async Task<State> CreateNodeFromSearcher(State previousState, CreateNodeFromSearcherAction action)
        {
            Func<GraphNodeCreationData, IGraphElementModel[]> createElements =
                await previousState.PromptGraphSearcher(action.MousePosition, action.CancellationTokenSource.Token);

            IGraphElementModel[] elementModels = createElements?.Invoke(
                new GraphNodeCreationData(action.GraphModel, action.GraphPosition));

            if (elementModels == null)
                return previousState;

            if(elementModels.FirstOrDefault() is INodeModel node)
                AnalyticsHelper.Instance.SetLastNodeCreated(((Object)node).GetInstanceID(), node.Title);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State DisconnectNode(State previousState, DisconnectNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            foreach (INodeModel nodeModel in action.NodeModels)
            {
                var portModels = nodeModel.InputPortModels.Union(nodeModel.OutputPortModels);
                var edgeModels = graphModel.GetEdgesConnections(portModels);

                graphModel.DeleteEdges(edgeModels);
            }

            return previousState;
        }

        static State BypassNode(State previousState, BypassNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.BypassNodes(action.NodeModels);

            return previousState;
        }

        static State ChangeNodeColor(State previousState, ChangeNodeColorAction action)
        {
            foreach (var nodeModel in action.NodeModels)
            {
                ((NodeModel)nodeModel).ChangeColor(action.Color);
            }
            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }

        static State ResetNodeColor(State previousState, ResetNodeColorAction action)
        {
            foreach (INodeModel nodeModel in action.NodeModels)
            {
                ((NodeModel)nodeModel).HasUserColor = false;
            }

            // TODO: Should not be topology
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorConvertToFunction(State previousState, RefactorConvertToFunctionAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            var newFunction = graphModel.ConvertNodeToFunction(action.NodeToConvert);
            previousState.EditorDataModel.ElementModelToRename = newFunction;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorExtractFunction(State previousState, RefactorExtractFunctionAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            var newFunction = graphModel.ExtractNodesAsFunction(action.Selection);
            previousState.EditorDataModel.ElementModelToRename = newFunction;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorExtractMacro(State previousState, RefactorExtractMacroAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            INodeModel newMacro;

            using (new AssetWatcher.Scope())
            {
                string assetName = string.IsNullOrEmpty(action.MacroPath)
                    ? null
                    : Path.GetFileNameWithoutExtension(action.MacroPath);
                VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create(assetName, action.MacroPath, typeof(VSGraphAssetModel));
                var macroGraph = graphAssetModel.CreateVSGraph<MacroStencil>(assetName);

                // A MacroStencil cannot be a parent stencil, so use its parent instead
                var parentGraph = graphModel.Stencil is MacroStencil macroStencil ? macroStencil.ParentType : graphModel.Stencil.GetType();
                ((MacroStencil)macroGraph.Stencil).ParentType = parentGraph;
                Utility.SaveAssetIntoObject(macroGraph, graphAssetModel);

                newMacro = graphModel.ExtractNodesAsMacro(macroGraph, action.Position, action.Selection);
            }
            previousState.EditorDataModel.ElementModelToRename = newMacro;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State CreateMacroRefNode(State previousState, CreateMacroRefAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateMacroRefNode((VSGraphModel)action.GraphModel, action.Position);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology/*, createdModel*/); // TODO support in partial rebuild
            return previousState;
        }
    }
}
