using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class Group : Experimental.GraphView.Group, IHasGraphElementModel, ICustomColor, IMovable
    {
        readonly Store m_Store;
        public readonly IGroupNodeModel model;
        readonly GraphView m_GraphView;

        public IGraphElementModel GraphElementModel => model;

        bool m_Populating;

        public Group(IGroupNodeModel model, Store store, GraphView graphView)
        {
            capabilities |= Capabilities.Deletable;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Group.uss"));

            this.model = model;
            m_Store = store;
            m_GraphView = graphView;

            this.MandatoryQ(name: "contentContainerPlaceholder").ElementAt(1).pickingMode = PickingMode.Ignore; // TODO hack for 0.4, fix this in graphview or something

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuEvent));

            base.title = model.Title;
        }

        internal void Populate()
        {
            m_Populating = true;
            // Find element matching to the grouped models, and add them to this visual group.
            var elements = m_GraphView.graphElements
                .ToList()
                .Where(x => x is IHasGraphElementModel elementModel && model.NodeModels.Contains(elementModel.GraphElementModel));

            foreach (var element in elements)
            {
                AddElement(element);
            }

            // Group position is set by it's content... unless there's no content.
            if (!elements.Any())
                SetPosition(new Rect(model.Position, Vector2.zero));

            m_Populating = false;
        }

        public void UpdatePinning()
        {
            foreach (var element in containedElements.Cast<IMovable>())
            {
                element.UpdatePinning();
            }
        }

        // Need this empty override so it does nothing (the base class does something we don't want).
        public override void UpdatePresenterPosition()
        {
        }

        public bool NeedStoreDispatch => false;

        public override bool AcceptsElement(GraphElement element, ref string reasonWhyNotAccepted)
        {
            if (!base.AcceptsElement(element, ref reasonWhyNotAccepted))
                return false;

            if (!(element is TokenDeclaration
                || element is IVisualScriptingField
                || (element is IHasGraphElementModel g && g.GraphElementModel is INodeModel)))
            {
                reasonWhyNotAccepted = "Groups don't support element " + element.GetType().Name;
                return false;
            }

            return true;
        }

        internal void AddElementForRebuild(GraphElement element)
        {
            bool wasPopulating = m_Populating;
            m_Populating = true; // disable creating node models when adding elements
            AddElement(element);
            m_Populating = wasPopulating;
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            List<GraphElement> elementsList = elements.ToList();
            base.OnElementsAdded(elementsList);
            if (!m_Populating)
            {
                INodeModel[] nodeModels = elementsList.OfType<IHasGraphElementModel>().Select(e => e.GraphElementModel).OfType<INodeModel>().ToArray();

                List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate =
                    DragAndDropHelper.ExtractVariablesFromDroppedElements(elementsList,
                        (VseGraphView)m_GraphView,
                        Event.current.mousePosition);
                if (variablesToCreate.Any())
                {
                    if (nodeModels.Any(e => !(e is IVariableModel)))
                    {
                        // fail because in the current setup this would mean dispatching several actions
                        throw new ArgumentException("Unhandled case, dropping blackboard/variables fields and nodes at the same time");
                    }
                    m_Store.Dispatch(new CreateVariableNodesAction(variablesToCreate, model));
                }
                else
                {
                    Node firstStackedNode = elementsList.OfType<Node>().FirstOrDefault(n => n.IsInStack && n.Stack == null && n.model != null);
                    m_Store.Dispatch(new AddToGroupNodeAction(model, firstStackedNode?.layout.position ?? Vector2.negativeInfinity, nodeModels));

                    // We now need to remove all newly added nodes that were actually added to a stack (and thus not directly to a group).
                    RemoveElementsWithoutNotification(elementsList.OfType<Node>().Where(n => n.IsInStack));
                }
            }
        }

        public void SetColor(Color c)
        {
            this.MandatoryQ("headerContainer").style.backgroundColor = c;
        }

        // TODO: Should eventually be an IRenamable, but since the rename mechanism of the Group is not the same as
        // the rest of the elements, we have to handle our own renaming.
        protected override void OnGroupRenamed(string oldName, string newName)
        {
            m_Store.Dispatch(new RenameGroupNodeAction(model, newName));
        }

        protected virtual void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            m_GraphView.BuildContextualMenu(evt);
        }
    }
}
