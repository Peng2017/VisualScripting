using System;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        VisualElement m_SaveAllButton;
        VisualElement m_BuildAllButton;
        VisualElement m_RefreshUIButton;
        VisualElement m_ViewInCodeViewerButton;
        VisualElement m_ResetBlackboardButton;

        void CreateCommonMenu()
        {
            m_SaveAllButton = this.MandatoryQ("saveAllButton");
            m_SaveAllButton.tooltip = "Save All";
            m_SaveAllButton.AddManipulator(new Clickable(OnSaveAllButton));

            m_BuildAllButton = this.MandatoryQ("buildAllButton");
            m_BuildAllButton.tooltip = "Build All";
            m_BuildAllButton.AddManipulator(new Clickable(OnBuildAllButton));

            m_ResetBlackboardButton = this.MandatoryQ("resetBlackboardButton");
            m_ResetBlackboardButton.tooltip = "Reset Blackboard Position & Size";
            m_ResetBlackboardButton.AddManipulator(new Clickable(OnResetBlackboardButton));

            m_RefreshUIButton = this.MandatoryQ("refreshButton");
            m_RefreshUIButton.tooltip = "Refresh UI";
            m_RefreshUIButton.AddManipulator(new Clickable(() => m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All))));

            m_ViewInCodeViewerButton = this.MandatoryQ("viewInCodeViewerButton");
            m_ViewInCodeViewerButton.tooltip = "Code Viewer";
            m_ViewInCodeViewerButton.AddManipulator(new Clickable(OnViewInCodeViewerButton));
        }

        void OnResetBlackboardButton()
        {
            m_GraphView.UIController.ResetBlackboard();
        }

        protected virtual void UpdateCommonMenu(VSPreferences prefs, bool enabled)
        {
            m_SaveAllButton.SetEnabled(enabled);
            m_BuildAllButton.SetEnabled(enabled);
            m_ViewInCodeViewerButton.SetEnabled(enabled);
        }

        static void OnSaveAllButton()
        {
            AssetDatabase.SaveAssets();
        }

        void OnBuildAllButton()
        {
            try
            {
                m_Store.Dispatch(new BuildAllEditorAction());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }

        void OnViewInCodeViewerButton()
        {
            var compilationResult = m_Store.GetState()?.CompilationResultModel?.GetLastResult();
            if (compilationResult == null)
            {
                Debug.LogWarning("Compilation returned empty results");
                return;
            }

            VseUtility.UpdateCodeViewer(show: true, sourceIndex: m_GraphView.window.ToggleCodeViewPhase,
                compilationResult: compilationResult,
                selectionDelegate: lineMetadata =>
                {
                    if (lineMetadata == null)
                        return;

                    int nodeInstanceId = (int)lineMetadata;
                    m_Store.Dispatch(new PanToNodeAction(nodeInstanceId));
                });
        }
    }
}
