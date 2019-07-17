using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class State : IDisposable
    {
        public IGraphAssetModel AssetModel { get; set; }
        public IGraphModel CurrentGraphModel => AssetModel?.GraphModel;

        public VSPreferences Preferences => EditorDataModel?.Preferences;

        public IEditorDataModel EditorDataModel { get; }
        public ICompilationResultModel CompilationResultModel { get; private set; }

        public class DebuggingDataModel
        {
            public INodeModel nodeModel;
            public Debugger.VisualScriptingFrameTrace.StepType type;
            public string text;
            public Dictionary<INodeModel, string> values;
        }
        public List<DebuggingDataModel> DebuggingData { get; set; }

        public int currentTracingFrame;
        public int currentTracingStep;
        public int maxTracingStep;
        public bool requestNodeAlignment;

        public enum UIRebuildType                             // for performance debugging purposes
        {
            None, Partial, Full
        }
        public string LastDispatchedActionName { get; set; }    // ---
        public UIRebuildType lastActionUIRebuildType;           // ---

        public State(IEditorDataModel editorDataModel)
        {
            CompilationResultModel = new CompilationResultModel();
            EditorDataModel = editorDataModel;
            currentTracingStep = -1;
        }

        public void Dispose()
        {
            UnloadCurrentGraphAsset();
            CompilationResultModel = null;
            DebuggingData = null;
        }

        public void UnloadCurrentGraphAsset()
        {
            AssetModel?.Dispose();
            AssetModel = null;
            //TODO: should not be needed ?
            EditorDataModel?.PluginRepository?.UnregisterPlugins();
        }

        protected internal virtual Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptGraphSearcher(Vector2 position,
            CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetGraphSearcherFilter();
            var adapter = new GraphNodeSearcherAdapter(CurrentGraphModel, "Add a graph Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetReferenceItemsSearcherDatabases())
                .ToList();

            return SearcherService.ShowGraphNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<StackNodeCreationData, IGraphElementModel[]>> PromptStackSearcher(
            IStackModel stackModel, Vector2 position, CancellationToken ct, ISearcherAdapter searcherAdapter = null)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetStackSearcherFilter(stackModel);
            var adapter = searcherAdapter ?? new StackNodeSearcherAdapter(stackModel, "Add a stack Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetReferenceItemsSearcherDatabases())
                .ToList();

            return SearcherService.ShowStackNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptGroupSearcher(
            Vector2 position, CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetGroupSearcherFilter();
            var adapter = new GraphNodeSearcherAdapter(CurrentGraphModel, "Add a Node to the group");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetReferenceItemsSearcherDatabases())
                .ToList();

            return SearcherService.ShowGraphNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptOutputToGraphSearcher(
            IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = new GraphNodeSearcherAdapter(
                CurrentGraphModel,
                $"Choose an action for {portModel.DataType.GetMetadata(stencil).FriendlyName}"
            );
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetTypeMembersSearcherDatabases(portModel.DataType))
                .ToList();

            return SearcherService.ShowGraphNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<StackNodeCreationData, IGraphElementModel[]>> PromptOutputToStackSearcher(
            IStackModel stackModel, IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetOutputToStackSearcherFilter(portModel, stackModel);
            var adapter = new StackNodeSearcherAdapter(stackModel, "Add a stack Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetTypeMembersSearcherDatabases(portModel.DataType))
                .ToList();

            return SearcherService.ShowStackNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptInputToGraphSearcher(
            IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            IStackModel stackModel = portModel.NodeModel.ParentStackModel;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetInputToGraphSearcherFilter(portModel);
            var adapter = new GraphNodeSearcherAdapter(CurrentGraphModel, "Pick a data member for this input port");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetGraphVariablesSearcherDatabases(CurrentGraphModel, stackModel?.OwningFunctionModel))
                .ToList();

            return SearcherService.ShowGraphNodes(dbs, filter, adapter, position, ct);
        }

        protected internal virtual Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptEdgeSearcher(
            IEdgeModel edgeModel, Vector2 position, CancellationToken ct)
        {
            Stencil stencil = CurrentGraphModel.Stencil;
            SearcherFilter filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = new GraphNodeSearcherAdapter(CurrentGraphModel, "Insert Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetTypeMembersSearcherDatabases(edgeModel.OutputPortModel.DataType))
                .ToList();

            return SearcherService.ShowGraphNodes(dbs, filter, adapter, position, ct);
        }

        public void RegisterReducers(Store store, Action clearRegistrations)
        {
            clearRegistrations();
            store.RegisterReducers();
            if (CurrentGraphModel?.Stencil != null)
                CurrentGraphModel?.Stencil.RegisterReducers(store);
        }
    }
}
