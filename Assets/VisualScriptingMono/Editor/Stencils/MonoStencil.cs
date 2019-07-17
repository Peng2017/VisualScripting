using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Compilation;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class MonoStencil : ClassStencil
    {

        [MenuItem("Assets/Create/Visual Script/Mono Graph")]
        public static void CreateEcsGraph() {
            VseWindow.CreateGraphAsset<MonoStencil>();
        }

        public override ITranslator CreateTranslator() {
            return new RoslynMonoTranslator(this);
        }

        public override Type GetBaseClass() {
            return typeof(MonoBehaviour);
        }
    }
    public class MonoBuilder : IBuilder
    {
        public void Build(IEnumerable<GraphAssetModel> vsGraphAssetModels, Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished) {
            VseUtility.RemoveLogEntries();
            CancellationToken token = CancellationToken.None;
            foreach (GraphAssetModel vsGraphAssetModel in vsGraphAssetModels) {
                VSGraphModel graphModel = (VSGraphModel)vsGraphAssetModel.GraphModel;
                var t = graphModel.Stencil.CreateTranslator();

                try {
                    // important for codegen, otherwise most of it will be skipped
                    graphModel.Stencil.PreProcessGraph(graphModel);
                    var result = t.TranslateAndCompile(graphModel, AssemblyType.Source, CompilationOptions.Default);
                    var graphAssetPath = AssetDatabase.GetAssetPath(vsGraphAssetModel);
                    foreach (var error in result.errors)
                        VseUtility.LogSticky(LogType.Error, LogOption.None, error.ToString(), graphAssetPath, vsGraphAssetModel.GetInstanceID());
                }
                catch (Exception e) {
                    Debug.LogWarning(e);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}

