using System;
using UnityEditor.EditorCommon;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public partial class VseMenu : VisualElement
    {
        protected readonly Store m_Store;
        readonly VseGraphView m_GraphView;
        readonly VseWindow m_Window;
        Label m_GraphNameBoundLabel;
        public Action<string> OnAssetNameChanged;

        public VseMenu(VseWindow window, Store store, VseGraphView graphView)
        {
            m_Window = window;
            m_Store = store;
            m_GraphView = graphView;
            name = "vseMenu";
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Menu/VseMenu.uss"));

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Menu/VseMenu.uxml").CloneTree(this);
            m_Store.StateChanged += StoreOnStateChanged;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            CreateCommonMenu();
            CreateErrorMenu();
            CreateBreadcrumbMenu();
            CreateTracingMenu();
            CreateOptionsMenu();
        }

        public virtual void UpdateUI()
        {
            VSPreferences prefs = m_Store.GetState().Preferences;
            bool isEnabled = m_Store.GetState().CurrentGraphModel != null;
            UpdateCommonMenu(prefs, isEnabled);
            UpdateErrorMenu(isEnabled);
            UpdateBreadcrumbMenu(isEnabled);
            UpdateTracingMenu(false);
        }

        public void UpdateErrorMenu()
        {
            bool isEnabled = m_Store.GetState().CurrentGraphModel != null;
            UpdateErrorMenu(isEnabled);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_GraphNameBoundLabel = new Label { name = "GraphNameBoundLabel", bindingPath = "m_Name" };
            m_GraphNameBoundLabel.style.display = DisplayStyle.None;
            Add(m_GraphNameBoundLabel);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_GraphNameBoundLabel != null && m_GraphNameBoundLabel.IsBound())
            {
                m_GraphNameBoundLabel.Unbind();
                m_GraphNameBoundLabel = null;
            }
        }

        void StoreOnStateChanged()
        {
            if (m_GraphNameBoundLabel != null)
            {
                if (m_GraphNameBoundLabel.IsBound())
                {
                    m_GraphNameBoundLabel.Unbind();
                }

                IGraphModel graphModel = m_Store.GetState()?.CurrentGraphModel;
                if (graphModel?.AssetModel is GraphAssetModel model)
                {
                    m_GraphNameBoundLabel.Bind(new SerializedObject(model));
                    m_GraphNameBoundLabel.RegisterCallback<ChangeEvent<string>>(OnModelNameChanged);
                }
                else
                {
                    m_GraphNameBoundLabel.UnregisterCallback<ChangeEvent<string>>(OnModelNameChanged);
                }

                void OnModelNameChanged(ChangeEvent<string> evt)
                {
                    var newScriptName = graphModel?.FriendlyScriptName ?? "";
                    OnAssetNameChanged(newScriptName);
                }
            }
        }
    }
}
