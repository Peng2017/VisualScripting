using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests
{
    public enum TestingMode { Action, UndoRedo }
    [PublicAPI]
    enum GroupingMode { Grouped, Ungrouped }

    class TestEditorDataModel : IEditorDataModel
    {
        public UpdateFlags UpdateFlags { get; private set; }
        public IGraphElementModel ElementModelToRename { get; set; }
        public int NodeToFrameInstanceId { get; set; } = -1;
        public int CurrentGraphIndex => 0;
        public VSPreferences Preferences { get; }  = new VSPreferences();

        public object BoundObject { get; set; }

        public List<GraphModel> PreviousGraphModels { get; } = new List<GraphModel>();
        public int UpdateCounter { get; set; }
        public Rect BlackboardPosition { get; set; }

        public void SetUpdateFlag(UpdateFlags flag)
        {
            UpdateFlags = flag;
        }

        public void RequestCompilation(RequestCompilationOptions options)
        {
            throw new NotImplementedException();
        }

        public bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            return false;
        }

        public void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select)
        {
        }

        public void ClearElementsToSelectUponCreation()
        {
        }

        public bool ShouldExpandBlackboardRowUponCreation(string rowName)
        {
            return false;
        }

        public void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand)
        {
        }

        public bool ShouldActivateElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            return false;
        }

        public void ActivateElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool activate)
        {
        }

        public bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField)
        {
            return false;
        }

        public void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand)
        {
        }

        public IPluginRepository PluginRepository { get; } = new TestPluginRepository();

        class TestPluginRepository : IPluginRepository
        {
            public void RegisterPlugins(CompilationOptions getCompilationOptions) {}
            public void UnregisterPlugins() {}
        }
    }

    class TestState : State
    {
        public TestState(IEditorDataModel dataModel)
            : base(dataModel)
        {
        }

        public TestState()
            :this(new TestEditorDataModel())
        {
        }

        Type m_QueryTypeMockResult = null;
        public Func<GraphNodeCreationData, IGraphElementModel[]> queryGraphNodeModelMockResult;
        public Func<StackNodeCreationData, IGraphElementModel[]> queryStackNodeModelMockResult;

        public virtual Task<Type> QueryType<T>(Vector2 showAtPosition)
        {
            return Task.FromResult(m_QueryTypeMockResult);
        }

        protected internal override Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptEdgeSearcher(
            IEdgeModel edgeModel, Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryGraphNodeModelMockResult);
        }

        protected internal override Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptOutputToGraphSearcher(
            IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryGraphNodeModelMockResult);
        }

        protected internal override Task<Func<StackNodeCreationData, IGraphElementModel[]>> PromptOutputToStackSearcher(
            IStackModel stackModel, IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryStackNodeModelMockResult);
        }

        protected internal override Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptInputToGraphSearcher(
            IPortModel portModel, Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryGraphNodeModelMockResult);
        }

        protected internal override Task<Func<StackNodeCreationData, IGraphElementModel[]>> PromptStackSearcher(
            IStackModel stackModel, Vector2 position, CancellationToken ct, ISearcherAdapter adapter = null)
        {
            return Task.FromResult(queryStackNodeModelMockResult);
        }

        protected internal override Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptGraphSearcher(
            Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryGraphNodeModelMockResult);
        }

        protected internal override Task<Func<GraphNodeCreationData, IGraphElementModel[]>> PromptGroupSearcher(
            Vector2 position, CancellationToken ct)
        {
            return Task.FromResult(queryGraphNodeModelMockResult);
        }
    }

    class TestStore : Store
    {
        public TestStore(State initialState)
            : base(initialState, Options.TrackUndoRedo)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public override void Dispose()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            Profiler.BeginSample("TestStore_UndoRedo");
            InvokeStateChanged();
            Profiler.EndSample();
        }

        public override void Dispatch<TAction>(TAction action)
        {
            base.Dispatch(action);
            Update();
            InvokeStateChanged();
        }
    }

    [SetUpFixture]
    class SetUpFixture
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTest()
        {
            AssetWatcher.disabled = true;
        }

        [OneTimeTearDown]
        public void RunAfterAllTests()
        {
            AssetWatcher.disabled = false;
        }
    }

    [Category("VSEditor")]
    [PublicAPI]
    public abstract class BaseFixture
    {
        protected Store m_Store;
        protected const string k_GraphPath = "Assets/test.asset";

        protected VSGraphModel GraphModel => ((VSGraphModel)m_Store.GetState().CurrentGraphModel);
        protected Stencil Stencil => GraphModel.Stencil;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual bool WriteOnDisk => false;
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        internal static void AssumePreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (AssertionException e)
            {
                var inconclusiveException = new InconclusiveException(e.Message, e);
                throw inconclusiveException;
            }
        }

        internal static void AssertPreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (InconclusiveException e)
            {
                var inconclusiveException = new AssertionException(e.Message, e);
                throw inconclusiveException;
            }
        }

        protected void TestPrereqActionPostreq<T>(TestingMode mode, Action checkReqs, Func<T> provideAction, Action checkPostReqs) where T : IAction
        {
            T action;
            switch (mode)
            {
                case TestingMode.Action:
                    checkReqs();
                    action = provideAction();
                    m_Store.Dispatch(action);

                    checkPostReqs();
                    break;
                case TestingMode.UndoRedo:
                    Undo.IncrementCurrentGroup();

                    AssumePreviousTest(() =>
                    {
                        checkReqs();
                        action = provideAction();
                        m_Store.Dispatch(action);
                        checkPostReqs();
                    });

                    Undo.IncrementCurrentGroup();

                    Undo.PerformUndo();
                    AssertPreviousTest(checkReqs);

                    Undo.PerformRedo();
                    AssertPreviousTest(checkPostReqs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        protected void TestPrereqActionPostreq<T>(TestingMode mode, Func<T> checkReqsAndProvideAction, Action checkPostReqs) where T : IAction
        {
            TestPrereqActionPostreq(mode, () => { }, checkReqsAndProvideAction, checkPostReqs);
        }

        [SetUp]
        public virtual void SetUp()
        {
            Profiler.BeginSample("VS Tests SetUp");
            if(WriteOnDisk)
                AssetDatabase.DeleteAsset(k_GraphPath);
            m_Store = new TestStore(new TestState());

            m_Store.StateChanged += AssertIntegrity;

            if (CreateGraphOnStartup)
            {
                m_Store.Dispatch(new CreateGraphAssetAction(CreatedGraphType, "Test", k_GraphPath, writeOnDisk:WriteOnDisk));
                AssumeIntegrity();
            }
            Profiler.EndSample();
        }

        [TearDown]
        public virtual void TearDown()
        {
            m_Store.StateChanged -= AssertIntegrity;
            m_Store.Dispatch(new UnloadGraphAssetAction());
            m_Store.Dispose();
            Profiler.enabled = false;
        }

        protected void AssertIntegrity()
        {
            if(GraphModel)
                Assert.IsTrue(GraphModel.CheckIntegrity(VisualScripting.GraphViewModel.GraphModel.Verbosity.Errors));
        }

        protected void AssumeIntegrity()
        {
            if(GraphModel)
                Assume.That(GraphModel.CheckIntegrity(VisualScripting.GraphViewModel.GraphModel.Verbosity.Errors));
        }

        protected IStackModel GetFunctionModel(string name)
        {
            return GraphModel.StackModels.OfType<FunctionModel>().FirstOrDefault(s => s.name == name);
        }

        protected IEnumerable<StackModel> GetAllStacks()
        {
            return GraphModel.StackModels.Cast<StackModel>();
        }

        protected StackModel GetStack(int index)
        {
            return GetAllStacks().ElementAt(index);
        }

        protected NodeModel GetStackedNode(int stackIndex, int nodeIndex)
        {
            return (NodeModel)GetAllStacks().ElementAt(stackIndex).NodeModels.ElementAt(nodeIndex);
        }

        protected int GetStackCount()
        {
            return GetAllStacks().Count();
        }

        protected IEnumerable<NodeModel> GetAllNodes()
        {
            return GraphModel.NodeModels.Cast<NodeModel>();
        }

        protected NodeModel GetNode(int index)
        {
            return GetAllNodes().ElementAt(index);
        }

        protected int GetNodeCount()
        {
            return GraphModel.NodeModels.Count;
        }

        protected IEnumerable<StackModel> GetAllFloatingStacks()
        {
            return GraphModel.StackModels.Cast<StackModel>().Where(x => !(x is FunctionModel) && x.InputPortModels.All(i => !i.Connected));
        }

        protected StackModel GetFloatingStack(int index)
        {
            return GetAllFloatingStacks().ElementAt(index);
        }

        protected IEnumerable<EdgeModel> GetAllEdges()
        {
            return GraphModel.EdgeModels.Cast<EdgeModel>();
        }

        protected EdgeModel GetEdge(int index)
        {
            return GetAllEdges().ElementAt(index);
        }

        protected int GetEdgeCount()
        {
            return GetAllEdges().Count();
        }

        protected IEnumerable<VariableDeclarationModel> GetAllVariableDeclarations()
        {
            return GraphModel.GraphVariableModels.Cast<VariableDeclarationModel>();
        }

        protected VariableDeclarationModel GetVariableDeclaration(int index)
        {
            return GetAllVariableDeclarations().ElementAt(index);
        }

        protected int GetVariableDeclarationCount()
        {
            return GetAllVariableDeclarations().Count();
        }

        protected IEnumerable<StickyNoteModel> GetAllStickyNotes()
        {
            return GraphModel.StickyNoteModels.Cast<StickyNoteModel>();
        }

        protected StickyNoteModel GetStickyNote(int index)
        {
            return GetAllStickyNotes().ElementAt(index);
        }

        protected int GetStickyNoteCount()
        {
            return GetAllStickyNotes().Count();
        }

        protected IVariableDeclarationModel GetFunctionVariable(IFunctionModel method, string localName)
        {
            return method.FunctionVariableModels.Single(f => f.Title == localName);
        }

        protected IVariableDeclarationModel GetGraphVariableDeclaration(string fieldName)
        {
            return GraphModel.GraphVariableModels.Single(f => f.Title == fieldName);
        }

        protected void AddUsage(IVariableDeclarationModel fieldModel)
        {
            int prevCount = GetFloatingVariableModels(GraphModel).Count();
            m_Store.Dispatch(new CreateVariableNodesAction(fieldModel, Vector2.one));
            Assume.That(GetFloatingVariableModels(GraphModel).Count(), Is.EqualTo(prevCount + 1));
        }

        protected IVariableModel GetFunctionVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.name == fieldName && f.VariableType == VariableType.FunctionVariable);
        }

        protected IVariableModel GetGraphVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.name == fieldName && f.VariableType == VariableType.GraphVariable);
        }

        protected IVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.GraphVariableModels.Count();

            m_Store.Dispatch(new CreateGraphVariableDeclarationAction(fieldName, false, type.GenerateTypeHandle(Stencil)));

            Assert.AreEqual(prevCount + 1, GraphModel.GraphVariableModels.Count());
            IVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.Title, Is.EqualTo(fieldName));
            return decl;
        }

        protected IVariableDeclarationModel CreateFunctionVariableDeclaration<T>(Func<FunctionModel> method, string localName)
        {
            int prevCount = method().FunctionVariableModels.Count();

            m_Store.Dispatch(new CreateFunctionVariableDeclarationAction(method(), localName, typeof(T).GenerateTypeHandle(Stencil)));

            Assert.AreEqual(prevCount + 1, method().FunctionVariableModels.Count());
            IVariableDeclarationModel decl = GetFunctionVariable(method(), localName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.Title, Is.EqualTo(localName));
            return decl;
        }

        protected void EnableUndoRedoModificationsLogging()
        {
            m_Store.Register(a => Debug.Log("Action " + a.GetType().Name));
            // TODO : Undo.postprocessModifications += PostprocessModifications;
        }

        public IEnumerable<VariableNodeModel> GetFloatingVariableModels(IGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<VariableNodeModel>().Where(v => v.OutputPortModels.All(p => !p.Connected));
        }

        protected void DebugLogAllEdges()
        {
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Debug.Log(((NodeModel)edgeModel.InputPortModel.NodeModel).name + "<->" + ((NodeModel)edgeModel.OutputPortModel.NodeModel).name);
            }
        }

        /*private static UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            Func<PropertyModification, string> f = mod =>
                {
                    var o = mod.objectReference is SemanticGraph ? "graph" : mod.objectReference == null ? mod.value : mod.objectReference.GetType().Name;
                    return $"{mod.target}: {o}\n";
                };
            StringBuilder sb = new StringBuilder();
            foreach (var mod in modifications)
            {
                sb.AppendLine($"{mod.previousValue.propertyPath}:\n{f(mod.previousValue)}\n{f(mod.currentValue)}");
            }
            Debug.Log("POSTPROCESSMODIFICATIONS\n" + sb);
            return modifications;
        }*/

        protected IEdgeModel ConnectTo(INodeModel from, int fromIndex, INodeModel to, int toIndex)
        {
            return GraphModel.CreateEdge(to.InputPortModels.ElementAt(toIndex), from.OutputPortModels.ElementAt(fromIndex));
        }
    }
}
