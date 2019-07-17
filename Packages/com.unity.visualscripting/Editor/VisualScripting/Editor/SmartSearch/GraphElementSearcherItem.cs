using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public struct StackNodeCreationData
    {
        public readonly IStackModel stackModel;
        public readonly int index;
        public readonly NodeCreationMode creationMode;

        public StackNodeCreationData(IStackModel stackModel, int index,
            NodeCreationMode creationMode = NodeCreationMode.GraphRoot)
        {
            this.stackModel = stackModel;
            this.index = index;
            this.creationMode = creationMode;
        }
    }

    public struct GraphNodeCreationData
    {
        public readonly IGraphModel graphModel;
        public readonly Vector2 position;
        public readonly NodeCreationMode creationMode;

        public GraphNodeCreationData(IGraphModel graphModel, Vector2 position,
            NodeCreationMode creationMode = NodeCreationMode.GraphRoot)
        {
            this.graphModel = graphModel;
            this.position = position;
            this.creationMode = creationMode;
        }
    }

    public class GraphNodeModelSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public Func<GraphNodeCreationData, IGraphElementModel[]> CreateElements { get; }
        public ISearcherItemData Data { get; }

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGraphElementModel> createElement,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : base(name, help, children)
        {
            Data = data;
            CreateElements = d => new[] { createElement.Invoke(d) };
        }
    }

    public class StackNodeModelSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public Func<StackNodeCreationData, IGraphElementModel[]> CreateElements { get; }
        public ISearcherItemData Data { get; }

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel[]> createElements,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : base(name, help, children)
        {
            Data = data;
            CreateElements = createElements;
        }

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel> createElement,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : base(name, help, children)
        {
            Data = data;
            CreateElements = d => new[] { createElement.Invoke(d) };
        }
    }
}
