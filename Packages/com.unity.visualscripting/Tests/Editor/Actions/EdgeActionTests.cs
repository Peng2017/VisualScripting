using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using static UnityEditor.VisualScripting.Model.VSPreferences;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Edge")]
    [Category("Action")]
    class EdgeActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateEdgeAction_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.InputPortModels[0];
            var output0 = node1.OutputPortModels[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    return new CreateEdgeAction(input0, output0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                });
        }

        [Test]
        public void Test_CreateEdgeAction_Duplicate([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.InputPortModels[0];
            var output0 = node1.OutputPortModels[0];

            GraphModel.CreateEdge(input0, output0);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    return new CreateEdgeAction(input0, output0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                });
        }

        [PublicAPI]
        public enum ItemizeTestType
        {
            Enabled, Disabled
        }

        static IEnumerable<object[]> GetItemizeTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                foreach (GroupingMode groupingMode in Enum.GetValues(typeof(GroupingMode)))
                {
                    // test both itemize option and non ItemizeTestType option
                    foreach (ItemizeTestType itemizeTest in Enum.GetValues(typeof(ItemizeTestType)))
                    {
                        yield return MakeItemizeTestCase(testingMode, groupingMode, ItemizeOptions.Variables, itemizeTest,
                            graphModel =>
                            {
                                string name = graphModel.GetUniqueName("myInt");
                                VariableDeclarationModel decl = graphModel.CreateGraphVariableDeclaration(name, typeof(int).GenerateTypeHandle(graphModel.Stencil), true);
                                return graphModel.CreateVariableNode(decl, Vector2.zero);
                            }
                        );

                        yield return MakeItemizeTestCase(testingMode, groupingMode, ItemizeOptions.SystemConstants, itemizeTest,
                            graphModel =>
                                graphModel.CreateNode<SystemConstantNodeModel>("Constant", Vector2.zero, m =>
                                {
                                    m.ReturnType = typeof(float).GenerateTypeHandle(graphModel.Stencil);
                                    m.DeclaringType = typeof(Mathf).GenerateTypeHandle(graphModel.Stencil);
                                    m.Identifier = "PI";
                                }));

                        yield return MakeItemizeTestCase(testingMode, groupingMode, ItemizeOptions.Constants, itemizeTest,
                            graphModel =>
                            {
                                string name = graphModel.GetUniqueName("myInt");
                                return graphModel.CreateConstantNode(name, typeof(int).GenerateTypeHandle(graphModel.Stencil), Vector2.zero);
                            });
                    }
                }
            }
        }

        static object[] MakeItemizeTestCase(TestingMode testingMode, GroupingMode groupingMode, ItemizeOptions options, ItemizeTestType itemizeTest, Func<VSGraphModel,INodeModel> makeNode)
        {
            return new object[] { testingMode, groupingMode, options, itemizeTest, makeNode };
        }

        [Test, TestCaseSource(nameof(GetItemizeTestCases))]
        public void Test_CreateEdgeAction_Itemize(TestingMode testingMode, GroupingMode groupingMode, ItemizeOptions options, ItemizeTestType itemizeTest, Func<VSGraphModel,INodeModel> makeNode)
        {
            // save initial itemize options
            VSPreferences pref = ((TestState)m_Store.GetState()).Preferences;
            ItemizeOptions initialOptions = pref.CurrentItemizeOptions;
            bool inGroupTest = groupingMode == GroupingMode.Grouped;

            try
            {
                // create int node
                INodeModel node0 = makeNode(GraphModel);
                IPortModel output0 = node0.OutputPortModels[0];

                // create Addition node
                BinaryOperatorNodeModel binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
                IPortModel input0 = binOp.InputPortModels[0];
                IPortModel input1 = binOp.InputPortModels[1];
                IPortModel binOutput = binOp.OutputPortModels[0];

                GroupNodeModel group = null;
                if (inGroupTest)
                {
                    // Create GroupNode
                    group = GraphModel.CreateGroupNode("", Vector2.zero);
                    group.AddNode(binOp);
                }

                // enable Itemize depending on the test case
                pref.CurrentItemizeOptions = (itemizeTest == ItemizeTestType.Enabled) ? options : ItemizeOptions.Nothing;

                // connect int to first input
                m_Store.Dispatch(new CreateEdgeAction(input0, output0));
                m_Store.Update();

                // test how the node reacts to getting connected a second time
                TestPrereqActionPostreq(testingMode,
                    () =>
                    {
                        Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 3 : 2));
                        Assert.That(GetEdgeCount(), Is.EqualTo(1));
                        Assert.That(input0, Is.ConnectedTo(output0));
                        Assert.That(input1, Is.Not.ConnectedTo(output0));
                        Assert.That(binOutput.Connected, Is.False);
                        Assert.False(node0.IsGrouped);
                        Assert.That(node0.GroupNodeModel, Is.EqualTo(null));
                        return new CreateEdgeAction(input1, output0);
                    },
                    () =>
                    {
                        Assert.That(GetEdgeCount(), Is.EqualTo(2));
                        Assert.That(input0, Is.ConnectedTo(output0));
                        Assert.That(binOutput.Connected, Is.False);
                        Assert.False(node0.IsGrouped);
                        Assert.That(node0.GroupNodeModel, Is.EqualTo(null));

                        if (itemizeTest == ItemizeTestType.Enabled)
                        {
                            Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 4 : 3));
                            NodeModel newNode = GetNode(inGroupTest ? 3 : 2);
                            Assert.That(newNode, Is.TypeOf(node0.GetType()));
                            Assert.That(newNode.IsGrouped, Is.EqualTo(inGroupTest));
                            Assert.That(newNode.GroupNodeModel, Is.EqualTo(group));
                            IPortModel output1 = newNode.OutputPortModels[0];
                            Assert.That(input1, Is.ConnectedTo(output1));
                        }
                        else
                        {
                            Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 3: 2));
                        }
                    });
            }
            finally
            {
                // restore itemize options
                pref.CurrentItemizeOptions = initialOptions;
            }
        }

        // TODO: Test itemization when connecting to stacked nodes (both grouped and ungrouped)

        [Test]
        public void Test_CreateEdgeAction_ManyEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.InputPortModels[0];
            var input1 = node0.InputPortModels[1];
            var input2 = node0.InputPortModels[2];
            var output0 = node1.OutputPortModels[0];
            var output1 = node1.OutputPortModels[1];
            var output2 = node1.OutputPortModels[2];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input0, output0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input1, output1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input2, output2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                });
        }

        [Test]
        public void Test_CreateEdgeFromSinglePortAction_DoesNothing()
        {
            var node0 = GraphModel.CreateNode<Type1FakeNodeModel>("Node0", new Vector2(-200, 0));
            var input0 = node0.InputPortModels[0];

            // When the port is an input port, then nothing should happen,
            // so no undo to test.
            Assert.That(GetNodeCount(), Is.EqualTo(1));
            Assert.That(GetEdgeCount(), Is.EqualTo(0));
            m_Store.Dispatch(new CreateEdgeFromSinglePortAction(input0, new Vector2(200, 0)));
            Assert.That(GetNodeCount(), Is.EqualTo(1));
            Assert.That(GetEdgeCount(), Is.EqualTo(0));
        }

        static IEnumerable<object[]> GetCreateTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                foreach (GroupingMode groupMode in Enum.GetValues(typeof(GroupingMode)))
                {
                    yield return new object[] { testingMode, groupMode };
                }
            }
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateEdgeFromSinglePortAction_CreateNode(TestingMode testingMode, GroupingMode groupingMode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero)
            };

            bool inGroupTest = groupingMode == GroupingMode.Grouped;
            var node0 = GraphModel.CreateNode<Type1FakeNodeModel>("Node0", Vector2.zero);
            var output0 = node0.OutputPortModels[0];
            GroupNodeModel group = null;
            if (inGroupTest)
                group = GraphModel.CreateGroupNode(string.Empty, Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 2 : 1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateEdgeFromSinglePortAction(output0, Vector2.zero, targetGroup:group);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 3 : 2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var newNode = GetNode(inGroupTest ? 2 : 1);
                    Assert.That(newNode, Is.TypeOf<BinaryOperatorNodeModel>());
                    Assert.That(newNode.IsGrouped, Is.EqualTo(inGroupTest));
                    Assert.That(newNode.GroupNodeModel, Is.EqualTo(group));
                });
        }

        [Test]
        public void Test_CreateEdgeFromSinglePortAction_CreateStack_DeletePreviousEdge([Values] TestingMode mode)
        {
            var stack1 = GraphModel.CreateStack("Stack1", Vector2.zero);
            var stack2 = GraphModel.CreateStack("Stack2", Vector2.zero);
            ConnectTo(stack1, 0, stack2, 0);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var edge = GraphModel.GetEdgesConnections(stack1.OutputPortModels[0]).SingleOrDefault(e => e.InputPortModel == stack2.InputPortModels[0]);
                    Assert.That(edge, Is.Not.Null);
                    return new CreateEdgeFromSinglePortAction(stack1.OutputPortModels[0], Vector2.down, new List<IEdgeModel> { edge });
                },
                () => {
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var stack1Edges = GraphModel.GetEdgesConnections(stack1.OutputPortModels[0]).ToList();
                    Assert.That(stack1Edges.Count, Is.EqualTo(1));
                    Assert.That(stack1Edges[0].InputPortModel.NodeModel, Is.EqualTo(GetStack(2)));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateEdgeFromSinglePortAction_CreateStack(TestingMode testingMode, GroupingMode groupingMode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero)
            };

            bool inGroupTest = groupingMode == GroupingMode.Grouped;
            var stack = GraphModel.CreateStack("Stack", Vector2.zero);
            var output0 = stack.OutputPortModels[0];
            GroupNodeModel group = null;
            if (inGroupTest)
                group = GraphModel.CreateGroupNode(string.Empty, Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateEdgeFromSinglePortAction(output0, Vector2.zero, targetGroup:group);
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var newStack = GetStack(1);
                    Assert.That(newStack, Is.TypeOf<StackModel>());
                    Assert.That(newStack.IsGrouped, Is.EqualTo(inGroupTest));
                    Assert.That(newStack.GroupNodeModel, Is.EqualTo(group));
                });
        }

        [Test]
        public void Test_CreateEdgeFromSinglePortAction_CreateAndInsertSetVariablePropertyToStack(
            [Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => new IGraphElementModel[]
            {
                stack.CreateSetPropertyGroupNode(-1)
            };

            var decl = GraphModel.CreateGraphVariableDeclaration("x", typeof(float).GenerateTypeHandle(Stencil), false);
            var node = GraphModel.CreateVariableNode(decl, Vector2.left * 200);
            var nodePortModel = node.OutputPortModels.First();

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(0));
                    Assert.That(nodePortModel.Connected, Is.False);
                    return new CreateEdgeFromSinglePortAction(nodePortModel, Vector2.zero, targetStack: stack);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
                    var propertyNode = stack.NodeModels.First();
                    Assert.That(propertyNode, Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(nodePortModel.Connected, Is.True);
                    var propertyPortModel = propertyNode.InputPortModels.First();
                    Assert.That(propertyPortModel.Connected, Is.True);
                    Assert.That(nodePortModel.ConnectionPortModels.Single().Index, Is.EqualTo(0));
                    Assert.That(nodePortModel.ConnectionPortModels.Single(), Is.EqualTo(propertyPortModel));
                });
        }

        [Test]
        public void Test_CreateEdgeFromSinglePortAction_InsertConstantToStack([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
                    var node = GraphModel.CreateNode<Type1FakeNodeModel>("Node0", Vector2.zero);
                    var nodePortModel = node.OutputPortModels.First();
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(0));
                    Assert.That(nodePortModel.Connected, Is.False);
                    return new CreateEdgeFromSinglePortAction(nodePortModel, Vector2.zero, targetStack: stack);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    var node = GetNode(0);
                    var nodePortModel = node.OutputPortModels.First();
                    Assert.That(nodePortModel.Connected, Is.False);
                });
        }

        [Test]
        public void Test_CreateEdgeFromSinglePortAction_CreateInsertLoopFromLoopStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var loopStack = GraphModel.CreateLoopStack(typeof(ForEachHeaderModel), Vector2.right * 100);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(0));
                    var portModel = loopStack.InputPortModels.First();
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateEdgeFromSinglePortAction(loopStack.InputPortModels.First(), Vector2.zero, targetStack: stack);
                },
                () => {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
                    var loopNode = stack.NodeModels.First();
                    Assert.That(loopNode, Is.TypeOf<ForEachNodeModel>());
                    var portModel = loopNode.OutputPortModels.First();
                    Assert.That(portModel.Connected, Is.True);
                    Assert.That(portModel.ConnectionPortModels.Single(), Is.EqualTo(loopStack.InputPortModels.First()));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateEdgeFromSinglePortAction_CreateLoopStackFromInsertLoop(TestingMode testingMode,
            GroupingMode groupingMode)
        {
            bool inGroupTest = groupingMode == GroupingMode.Grouped;
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var loopNode = stack.CreateStackedNode<WhileNodeModel>("loop", 0);
            var stackCount = GetStackCount();
            GroupNodeModel group = null;
            if (inGroupTest)
                group = GraphModel.CreateGroupNode(string.Empty, Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount));
                    var portModel = loopNode.OutputPortModels.First();
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateEdgeFromSinglePortAction(loopNode.OutputPortModels.First(), Vector2.zero, targetStack: stack, targetGroup:group);
                },
                () => {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount + 1));
                    var portModel = loopNode.OutputPortModels.First();
                    Assert.That(portModel.Connected, Is.True);
                    var connectedStack = portModel.ConnectionPortModels.Single().NodeModel;
                    Assert.That(connectedStack, Is.TypeOf<WhileHeaderModel>());
                    Assert.That(connectedStack.IsGrouped, Is.EqualTo(inGroupTest));
                    Assert.That(connectedStack.GroupNodeModel, Is.EqualTo(group));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.InputPortModels[0];
            var input1 = node0.InputPortModels[1];
            var input2 = node0.InputPortModels[2];
            var output0 = node1.OutputPortModels[0];
            var output1 = node1.OutputPortModels[1];
            var output2 = node1.OutputPortModels[2];
            var edge0 = GraphModel.CreateEdge(input0, output0);
            GraphModel.CreateEdge(input1, output1);
            GraphModel.CreateEdge(input2, output2);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                    return new DeleteElementsAction(edge0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyEdges([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            GraphModel.CreateEdge(node0.InputPortModels[0], node1.OutputPortModels[0]);
            GraphModel.CreateEdge(node0.InputPortModels[1], node1.OutputPortModels[1]);
            GraphModel.CreateEdge(node0.InputPortModels[2], node1.OutputPortModels[2]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(node0.InputPortModels[0], Is.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.ConnectedTo(node1.OutputPortModels[2]));
                    var edge0 = GraphModel.EdgeModels.First(e => e.InputPortModel == node0.InputPortModels[0]);
                    return new DeleteElementsAction(edge0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.InputPortModels[0], Is.Not.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.ConnectedTo(node1.OutputPortModels[2]));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.InputPortModels[0], Is.Not.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.ConnectedTo(node1.OutputPortModels[2]));
                    var edge1 = GraphModel.EdgeModels.First(e => e.InputPortModel == node0.InputPortModels[1]);

                    return new DeleteElementsAction(edge1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.InputPortModels[0], Is.Not.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.Not.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.ConnectedTo(node1.OutputPortModels[2]));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.InputPortModels[0], Is.Not.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.Not.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.ConnectedTo(node1.OutputPortModels[2]));
                    var edge2 = GraphModel.EdgeModels.First(e => e.InputPortModel == node0.InputPortModels[2]);
                    return new DeleteElementsAction(edge2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.InputPortModels[0], Is.Not.ConnectedTo(node1.OutputPortModels[0]));
                    Assert.That(node0.InputPortModels[1], Is.Not.ConnectedTo(node1.OutputPortModels[1]));
                    Assert.That(node0.InputPortModels[2], Is.Not.ConnectedTo(node1.OutputPortModels[2]));
                });
        }

        [Test]
        public void Test_SplitEdgeAndInsertNodeAction([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode("Constant", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var edge = GraphModel.CreateEdge(binary0.InputPortModels[0], constant.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary0.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));
                    return new SplitEdgeAndInsertNodeAction(edge, binary1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(binary1.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));
                    Assert.That(binary0.InputPortModels[0], Is.ConnectedTo(binary1.OutputPortModels[0]));
                });
        }

        [Test]
        public void TestCreateNodeOnEdge_DoesNothing()
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[] {};

            var constant = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var edge = GraphModel.CreateEdge(binary.InputPortModels[0], constant.OutputPortModels[0]);

            Assert.That(GetNodeCount(), Is.EqualTo(2));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(binary.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));

            m_Store.Dispatch(new CreateNodeOnEdgeAction(edge, Vector2.zero, Vector2.down));

            Assert.That(GetNodeCount(), Is.EqualTo(2));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(binary.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));
        }

        [Test]
        public void TestCreateNodeOnEdge_OnlyInputPortConnected([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Equals, Vector2.zero)
            };

            var constant = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var unary = GraphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero);
            IPortModel outputPort = constant.OutputPortModels[0];
            var edge = GraphModel.CreateEdge(unary.InputPortModels[0], outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(unary.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, Vector2.down);
                },
                () =>
                {
                    var binary = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().FirstOrDefault();

                    Assert.IsNotNull(binary);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(unary.InputPortModels[0]));
                    Assert.That(binary.OutputPortModels[0], Is.Not.ConnectedTo(unary.InputPortModels[0]));
                    Assert.IsFalse(GraphModel.EdgeModels.Contains(edge));
                }
            );
        }

        [Test]
        public void TestCreateNodeOnEdge_BothPortsConnected([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero)
            };

            var constant = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var unary = GraphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero);
            var edge = GraphModel.CreateEdge(unary.InputPortModels[0], constant.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(unary.InputPortModels[0], Is.ConnectedTo(constant.OutputPortModels[0]));
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, Vector2.down);
                },
                () =>
                {
                    var unary2 = GraphModel.NodeModels.OfType<UnaryOperatorNodeModel>().ToList()[1];

                    Assert.IsNotNull(unary2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(unary2.InputPortModels[0]));
                    Assert.That(unary2.OutputPortModels[0], Is.ConnectedTo(unary.InputPortModels[0]));
                    Assert.IsFalse(GraphModel.EdgeModels.Contains(edge));
                }
            );
        }

        [Test]
        public void TestCreateNodeOnEdge_WithOutputNodeConnectedToUnknown([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Multiply, Vector2.zero)
            };

            var constantNode = GraphModel.CreateConstantNode("int1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var addNode = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(addNode.InputPortModels[0], constantNode.OutputPortModels[0]);
            GraphModel.CreateEdge(addNode.InputPortModels[1], constantNode.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));

                    Assert.That(addNode, Is.Not.Null);
                    Assert.That(addNode.InputPortModels[0], Is.ConnectedTo(constantNode.OutputPortModels[0]));
                    var edge = GraphModel.EdgeModels.First();
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, Vector2.down);
                },
                () =>
                {
                    var multiplyNode = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().ToList()[1];

                    Assert.IsNotNull(multiplyNode);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(constantNode.OutputPortModels[0], Is.ConnectedTo(multiplyNode.InputPortModels[0]));
                    Assert.That(multiplyNode.OutputPortModels[0], Is.ConnectedTo(addNode.InputPortModels[0]));
                    Assert.That(constantNode.OutputPortModels[0], Is.Not.ConnectedTo(addNode.InputPortModels[0]));
                }
            );
        }

        [Test]
        public void TestCreateEdge_CannotConnectForEachNodeToWhileStack()
        {
            var stack = GraphModel.CreateStack("", Vector2.zero);
            var forEach = stack.CreateStackedNode<ForEachNodeModel>("", -1);
            var whileStack = GraphModel.CreateLoopStack(typeof(WhileHeaderModel), Vector2.zero);

            var edgeCount = GetEdgeCount();
            m_Store.Dispatch(new CreateEdgeAction(whileStack.InputPortModels[0], forEach.OutputPortModels[0]));
            Assert.That(GetEdgeCount(), Is.EqualTo(edgeCount));
        }

        [Test]
        public void TestCreateNodeOnEdge_CannotInsertOnLoopEdge([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Multiply, Vector2.zero)
            };

            var stack = GraphModel.CreateStack("", Vector2.zero);
            var loopStack = GraphModel.CreateLoopStack(typeof(ForEachHeaderModel), Vector2.zero);
            var loopNode = stack.CreateLoopNode(loopStack, -1);
            var edge = GraphModel.CreateEdge(loopStack.InputPortModels[0], loopNode.OutputPortModels[0]);

            Assert.That(GetNodeCount(), Is.EqualTo(2));
            Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(loopStack.InputPortModels[0], Is.ConnectedTo(loopNode.OutputPortModels[0]));

            m_Store.Dispatch(new CreateNodeOnEdgeAction(edge, Vector2.zero, Vector2.down));

            Assert.That(GetNodeCount(), Is.EqualTo(2));
            Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            Assert.That(loopStack.InputPortModels[0], Is.ConnectedTo(loopNode.OutputPortModels[0]));
        }
    }
}
