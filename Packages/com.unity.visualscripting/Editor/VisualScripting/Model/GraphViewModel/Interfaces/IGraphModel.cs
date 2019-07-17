using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphModel : IDisposable
    {
        string Name { get; }
        IGraphAssetModel AssetModel { get; }
        IReadOnlyList<INodeModel> NodeModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        string GetAssetPath();
        IEnumerable<IEdgeModel> GetEdgesConnections(IPortModel portModel);
        IEnumerable<IPortModel> GetConnections(IPortModel portModel);
        Stencil Stencil { get; }
        string FriendlyScriptName { get; }
        IGraphChangeList LastChanges { get; }
        void ResetChanges();
        void CleanUp();
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class IGraphModelExtensions
    {
        public static bool HasAnyTopologyChange(this IGraphModel graph)
        {
            return graph?.LastChanges != null && graph.LastChanges.HasAnyTopologyChange();
        }
    }
}
