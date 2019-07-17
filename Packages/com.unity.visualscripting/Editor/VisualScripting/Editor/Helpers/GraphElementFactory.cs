using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface INodeBuilder
    {
        GraphView GraphView { get; }
    }

    public class NodeBuilder : INodeBuilder
    {
        public GraphView GraphView { get; set; }
    }

    static class GraphElementFactory
    {
        [CanBeNull]
        internal static GraphElement CreateUI(GraphView graphView, Store store, IGraphElementModel model)
        {
            if (model == null)
            {
                Debug.LogError("GraphElementFactory could not create node because of a null reference model.");
                return null;
            }

            var ext = ModelUtility.ExtensionMethodCache<INodeBuilder>.GetExtensionMethod(
                model.GetType(),
                FilterMethods,
                KeySelector
            );

            if (ext != null)
            {
                var nodeBuilder = new NodeBuilder { GraphView = graphView };
                var graphElement = (GraphElement)ext.Invoke(null, new object[] { nodeBuilder, store, model });
                if (model is INodeModel nodeModel && nodeModel.HasUserColor)
                    (graphElement as ICustomColor)?.SetColor(nodeModel.Color);

                return graphElement;
            }

            Debug.LogError($"GraphElementFactory doesn't know how to create a node of type: {model.GetType()}");
            return null;
        }

        static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[2].ParameterType;
        }

        static bool FilterMethods(MethodInfo x)
        {
            if (x.ReturnType != typeof(GraphElement))
                return false;

            var parameters = x.GetParameters();
            return parameters.Length == 3 && parameters[1].ParameterType == typeof(Store);
        }
    }
}
