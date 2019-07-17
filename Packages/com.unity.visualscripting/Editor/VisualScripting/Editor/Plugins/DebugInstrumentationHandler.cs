using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Plugins;
using UnityEngine;
using UnityEngine.UIElements;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public class DebugInstrumentationHandler : IRoslynPluginHandler
    {
        const string k_TraceHighlight = "trace-highlight";
        const string k_ExceptionHighlight = "exception-highlight";
        const string k_TraceSecondaryHighlight = "trace-secondary-highlight";
        const int k_UpdateIntervalMs = 10;

        VisualElement m_IconsParent;
        Store m_Store;
        GraphView m_GraphView;
        List<State.DebuggingDataModel> GraphDebuggingData => m_Store.GetState()?.DebuggingData;
        PauseState m_PauseState = PauseState.Unpaused;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        int? m_CurrentFrame;
        int? m_CurrentStep;
        Stopwatch m_Stopwatch;
        bool m_ForceUpdate;

        public void Register(Store store, GraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_Store.StateChanged += OnStateChangeUpdate;
        }

        public void Unregister()
        {
            ClearHighlights();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= OnUpdate;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            m_Store.StateChanged -= OnStateChangeUpdate;
        }

        public  bool RequiresSemanticModel => false;

        public void Apply(ref Microsoft.CodeAnalysis.SyntaxTree syntaxTree, SemanticModel model, CompilationOptions options)
        {
            syntaxTree = new InstrumentForInEditorDebugging().Visit(syntaxTree.GetRoot()).SyntaxTree;
            // TODO handle exceptions again: syntaxTree = new ExceptionHandlingInjection().Visit(syntaxTree.GetRoot()).SyntaxTree;
        }

        void OnStateChangeUpdate()
        {
            m_ForceUpdate = true;
            OnUpdate();
            m_ForceUpdate = false;
        }

        void OnUpdate()
        {
            if (!IsDirty())
                return;

            if (EditorApplication.isPlaying)
                MapDebuggingData();
        }

        void OnEditorPauseStateChanged(PauseState state)
        {
            m_PauseState = state;

            // TODO Save tracing data
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            m_PlayState = state;

            if (m_PlayState == PlayModeStateChange.ExitingPlayMode)
            {
                ClearHighlights();
                m_Stopwatch?.Stop();
                m_Stopwatch = null;
            }
        }

        void MapDebuggingData()
        {
            bool needUpdate = false;
            if (m_Stopwatch == null)
                m_Stopwatch = Stopwatch.StartNew();
            else if (EditorApplication.isPaused || m_Stopwatch.ElapsedMilliseconds > k_UpdateIntervalMs)
            {
                needUpdate = true;
                m_Stopwatch.Restart();
            }

            if (needUpdate)
                ClearHighlights();

            var graph = (Object)m_Store.GetState()?.CurrentGraphModel;
            if (graph == null)
                return;
            var d = Debugger.GetData(m_Store.GetState().currentTracingFrame, graph);
            if (needUpdate && d != null && d.steps.Count > 0)
            {
                m_Store.GetState().maxTracingStep = d.steps.Count - 1;
                m_Store.GetState().DebuggingData = d.steps.Select((x, i) =>
                {
                    var nodeModel = EditorUtility.InstanceIDToObject(x.nodeId) as INodeModel;
                    Dictionary<INodeModel, string> valueRecords = null;
                    if (d.values.TryGetValue(i, out var values))
                    {
                        valueRecords = values.ToDictionary(r => EditorUtility.InstanceIDToObject(r.nodeId) as INodeModel, r => r.readableValue);
                    }

                    return new State.DebuggingDataModel { nodeModel = nodeModel, type = x.type, text = x.exceptionText, values = valueRecords };
                }).ToList();
                HighlightTrace();
            }
        }

        void ClearHighlights()
        {
            VseGraphView gv = (VseGraphView)m_GraphView;
            if (gv?.UIController.ModelsToNodeMapping != null)
            {
                foreach (GraphElement x in gv.UIController.ModelsToNodeMapping.Values)
                {
                    x.RemoveFromClassList(k_TraceHighlight);
                    x.RemoveFromClassList(k_TraceSecondaryHighlight);
                    x.RemoveFromClassList(k_ExceptionHighlight);

                    VseUIController.ClearErrorBadge(x);
                    VseUIController.ClearValue(x);

                    // TODO ugly
                    gv.UIController.DisplayCompilationErrors(gv.store.GetState());
                }
            }
        }

        void HighlightTrace()
        {
            Dictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping =
                ((VseGraphView)m_GraphView).UIController.ModelsToNodeMapping;
            if (GraphDebuggingData != null)
            {
                if (m_Store.GetState().currentTracingStep == -1)
                {
                    foreach (var step in GraphDebuggingData)
                    {
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
                else
                {
                    var step = GraphDebuggingData[m_Store.GetState().currentTracingStep];
                    AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceSecondaryHighlight);
                    DisplayStepValues(step, modelsToNodeUiMapping);

                    for (var i = 0; i < m_Store.GetState().currentTracingStep; i++)
                    {
                        step = GraphDebuggingData[i];
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
            }
        }

        void DisplayStepValues(State.DebuggingDataModel step, Dictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping)
        {
            if (step.values != null)
                foreach (var value in step.values)
                    AddValueToNode(modelsToNodeUiMapping, value.Key, value.Value);
        }

        void AddValueToNode(IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, INodeModel node, string valueReadableValue)
        {
            if (node != null && modelsToNodeUiMapping.TryGetValue(node, out GraphElement ui))
            {
                if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                {
                    var n = (Experimental.GraphView.Node)ui;
                    Port p = n.outputContainer.childCount > 0
                        ? n.outputContainer[0] as Port
                        : null;
                    IBadgeContainer badgeContainer = (IBadgeContainer)n;
                    if (p == null)
                        return;
                    VisualElement cap = p.Q(className: "connectorCap");
                    ((VseGraphView)m_GraphView).UIController.AttachValue(badgeContainer, cap, valueReadableValue, p.portColor, SpriteAlignment.BottomRight);
                }
            }
        }

        void AddStyleClassToModel(State.DebuggingDataModel step, IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, string highlightStyle)
        {
            if (step.nodeModel != null && modelsToNodeUiMapping.TryGetValue(step.nodeModel, out GraphElement ui))
            {
                if (step.type == Debugger.VisualScriptingFrameTrace.StepType.Exception)
                {
                    ui.AddToClassList(k_ExceptionHighlight);

                    if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                    {
                        ((VseGraphView)m_GraphView).UIController.AttachErrorBadge(ui, step.text, SpriteAlignment.TopLeft);
                    }
                }
                else
                {
                    ui.AddToClassList(highlightStyle);
                }
            }
        }

        bool IsDirty()
        {
            bool dirty = m_CurrentFrame != m_Store.GetState().currentTracingFrame
                || m_CurrentStep != m_Store.GetState().currentTracingStep
                || m_ForceUpdate;

            m_CurrentFrame = m_Store.GetState().currentTracingFrame;
            m_CurrentStep = m_Store.GetState().currentTracingStep;

            return dirty;
        }
    }
}
