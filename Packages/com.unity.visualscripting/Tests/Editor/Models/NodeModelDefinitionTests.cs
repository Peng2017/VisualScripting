using System;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class NodeModelDefinitionTests
    {
        NodeModel m_Node;
        public void M1(int i) { }
        public int M3(int i, bool b) => 0;

        class TestNodeModel : NodeModel
        {
            protected override void OnDefineNode()
            {
                AddDataInput<float>("one");
            }
        }

        [Test]
        public void CallingDefineTwiceCreatesPortsOnce()
        {
            VSGraphAssetModel asset = ScriptableObject.CreateInstance<VSGraphAssetModel>();
            VSGraphModel g = asset.CreateVSGraph<ClassStencil>("asd");

            m_Node = g.CreateNode<TestNodeModel>("test", Vector2.zero);
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(1));

            m_Node.DefineNode();
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(1));
        }

        [Test]
        public void CallingDefineTwiceCreatesOneEmbeddedConstant()
        {
            VSGraphAssetModel asset = ScriptableObject.CreateInstance<VSGraphAssetModel>();
            VSGraphModel g = asset.CreateVSGraph<ClassStencil>("asd");

            m_Node = g.CreateNode<TestNodeModel>("test", Vector2.zero);
            Assert.That(m_Node.InputConstants.Count, Is.EqualTo(1));

            m_Node.DefineNode();
            Assert.That(m_Node.InputConstants.Count, Is.EqualTo(1));
        }

        [Test]
        public void MethodWithOneParameterCreatesOnePortWhenDefinedTwice()
        {
            VSGraphAssetModel asset = ScriptableObject.CreateInstance<VSGraphAssetModel>();
            VSGraphModel g = asset.CreateVSGraph<ClassStencil>("asd");

            m_Node = g.CreateFunctionCallNode(GetType().GetMethod(nameof(M1)), Vector2.zero);
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(2));

            m_Node.DefineNode();
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(2));
        }

        [Test]
        public void ChangingMethodRecreatesOnlyNeededPorts()
        {
            MethodWithOneParameterCreatesOnePortWhenDefinedTwice();
            ((FunctionCallNodeModel)m_Node).MethodInfo = GetType().GetMethod(nameof(M3));
            m_Node.DefineNode();
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(3));
        }

        [Test]
        public void ChangingMethodDeletesPorts()
        {
            ChangingMethodRecreatesOnlyNeededPorts();
            ((FunctionCallNodeModel)m_Node).MethodInfo = GetType().GetMethod(nameof(M1));
            m_Node.DefineNode();
            Assert.That(m_Node.InputPortModels.Count, Is.EqualTo(2));
        }

        [Test]
        public void ChangingMethodKeepsConstantsConsistentWithInputPorts()
        {
            MethodWithOneParameterCreatesOnePortWhenDefinedTwice();

            Assert.That(m_Node.InputConstants.Count, Is.EqualTo(m_Node.InputPortModels.Count));

            ((FunctionCallNodeModel)m_Node).MethodInfo = GetType().GetMethod(nameof(M3));
            m_Node.DefineNode();

            Assert.That(m_Node.InputConstants.Count, Is.EqualTo(m_Node.InputPortModels.Count));

            ((FunctionCallNodeModel)m_Node).MethodInfo = GetType().GetMethod(nameof(M1));
            m_Node.DefineNode();

            Assert.That(m_Node.InputConstants.Count, Is.EqualTo(m_Node.InputPortModels.Count));
        }
    }
}
