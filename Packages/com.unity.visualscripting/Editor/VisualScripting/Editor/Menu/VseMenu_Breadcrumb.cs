using System;
using UnityEditor.EditorCommon.UIElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        VisualElement m_BreadcrumbButton;
        Breadcrumb m_Breadcrumb;

//        string m_CurrentGraph = "";

        void CreateBreadcrumbMenu()
        {
            m_BreadcrumbButton = this.MandatoryQ("breadcrumbButton");
            m_BreadcrumbButton.tooltip = "Click to navigate to other scripts";
            m_BreadcrumbButton.AddManipulator(new Clickable(OnBreadcrumbButtonClicked));
            m_Breadcrumb = this.MandatoryQ<Breadcrumb>("breadcrumb");
            OnAssetNameChanged += _ => UpdateBreadcrumbMenu(m_Store.GetState().CurrentGraphModel != null);
        }

        void UpdateBreadcrumbMenu(bool isEnabled)
        {
            m_BreadcrumbButton.SetEnabled(isEnabled);
            m_Breadcrumb.SetEnabled(isEnabled);

            State state = m_Store.GetState();
            IGraphModel graphModel = state.CurrentGraphModel;

            int i = 0;
            for (; i < state.EditorDataModel.PreviousGraphModels.Count; i++)
            {
                GraphModel graphToLoad = state.EditorDataModel.PreviousGraphModels[i];
                string label = graphToLoad ? graphToLoad.FriendlyScriptName : "<Unknown>";
                int i1 = i;
                m_Breadcrumb.CreateOrUpdateItem(i, label, () =>
                {
                    while (state.EditorDataModel.PreviousGraphModels.Count > i1)
                        state.EditorDataModel.PreviousGraphModels.RemoveAt(state.EditorDataModel.PreviousGraphModels.Count - 1);
                    m_Store.Dispatch(new LoadGraphAssetAction(graphToLoad.GetAssetPath(), loadType: LoadGraphAssetAction.Type.KeepHistory));
                });
            }

            string newCurrentGraph = graphModel?.FriendlyScriptName;
            if (newCurrentGraph != null)
                m_Breadcrumb.CreateOrUpdateItem(i++, newCurrentGraph, null);

            object boundObject = state.EditorDataModel.BoundObject;
            string newBoundObjectName = boundObject?.ToString();
            if (newBoundObjectName != null)
                m_Breadcrumb.CreateOrUpdateItem(i++, newBoundObjectName, null);

            m_Breadcrumb.TrimItems(i);
        }

        void OnBreadcrumbButtonClicked()
        {
            GenericMenu scriptMenu = new GenericMenu();
//            var selectionScriptsCount = 0;
//
//            if (m_Store.GetState().editorDataModel.boundObject != null)
//            {
//                VisualBehaviour[] selectionScripts = VseUtility.GetVisualScriptsFromGameObject(m_Store.GetState().editorDataModel.boundObject);
//                foreach (VisualBehaviour script in selectionScripts)
//                {
//                    string assetPath = VseUtility.GetAssetPathFromComponent(script);
//                    string menuText = Path.GetFileNameWithoutExtension(assetPath);
//                    scriptMenu.AddItem(new GUIContent(menuText), menuText == m_CurrentGraph, arg =>
//                    {
//                        var component = (VisualBehaviour)arg;
//                        m_Store.Dispatch(new LoadGraphAssetAction(VseUtility.GetAssetPathFromComponent(component), component));
//                    }, script);
//                }
//
//                selectionScriptsCount = selectionScripts.Length;
//            }
//
//            VisualBehaviour[] allScripts = VseUtility.GetVisualScriptsInScene();
//            if (allScripts.Length > 0 && selectionScriptsCount > 0)
//                scriptMenu.AddSeparator(string.Empty);
//
//            foreach (var script in allScripts)
//            {
//                string assetPath = VseUtility.GetAssetPathFromComponent(script);
//                string objectName = script.name;
//                string menuText = objectName + "/" + Path.GetFileNameWithoutExtension(assetPath);
//                scriptMenu.AddItem(new GUIContent(menuText), menuText == m_CurrentGraph, arg =>
//                {
//                    var component = (VisualBehaviour)arg;
//                    m_Store.Dispatch(new LoadGraphAssetAction(VseUtility.GetAssetPathFromComponent(component), component));
//                }, script);
//            }

//            if (allScripts.Length + selectionScriptsCount == 0)
//                scriptMenu.AddDisabledItem(VseStyles.noScriptsInSceneText);

            scriptMenu.ShowAsContext();
        }
    }
}
