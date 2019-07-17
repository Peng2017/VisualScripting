using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Action")]
    [Category("MakeArray")]
    class MakeArrayActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_ChangeMakeArrayNodePortCountAction([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateMakeArrayNode(Vector2.zero);
            var node1 = GraphModel.CreateMakeArrayNode(Vector2.zero);
            var node2 = GraphModel.CreateMakeArrayNode(Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(1));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(1));
                    return new ChangeMakeArrayNodePortCountAction(4, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(5));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(5));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(5));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(5));
                    return new ChangeMakeArrayNodePortCountAction(-2, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(3));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(3));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(3));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(3));
                    return new ChangeMakeArrayNodePortCountAction(5, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(8));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(8));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(8));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(8));
                    return new ChangeMakeArrayNodePortCountAction(-4, node0);
                },
                () =>
                {
                    // Port count should never go below 1.
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(4));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    Assert.That(node0.InputPortModels.Count, Is.EqualTo(4));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(4));
                    Assert.That(node1.PortCount, Is.EqualTo(1));
                    Assert.That(node2.PortCount, Is.EqualTo(1));
                    return new ChangeMakeArrayNodePortCountAction(5, node1, node2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.PortCount, Is.EqualTo(4));
                    Assert.That(node1.PortCount, Is.EqualTo(6));
                    Assert.That(node2.PortCount, Is.EqualTo(6));
                });
        }

        [Test]
        public void Test_ChangeMakeArrayNodePortCountAction_WithConnectedPort()
        {
            var makeArrayNode = GraphModel.CreateMakeArrayNode(Vector2.zero);
            var constNode0 = GraphModel.CreateConstantNode("Const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var constNode1 = GraphModel.CreateConstantNode("Const1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var constNode2 = GraphModel.CreateConstantNode("Const2", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            makeArrayNode.IncreasePortCount(2);
            GraphModel.CreateEdge(makeArrayNode.InputPortModels[0], constNode0.OutputPortModels[0]);
            GraphModel.CreateEdge(makeArrayNode.InputPortModels[1], constNode1.OutputPortModels[0]);
            GraphModel.CreateEdge(makeArrayNode.InputPortModels[2], constNode2.OutputPortModels[0]);

            Assert.That(GetNodeCount(), Is.EqualTo(4));
            Assert.That(GetEdgeCount(), Is.EqualTo(3));
            Assert.That(makeArrayNode.PortCount, Is.EqualTo(3));
            Assert.That(makeArrayNode.InputPortModels.Count, Is.EqualTo(3));
            Assert.That(makeArrayNode.InputPortModels[0], Is.ConnectedTo(constNode0.OutputPortModels[0]));
            Assert.That(makeArrayNode.InputPortModels[1], Is.ConnectedTo(constNode1.OutputPortModels[0]));
            Assert.That(makeArrayNode.InputPortModels[2], Is.ConnectedTo(constNode2.OutputPortModels[0]));

            m_Store.Dispatch(new ChangeMakeArrayNodePortCountAction(-2, makeArrayNode));

            Assert.That(GetNodeCount(), Is.EqualTo(4));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(makeArrayNode.PortCount, Is.EqualTo(1));
            Assert.That(makeArrayNode.InputPortModels.Count, Is.EqualTo(1));
            Assert.That(makeArrayNode.InputPortModels[0], Is.ConnectedTo(constNode0.OutputPortModels[0]));

            m_Store.Dispatch(new ChangeMakeArrayNodePortCountAction(1, makeArrayNode));

            Assert.That(GetNodeCount(), Is.EqualTo(4));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(makeArrayNode.PortCount, Is.EqualTo(2));
            Assert.That(makeArrayNode.InputPortModels.Count, Is.EqualTo(2));
            Assert.That(makeArrayNode.InputPortModels[0], Is.ConnectedTo(constNode0.OutputPortModels[0]));
            Assert.That(makeArrayNode.InputPortModels[1], Is.Not.ConnectedTo(constNode1.OutputPortModels[0]));
        }
    }
}
