using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class BlackboardMacroProvider : IBlackboardProvider
    {
        const string k_ClassLibrarySubTitle = "Macro Library";

        const int k_InputPortDeclarationsSection = 0;
        const string k_InputPortDeclarationsSectionTitle = "Input Ports";

        const int k_OutputPortDeclarationsSection = 1;
        const string k_OutputPortDeclarationsSectionTitle = "Output Ports";

        Stencil m_Stencil;
        static GUIContent[] s_Options;

        public BlackboardMacroProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public IEnumerable<BlackboardSection> CreateSections()
        {
            yield return new BlackboardSection {title = k_InputPortDeclarationsSectionTitle };
            yield return new BlackboardSection {title = k_OutputPortDeclarationsSectionTitle };
        }

        public string GetSubTitle()
        {
            return k_ClassLibrarySubTitle;
        }

        public void AddItemRequested<TAction>(Store store, TAction _) where TAction : IAction
        {
            var options = new[] { new GUIContent("Input"), new GUIContent("Output") };

            EditorUtility.DisplayCustomMenu(
                new Rect(Event.current.mousePosition, Vector2.one),
                options,
                -1,
                (data, opts, i) =>
                {
                    bool isInput = i == 0;
                    string name = isInput ? "Input" : "Output";
                    ModifierFlags modifierFlags = isInput ? ModifierFlags.ReadOnly : ModifierFlags.WriteOnly;
                    store.Dispatch(new CreateGraphVariableDeclarationAction(name, true, typeof(float).GenerateTypeHandle(m_Stencil), modifierFlags));
                },
                null);
        }

        public void MoveItemRequested(Store store, int index, VisualElement field)
        {
            if (field is BlackboardVariableField blackboardField)
                store.Dispatch(new ReorderGraphVariableDeclarationAction(blackboardField.VariableDeclarationModel, index));
        }

        public void RebuildSections(Blackboard blackboard)
        {
            var currentGraphModel = (VSGraphModel)blackboard.Store.GetState().CurrentGraphModel;

            blackboard.ClearContents();

            if (blackboard.Sections != null && blackboard.Sections.Count > 1)
            {
                blackboard.Sections[k_InputPortDeclarationsSection].title = k_InputPortDeclarationsSectionTitle;
                blackboard.Sections[k_OutputPortDeclarationsSection].title = k_OutputPortDeclarationsSectionTitle;
            }

            if (currentGraphModel.NodeModels == null)
                return;

            foreach (VariableDeclarationModel declaration in currentGraphModel.VariableDeclarations)
            {
                var blackboardField = new BlackboardVariableField(blackboard.Store, declaration, blackboard.GraphView);
                if (blackboard.Sections != null)
                {
                    switch (declaration.Modifiers)
                    {
                        case ModifierFlags.ReadOnly:
                            blackboard.Sections[k_InputPortDeclarationsSection].Add(blackboardField);
                            break;
                        case ModifierFlags.WriteOnly:
                            blackboard.Sections[k_OutputPortDeclarationsSection].Add(blackboardField);
                            break;
                    }
                }
                blackboard.GraphVariables.Add(blackboardField);
            }
        }

        public void DisplayAppropriateSearcher(Vector2 mousePosition, VseGraphView graphView)
        {
            VisualElement picked = graphView.panel.Pick(mousePosition);
            while (picked != null && !(picked is IVisualScriptingField))
                picked = picked.parent;

            // optimization: stop at the first IVsBlackboardField, but still exclude BlackboardThisFields
            if (picked != null && picked is BlackboardVariableField field)
                graphView.window.DisplayTokenDeclarationSearcher((VariableDeclarationModel)field.VariableDeclarationModel, mousePosition);
            else
                graphView.window.DisplayAddVariableSearcher(mousePosition);
        }

        public bool CanAddItems => true;
        public void BuildContextualMenu(DropdownMenu evtMenu, VisualElement visualElement, Store store, Vector2 mousePosition) { }
    }
}
