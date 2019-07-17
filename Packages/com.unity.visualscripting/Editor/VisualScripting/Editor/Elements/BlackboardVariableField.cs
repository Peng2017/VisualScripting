using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public class BlackboardVariableField : BlackboardField, IHighlightable, IRenamable, IVisualScriptingField
    {
        readonly VseGraphView m_GraphView;
        readonly IVariableDeclarationModel m_Model;

        TextField m_TitleTextfield;
#if UNITY_2019_3_OR_NEWER
        Label m_TitleLabel;
#else
        BoundLabel m_TitleLabel;
#endif

        public IGraphElementModel GraphElementModel => userData as IVariableDeclarationModel;
        public IGraphElementModel ExpandableGraphElementModel => null;
        public Store Store { get; }

        public string TitleValue => m_Model.Title.Nicify();

        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement Element => this;

        public IVariableDeclarationModel VariableDeclarationModel => m_Model;

        public void Expand() {}
        public bool CanInstantiateInGraph() => true;

        public override bool IsRenamable()
        {
            var varDeclarationModel = VariableDeclarationModel as VariableDeclarationModel;
            return base.IsRenamable() && varDeclarationModel != null && varDeclarationModel.Capabilities.HasFlag(CapabilityFlags.Renamable);
        }

        public bool Highlighted
        {
            get => highlighted;
            set => highlighted = value;
        }

        public bool IsFramable() => false;

        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => OpenTextEditor;

        public BlackboardVariableField(Store store, IVariableDeclarationModel variableDeclarationModel, VseGraphView graphView)
        {
            Store = store;
            userData = variableDeclarationModel;
            m_Model = variableDeclarationModel;
            m_GraphView = graphView;

            UpdateTitleFromModel();

            typeText = variableDeclarationModel.DataType.GetMetadata(variableDeclarationModel.GraphModel.Stencil).FriendlyName;

            icon = variableDeclarationModel.IsExposed
                ? VisualScriptingIconUtility.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            this.MandatoryQ<Pill>("pill").tooltip = TitleValue;

#if UNITY_2019_3_OR_NEWER
            m_TitleLabel = this.Q<Label>("title-label");
#else
            m_TitleLabel = this.Q<Label>("title-label").ReplaceWithBoundLabel();
#endif
            m_TitleLabel.bindingPath = "m_Name";

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            viewDataKey = variableDeclarationModel.GetId();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_TitleLabel.Bind(new SerializedObject((Object)VariableDeclarationModel));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_TitleLabel.Unbind();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            m_GraphView.HighlightGraphElements();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            m_GraphView.ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel model)
        {
            var variableModel = model as IVariableModel;
            var candidate = model as IVariableDeclarationModel;
            return variableModel != null
                && Equals(variableModel.DeclarationModel, m_Model)
                || Equals(candidate, m_Model);
        }

        public void UpdateTitleFromModel()
        {
            text = VariableDeclarationModel.Title;
        }
    }
}
