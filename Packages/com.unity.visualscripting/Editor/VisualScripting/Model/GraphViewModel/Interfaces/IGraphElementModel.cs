using System;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphElementModel : ICapabilitiesModel
    {
        IGraphAssetModel AssetModel { get; }
        IGraphModel GraphModel { get; }
        string GetId();
    }

    public interface IUndoRedoAware
    {
        void UndoRedoPerformed();
    }
}
