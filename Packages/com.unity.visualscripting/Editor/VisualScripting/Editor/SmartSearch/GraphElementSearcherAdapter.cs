using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    abstract class GraphElementSearcherAdapter : SimpleSearcherAdapter
    {
        protected GraphElementSearcherAdapter(string title) : base(title) { }

        /*
        public override void OnItemSelected(SearcherItem item)
        {
            base.OnItemSelected(item);

            // TODO : Uncomment this after 0.5

            SearcherGraphView graphView = SearcherService.GraphView;

            foreach (GraphElement graphElement in graphView.graphElements.ToList())
            {
                graphView.RemoveElement(graphElement);
            }

            if (!detailsPanel.Contains(graphView))
            {
                detailsPanel.Add(graphView);
                graphView.StretchToParentSize();
            }

            IEnumerable<IGraphElementModel> elements = CreateGraphElements(item);
            foreach (INodeModel element in elements.OfType<INodeModel>())
            {
                graphView.AddElement(GraphElementFactory.CreateUI(graphView, graphView.store, element));
            }
        }
         */

        public virtual IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            throw new NotImplementedException();
        }
    }

    class GraphNodeSearcherAdapter : GraphElementSearcherAdapter
    {
        readonly IGraphModel m_GraphModel;

        public GraphNodeSearcherAdapter(IGraphModel graphModel, string title)
            : base(title)
        {
            m_GraphModel = graphModel;
        }

        public override IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem graphItem
                ? graphItem.CreateElements.Invoke(
                    new GraphNodeCreationData(m_GraphModel, Vector2.zero, NodeCreationMode.Orphan))
                : Enumerable.Empty<IGraphElementModel>();
        }
    }

    class StackNodeSearcherAdapter : GraphElementSearcherAdapter
    {
        readonly IStackModel m_StackModel;

        public StackNodeSearcherAdapter(IStackModel stackModel, string title)
            : base(title)
        {
            m_StackModel = stackModel;
        }

        public override IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            return item is StackNodeModelSearcherItem stackItem
                ? stackItem.CreateElements.Invoke(new StackNodeCreationData(m_StackModel, -1, NodeCreationMode.Orphan))
                : Enumerable.Empty<IGraphElementModel>();
        }
    }
}
