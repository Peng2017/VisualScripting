using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateGraphAssetAction : IAction
    {
        public readonly Type GraphType;
        public readonly Type AssetType;
        public readonly string Name;
        public readonly string AssetPath;
        public readonly GameObject Instance;
        public readonly bool WriteOnDisk;
        public readonly IGraphTemplate GraphTemplate;

        public CreateGraphAssetAction(Type graphType, string name = "", string assetPath = "", GameObject instance = null, bool writeOnDisk = true, IGraphTemplate graphTemplate = null)
        : this(graphType, typeof(VSGraphAssetModel), name, assetPath, instance, writeOnDisk, graphTemplate)
        {
        }

        public CreateGraphAssetAction(Type graphType, Type assetType, string name = "", string assetPath = "", GameObject instance = null, bool writeOnDisk = true, IGraphTemplate graphTemplate = null)
        {
            GraphType = graphType;
            AssetType = assetType;
            Name = name;
            AssetPath = assetPath;
            Instance = instance;
            WriteOnDisk = writeOnDisk;
            GraphTemplate = graphTemplate;
        }
    }

    public class LoadGraphAssetAction : IAction
    {
        public enum Type
        {
            Replace,
            PushOnStack,
            KeepHistory
        }

        public readonly string AssetPath;
        public readonly Type LoadType;

        public readonly bool AlignAfterLoad;

        public LoadGraphAssetAction(string assetPath, bool alignAfterLoad = false, Type loadType = Type.Replace)
        {
            AssetPath = assetPath;
            LoadType = loadType;
            AlignAfterLoad = alignAfterLoad;
        }
    }

    public class UnloadGraphAssetAction : IAction {}
}
