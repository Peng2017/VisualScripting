using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScriptingTests.Graph
{
    class GraphSerialization : BaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void LoadGraphActionLoadsCorrectGraph()
        {
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), "test", k_GraphPath));
            AssumeIntegrity();

            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(GraphModel);
            m_Store.Dispatch(new LoadGraphAssetAction(k_GraphPath));
            Assert.AreEqual(k_GraphPath, AssetDatabase.GetAssetPath((Object)GraphModel.AssetModel));
            AssertIntegrity();
        }

        [Test]
        public void CreateGraphActionBuildsValidGraphModel()
        {
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), "test", k_GraphPath));
            AssumeIntegrity();
        }

        [Test]
        public void CreateTestGraphBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraph();
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate:graphTemplate));
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            CreateTestGraphBuildsValidGraphModel();

            VSGraphModel graph = AssetDatabase.LoadAssetAtPath<VSGraphModel>(k_GraphPath);
            Resources.UnloadAsset(graph);
            m_Store.Dispatch(new LoadGraphAssetAction(k_GraphPath));

            AssertIntegrity();
        }
    }
}
