using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class TestGraph: ICreatableGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public bool ListInHomePage => false;
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        public void InitBasicGraph(VSGraphModel graph)
        {
            Stencil stencil = graph.Stencil;
            AssetDatabase.SaveAssets();

            var method = graph.CreateFunction("method", Vector2.left * 200);
            method.CreateFunctionVariableDeclaration("l", typeof(int).GenerateTypeHandle(stencil));
            method.CreateFunctionParameterDeclaration("a", typeof(int).GenerateTypeHandle(stencil));

            var log = method.CreateFunctionCallNode(TypeSystem.GetMethod(typeof(Debug), nameof(Debug.Log), true));
            var abs = graph.CreateFunctionCallNode(TypeSystem.GetMethod(typeof(Mathf), "Abs", true), new Vector2(-350, 100));
            graph.CreateEdge(log.InputPortModels[0], abs.OutputPortModels[0]);

            var xDecl = graph.CreateGraphVariableDeclaration("x", typeof(float).GenerateTypeHandle(stencil), true);
            var xUsage = graph.CreateVariableNode(xDecl, new Vector2(-450, 100));
            graph.CreateEdge(abs.InputPortModels[0], xUsage.OutputPortModels[0]);

            var stack001 = graph.CreateStack(string.Empty, new Vector2(-200, 300));
            stack001.CreateFunctionCallNode(TypeSystem.GetMethod(typeof(Debug), "Log", true));

            var method2 = graph.CreateFunction("method2", Vector2.left * 800);
            method2.CreateFunctionRefCallNode(method);
        }
    }
}
