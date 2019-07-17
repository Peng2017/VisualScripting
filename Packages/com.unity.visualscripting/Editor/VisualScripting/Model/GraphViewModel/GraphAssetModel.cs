using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel
    {
        [SerializeField]
        GraphModel m_GraphModel;

        public string Name => name;
        public IGraphModel GraphModel => m_GraphModel;

        public static GraphAssetModel Create(string assetName, string assetPath, Type assetTypeToCreate, bool writeOnDisk = true)
        {
            var asset = (GraphAssetModel)CreateInstance(assetTypeToCreate);
            if (!string.IsNullOrEmpty(assetPath) && writeOnDisk)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "");
                if (File.Exists(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.name = assetName;
            return asset;
        }

        [PublicAPI]
        public TGraphType CreateGraph<TGraphType>(string actionName, Type stencilType, bool writeOnDisk = true)
            where TGraphType : GraphModel
        {
            return (TGraphType)CreateGraph(typeof(TGraphType), actionName, stencilType, writeOnDisk);
        }

        [PublicAPI]
        public GraphModel CreateGraph(Type graphTypeToCreate, string graphName, Type stencilType, bool writeOnDisk = true)
        {
            var graphModel = (GraphModel)CreateInstance(graphTypeToCreate);
            graphModel.name = graphName;
            graphModel.AssetModel = this;
            m_GraphModel = graphModel;
            if(writeOnDisk)
                Utility.SaveAssetIntoObject(graphModel, this);
            var stencil = (Stencil)CreateInstance(stencilType);
            graphModel.Stencil = stencil;
            if(writeOnDisk)
                Utility.SaveAssetIntoObject(stencil, this);
            return graphModel;
        }

        public bool IsSameAsset(IGraphAssetModel otherGraphAssetModel)
        {
            return GetHashCode() == otherGraphAssetModel?.GetHashCode();
        }

        public void ShowInInspector()
        {
            Selection.activeObject = this;
        }

        public virtual void Dispose()
        {
             m_GraphModel?.Dispose();
        }
    }
}
