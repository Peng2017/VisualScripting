using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [InitializeOnLoad]
    public class AssetWatcher : AssetPostprocessor
    {
        public static int Version;

        public class Scope : IDisposable
        {
            bool m_PreviousValue;

            public Scope()
            {
                m_PreviousValue = disabled;
                disabled = true;
            }

            public void Dispose()
            {
                disabled = m_PreviousValue;
            }
        }

        public static bool disabled;
        static AssetWatcher s_Instance;
        static AssetWatcher Instance => s_Instance;

        HashSet<string> m_ProjectAssetPaths;

        static AssetWatcher()
        {
            s_Instance = new AssetWatcher();
            Instance.m_ProjectAssetPaths = new HashSet<string>();

            var graphAssetGUIDs = AssetDatabase.FindAssets("t:" + typeof(VSGraphAssetModel).Name);
            foreach (var guid in graphAssetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var graphAssetModel = AssetDatabase.LoadMainAssetAtPath(assetPath) as VSGraphAssetModel;
                if (graphAssetModel)
                {
                    // HACK: should not hardcode the Script suffix
                    Instance.m_ProjectAssetPaths.Add(assetPath);
                }
            }
            // TODO: be smarter
            AssetDatabase.importPackageCompleted += name =>
            {
                EditorReducers.BuildAll(null);
            };
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var vseWindows = (VseWindow[])Resources.FindObjectsOfTypeAll(typeof(VseWindow));

            if (deletedAssets.Any())
            {
                foreach (var deleted in deletedAssets)
                {
                    if (Instance.m_ProjectAssetPaths.Contains(deleted))
                    {
                        foreach (var vseWindow in vseWindows)
                            vseWindow.UnloadGraphIfDeleted();

                        // TODO : Fix for 1st drop. Find a better solution
                        var graphName = Path.GetFileNameWithoutExtension(deleted);
                        if (!string.IsNullOrEmpty(graphName))
                        {
                            var path = Path.Combine(ModelUtility.GetAssemblyRelativePath(), graphName + ".cs");
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                }
            }

            if (movedAssets.Any())
            {
                for (var i = 0; i < movedAssets.Length; ++i)
                {
                    var newAsset = movedAssets[i];
                    var oldAsset = movedFromAssetPaths[i];

                    if (Instance.m_ProjectAssetPaths.Contains(oldAsset))
                    {
                        foreach (var vseWindow in vseWindows)
                            vseWindow.UnloadGraphIfDeleted();

                        // TODO : Fix for 1st drop. Find a better solution
                        var newGraphName = Path.GetFileNameWithoutExtension(newAsset);
                        var oldGraphName = Path.GetFileNameWithoutExtension(oldAsset);

                        // if the Graph has been renamed, not just moved
                        if (!string.IsNullOrEmpty(newGraphName) && newGraphName != oldGraphName)
                        {
                            var path = Path.Combine(ModelUtility.GetAssemblyRelativePath(), oldGraphName + ".cs");
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                }
            }

            var importedGraphAssets = importedAssets.Where(AssetAtPathIsVsGraphAsset).ToList();
            Instance.m_ProjectAssetPaths.AddRange(importedGraphAssets);
            if (importedGraphAssets.Any())
                Version++;
        }

        public static bool AssetAtPathIsVsGraphAsset(string path)
        {
            if (Path.GetExtension(path) != ".asset")
                return false;

            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(VSGraphAssetModel);
        }
    }

    public class AssetModificationWatcher : AssetModificationProcessor
    {
        public static int Version;

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(p => Path.GetExtension(p) == ".unity"
                && !string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(p))))
            {
                // Build All VS, before returning.
                EditorReducers.BuildAll(null);
            }

            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetWatcher.AssetAtPathIsVsGraphAsset(assetPath))
                Version++;
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetWatcher.AssetAtPathIsVsGraphAsset(sourcePath))
                Version++;
            return AssetMoveResult.DidNotMove;
        }
    }
}
