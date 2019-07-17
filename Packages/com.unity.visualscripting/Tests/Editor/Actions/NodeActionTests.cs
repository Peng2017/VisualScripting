using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Node")]
    [Category("Action")]
    class NodeActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateNodeFromSearcherAction([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                ((VSGraphModel)data.graphModel).CreateBinaryOperatorNode(BinaryOperatorKind.Add, data.position)
            };

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    return new CreateNodeFromSearcherAction(GraphModel, Vector2.zero, new Vector2(100, 200));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<BinaryOperatorNodeModel>());
                    Assert.That(GraphModel.NodeModels.First().Position,
                        Is.EqualTo(new Vector2(100, 200)));
                }
            );
        }

        [Test]
        public void Test_CreateNodesFromSearcherAction([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data =>
            {
                var graph = (VSGraphModel)data.graphModel;
                var decl = graph.CreateGraphVariableDeclaration("georges", typeof(int).GenerateTypeHandle(Stencil), true);
                var variable = graph.CreateVariableNode(decl, data.position);
                var property = graph.CreateGetPropertyGroupNode(new Vector2(500, 1000));
                IPortModel outputPort = variable.OutputPortModels[0];
                var edge = graph.CreateEdge(property.InputPortModels[0], outputPort);

                return new List<IGraphElementModel>{ variable, property, edge }.ToArray();
            };

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(0));
                    Assert.That(GraphModel.EdgeModels.Count, Is.EqualTo(0));
                    return new CreateNodeFromSearcherAction(GraphModel, Vector2.zero, new Vector2(100, 200));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var nodes = GraphModel.NodeModels.ToList();
                    Assert.That(nodes[0], Is.TypeOf<VariableNodeModel>());
                    Assert.That(nodes[1], Is.TypeOf<GetPropertyGroupNodeModel>());
                    Assert.That(nodes[0].Position, Is.EqualTo(new Vector2(100, 200)));
                    Assert.That(nodes[1].Position, Is.EqualTo(new Vector2(500, 1000)));
                }
            );
        }

        [Test]
        public void Test_DuplicateAction_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    var nodeModel = GetNode(0);
                    Assert.That(nodeModel, Is.TypeOf<Type0FakeNodeModel>());

                    TargetInsertionInfo info = new TargetInsertionInfo();
                    info.OperationName = "Duplicate";
                    info.Delta = Vector2.one;
                    info.TargetStackInsertionIndex = -1;

                    IEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
                    VseGraphView.CopyPasteData copyPasteData = VseGraphView.GatherCopiedElementsData(x => x, new List<IGraphElementModel> { nodeModel });

                    return new PasteSerializedDataAction(GraphModel, info, editorDataModel, copyPasteData.ToJson());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.Count(n => n == null), Is.Zero);
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyNodesSequential([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyNodesSameTime([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0), GetNode(1), GetNode(2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DisconnectNodeAction([Values] TestingMode mode)
        {
            var const0 = GraphModel.CreateConstantNode("const0", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const1 = GraphModel.CreateConstantNode("const1", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const2 = GraphModel.CreateConstantNode("const2", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const3 = GraphModel.CreateConstantNode("const3", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const4 = GraphModel.CreateConstantNode("const4", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const5 = GraphModel.CreateConstantNode("const5", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary0.InputPortModels[0], const0.OutputPortModels[0]);
            GraphModel.CreateEdge(binary0.InputPortModels[1], const1.OutputPortModels[0]);
            GraphModel.CreateEdge(binary1.InputPortModels[0], binary0.OutputPortModels[0]);
            GraphModel.CreateEdge(binary1.InputPortModels[1], const0.OutputPortModels[0]);
            GraphModel.CreateEdge(binary2.InputPortModels[0], const2.OutputPortModels[0]);
            GraphModel.CreateEdge(binary2.InputPortModels[1], const3.OutputPortModels[0]);
            GraphModel.CreateEdge(binary3.InputPortModels[0], const4.OutputPortModels[0]);
            GraphModel.CreateEdge(binary3.InputPortModels[1], const5.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(8));
                    return new DisconnectNodeAction(binary0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    return new DisconnectNodeAction(binary2, binary3);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void Test_BypassNodeAction([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary0.InputPortModels[0], constantA.OutputPortModels[0]);
            GraphModel.CreateEdge(binary1.InputPortModels[0], binary0.OutputPortModels[0]);

            var constantB = GraphModel.CreateConstantNode("constantB", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary2.InputPortModels[0], constantB.OutputPortModels[0]);
            GraphModel.CreateEdge(binary3.InputPortModels[0], binary2.OutputPortModels[0]);

            var constantC = GraphModel.CreateConstantNode("constantC", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary4 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary5 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary4.InputPortModels[0], constantC.OutputPortModels[0]);
            GraphModel.CreateEdge(binary5.InputPortModels[0], binary4.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(6));
                    Assert.That(binary0.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(binary0.OutputPortModels[0]));
                    Assert.That(binary2.InputPortModels[0], Is.ConnectedTo(constantB.OutputPortModels[0]));
                    Assert.That(binary3.InputPortModels[0], Is.ConnectedTo(binary2.OutputPortModels[0]));
                    Assert.That(binary4.InputPortModels[0], Is.ConnectedTo(constantC.OutputPortModels[0]));
                    Assert.That(binary5.InputPortModels[0], Is.ConnectedTo(binary4.OutputPortModels[0]));
                    return new BypassNodeAction(binary0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                    Assert.That(binary2.InputPortModels[0], Is.ConnectedTo(constantB.OutputPortModels[0]));
                    Assert.That(binary3.InputPortModels[0], Is.ConnectedTo(binary2.OutputPortModels[0]));
                    Assert.That(binary4.InputPortModels[0], Is.ConnectedTo(constantC.OutputPortModels[0]));
                    Assert.That(binary5.InputPortModels[0], Is.ConnectedTo(binary4.OutputPortModels[0]));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                    Assert.That(binary2.InputPortModels[0], Is.ConnectedTo(constantB.OutputPortModels[0]));
                    Assert.That(binary3.InputPortModels[0], Is.ConnectedTo(binary2.OutputPortModels[0]));
                    Assert.That(binary4.InputPortModels[0], Is.ConnectedTo(constantC.OutputPortModels[0]));
                    Assert.That(binary5.InputPortModels[0], Is.ConnectedTo(binary4.OutputPortModels[0]));
                    return new BypassNodeAction(binary2, binary4);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                    Assert.That(binary3.InputPortModels[0], Is.ConnectedTo(constantB.OutputPortModels[0]));
                    Assert.That(binary5.InputPortModels[0], Is.ConnectedTo(constantC.OutputPortModels[0]));
                });
        }

        [Test]
        public void Test_RemoveNodesAction([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            IPortModel outputPort = constantA.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[0], outputPort);
            IPortModel outputPort1 = binary0.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[0], outputPort1);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var nodeToDeleteAndBypass = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();

                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(nodeToDeleteAndBypass.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(nodeToDeleteAndBypass.OutputPortModels[0]));
                    return new RemoveNodesAction(new INodeModel[]{nodeToDeleteAndBypass}, new INodeModel[]{nodeToDeleteAndBypass});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(constantA.OutputPortModels[0]));
                });
        }

        //TODO We disabled exception to fix a bug where Bypass&Remove would throw when removing a group of nodes...
        //     where one of the nodes (ex:constant) has only one edge connected to the group and that edge is removed
        //     before being doing the bypass on the constant node. See the fogbugz case 1049559
        [Ignore("Disable until remove corner cases handled")]
        [Test]
        public void Test_BypassNodeAction_Throw()
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);

            Assert.Throws<InvalidOperationException>(() => m_Store.Dispatch(new BypassNodeAction(constantA)));
        }

        [Test]
        public void Test_ChangeNodeColorAction([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Color, Is.EqualTo(Color.clear));
                    Assert.That(node1.Color, Is.EqualTo(Color.clear));
                    Assert.That(node2.Color, Is.EqualTo(Color.clear));
                    Assert.That(node3.Color, Is.EqualTo(Color.clear));
                    return new ChangeNodeColorAction(Color.red, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Color, Is.EqualTo(Color.red));
                    Assert.That(node1.Color, Is.EqualTo(Color.clear));
                    Assert.That(node2.Color, Is.EqualTo(Color.clear));
                    Assert.That(node3.Color, Is.EqualTo(Color.clear));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Color, Is.EqualTo(Color.red));
                    Assert.That(node1.Color, Is.EqualTo(Color.clear));
                    Assert.That(node2.Color, Is.EqualTo(Color.clear));
                    Assert.That(node3.Color, Is.EqualTo(Color.clear));
                    return new ChangeNodeColorAction(Color.blue, node1, node2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Color, Is.EqualTo(Color.red));
                    Assert.That(node1.Color, Is.EqualTo(Color.blue));
                    Assert.That(node2.Color, Is.EqualTo(Color.blue));
                    Assert.That(node3.Color, Is.EqualTo(Color.clear));
                });
        }
    }
}
