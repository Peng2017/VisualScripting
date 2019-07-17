using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScriptingTests.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.VisualScripting.Editor.Blackboard;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Stack")]
    [Category("Action")]
    class StackActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateStackedNodeFromSearcherAction([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            stack.CreateSetPropertyGroupNode(-1);

            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => new IGraphElementModel[]
            {
                ((StackModel)data.stackModel).CreateUnaryStatementNode(UnaryOperatorKind.PostDecrement, data.index)
            };

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
                    return new CreateStackedNodeFromSearcherAction(stack, 1, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(2));

                    var stackedNodes = stack.NodeModels.ToList();
                    Assert.That(stackedNodes[0], Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(stackedNodes[1], Is.TypeOf<UnaryOperatorNodeModel>());
                }
            );
        }

        static IEnumerable<object[]> GetCreateFromSearcherTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                foreach (GroupingMode groupingMode in Enum.GetValues(typeof(GroupingMode)))
                {
                    // Test ForEach node creation
                    yield return MakeCreateFromSearcherTestCase(
                        testingMode, groupingMode, typeof(ForEachNodeModel), new []{typeof(ForEachHeaderModel)},
                        (data, stencil, stack) =>
                        {
                            var db = new GraphElementSearcherDatabase(stencil).AddControlFlows().Build();
                            var items = db.Search("for each", out _);
                            return ((StackNodeModelSearcherItem)items[0]).CreateElements.Invoke(new StackNodeCreationData(stack, -1));
                        });

                    // Test If node creation (complete)
                    yield return MakeCreateFromSearcherTestCase(
                        testingMode, groupingMode, typeof(IfConditionNodeModel),
                        new []{typeof(StackModel), typeof(StackModel), typeof(StackModel)},
                        (data, stencil, stack) =>
                        {
                            var db = new GraphElementSearcherDatabase(stencil).AddControlFlows().Build();
                            var items = db.Search("if complete", out _);
                            return ((StackNodeModelSearcherItem)items[0]).CreateElements.Invoke(new StackNodeCreationData(stack, -1));
                        });
                }
            }
        }

        static object[] MakeCreateFromSearcherTestCase(TestingMode testingMode, GroupingMode groupingMode,
            Type expectedNodeType, Type[] expectedStackTypes,
            Func<StackNodeCreationData, Stencil, StackModel, IGraphElementModel[]> makeNodeFunc)
        {
            return new object[] { testingMode, groupingMode, expectedNodeType, expectedStackTypes, makeNodeFunc };
        }

        [Test, TestCaseSource(nameof(GetCreateFromSearcherTestCases))]
        public void Test_CreateLoopStackedNodeFromSearcherAction(TestingMode mode, GroupingMode groupingMode,
            Type expectedNodeType, Type[] expectedStackTypes,
            Func<StackNodeCreationData, Stencil, StackModel, IGraphElementModel[]> makeNodeFunc)
        {
            bool inGroupTest = groupingMode == GroupingMode.Grouped;
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            if (inGroupTest)
            {
                GroupNodeModel group = GraphModel.CreateGroupNode("", Vector2.zero);
                group.AddNode(stack);
            }

            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => makeNodeFunc(data, Stencil, stack);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(inGroupTest ? 2: 1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(0));
                    return new CreateStackedNodeFromSearcherAction(stack, 1, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1 + expectedStackTypes.Length + (inGroupTest ? 1: 0)));
                    Assert.That(GetStackCount(), Is.EqualTo(1 + expectedStackTypes.Length));
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));

                    var stackedNodes = stack.NodeModels.ToList();
                    Assert.That(stackedNodes[0].GetType(), Is.EqualTo(expectedNodeType));

                    int cnt = 1;
                    foreach (var expectedStackType in expectedStackTypes)
                    {
                        var newStack = GetStack(cnt++);
                        Assert.That(newStack.GetType(), Is.EqualTo(expectedStackType));
                        // Nodes added to a stack that is in a group are _not_ themselves grouped.
                        Assert.That(newStack.IsGrouped, Is.EqualTo(false));
                        Assert.That(newStack.GroupNodeModel, Is.EqualTo(null));
                    }
                }
            );
        }

        [Test]
        public void Test_CreateStackAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                    return new CreateStacksForNodesAction(new StackCreationOptions(Vector2.zero));
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void DuplicateStackedNode([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            stack.CreateStackedNode<Type0FakeNodeModel>("test", 0);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(1));
                }, () =>
                {
                    var node = stack.NodeModels.Single();

                    TargetInsertionInfo info;
                    info.OperationName = "Duplicate";
                    info.Delta = Vector2.one;
                    info.TargetStack = stack;
                    info.TargetStackInsertionIndex = -1;

                    IEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
                    VseGraphView.CopyPasteData copyPasteData = VseGraphView.GatherCopiedElementsData(x => x, new List<IGraphElementModel> { node });
                    Assert.That(copyPasteData.IsEmpty(), Is.False);

                    return new PasteSerializedDataAction(GraphModel, info, editorDataModel, copyPasteData.ToJson());
                }, () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_MoveNodesFromSeparateStacks([Values] TestingMode mode)
        {
            string customPrefix = "InitialStack";
            var stack1 = GraphModel.CreateStack(customPrefix + "1", Vector2.zero);
            var stack2 = GraphModel.CreateStack(customPrefix + "2", Vector2.right * 400f);

            stack1.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            stack1.CreateStackedNode<Type0FakeNodeModel>("B", 1);

            stack2.CreateStackedNode<Type0FakeNodeModel>("C", 0);

            Vector2 newStackOffset = Vector2.down * 500f;

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetAllStacks().All(s => s.name.StartsWith(customPrefix)), Is.True);

                    var stackMovements = GetAllStacks().Select(s => new StackCreationOptions(s.Position + newStackOffset, s.NodeModels.ToList()));

                    return new CreateStacksForNodesAction(stackMovements.ToList());
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetAllStacks().Any(s => s.name.StartsWith(customPrefix)), Is.False);
                });
        }

        [Test]
        public void Test_DeleteStackAction_OneEmptyStack([Values] TestingMode mode)
        {
            GraphModel.CreateStack(string.Empty, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    return new DeleteElementsAction(GetFloatingStack(0));
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneStackWithNodes([Values] TestingMode mode)
        {
            GraphModel.CreateStack(string.Empty, Vector2.zero);
            var stack = GetStack(0);
            stack.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            stack.CreateStackedNode<Type0FakeNodeModel>("B", 1);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetStack(0).NodeModels.Count(), Is.EqualTo(2));
                    return new DeleteElementsAction(GetFloatingStack(0));
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_ChangeStackedNode_SetVariableToLog([Values] TestingMode mode)
        {
            VariableDeclarationModel decl = GraphModel.CreateGraphVariableDeclaration("a", TypeHandle.Bool, true);
            StackModel stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var set = stackModel.CreateStackedNode<SetVariableNodeModel>("set", 0);
            var v1 = GraphModel.CreateVariableNode(decl, Vector2.left);
            var v2 = GraphModel.CreateVariableNode(decl, Vector2.left);
            GraphModel.CreateEdge(set.InputPortModels[0], v1.OutputPortModels[0]);
            GraphModel.CreateEdge(set.InputPortModels[1], v2.OutputPortModels[0]);

            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => new IGraphElementModel[] {
                stackModel.CreateFunctionCallNode(typeof(Debug).GetMethod("Log", new[]{typeof(object)}), 0)
            };

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(stackModel.NodeModels.Single(), Is.TypeOf<SetVariableNodeModel>());
                    set = (SetVariableNodeModel)stackModel.NodeModels.Single();
                    Assert.That(v1.OutputPortModels[0], Is.ConnectedTo(set.InputPortModels[0]));
                    Assert.That(v2.OutputPortModels[0], Is.ConnectedTo(set.InputPortModels[1]));
                    return new ChangeStackedNodeAction(set, stackModel, Vector2.zero);
                },
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(stackModel.NodeModels.Single(), Is.TypeOf<FunctionCallNodeModel>());
                    var log = stackModel.NodeModels.Single();
                    Assert.That(v1.OutputPortModels[0], Is.ConnectedTo(log.InputPortModels[0]));
                    Assert.That(v2.OutputPortModels[0].Connected, Is.False);
                });
        }

        [Test]
        public void Test_ChangeStackedNode_ToSameNodeModel([Values] TestingMode mode)
        {
            StackModel stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);

            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => new IGraphElementModel[] {
                stackModel.CreateUnaryStatementNode(UnaryOperatorKind.PostIncrement, 1)
            };

            var node1 = stackModel.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var node2 = stackModel.CreateUnaryStatementNode(UnaryOperatorKind.PostDecrement, 1);
            var node3 = stackModel.CreateStackedNode<Type0FakeNodeModel>("B", 2);

            var variableNode = GraphModel.CreateVariableNode(
                ScriptableObject.CreateInstance<VariableDeclarationModel>(), Vector2.zero);

            GraphModel.CreateEdge(node2.InputPortModels.First(), variableNode.OutputPortModels.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(node1, Has.IndexInStack(0, stackModel));
                    var oldNode = GetStackedNode(0, 1);
                    Assert.That(oldNode, Is.InstanceOf<UnaryOperatorNodeModel>());
                    Assert.That(node3, Has.IndexInStack(2, stackModel));
                    Assert.That(variableNode.OutputPortModels[0], Is.ConnectedTo(oldNode.InputPortModels[0]));
                    return new ChangeStackedNodeAction(oldNode, stackModel, Vector2.zero);
                },
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(node1, Has.IndexInStack(0, stackModel));
                    Assert.That(node2.IsStacked, Is.False);
                    Assert.That(node3, Has.IndexInStack(2, stackModel));

                    var newNode = GetStackedNode(0, 1);
                    Assert.That(newNode, Is.Not.Null);
                    Assert.That(newNode, Is.InstanceOf<UnaryOperatorNodeModel>());

                    var newUnaryNode = (UnaryOperatorNodeModel)newNode;
                    Assert.That(newUnaryNode.kind, Is.EqualTo(UnaryOperatorKind.PostIncrement));
                    Assert.That(variableNode.OutputPortModels[0], Is.ConnectedTo(newUnaryNode.InputPortModels[0]));
                });
        }

        [Test]
        public void Test_ChangeStackedNode_ToDifferentNodeModel([Values] TestingMode mode)
        {
            StackModel stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);

            ((TestState)m_Store.GetState()).queryStackNodeModelMockResult = data => new IGraphElementModel[] {
                stackModel.CreateStackedNode<WhileNodeModel>("", 1)
            };

            var node1 = stackModel.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var node2 = stackModel.CreateUnaryStatementNode(UnaryOperatorKind.PostDecrement, 1);
            var node3 = stackModel.CreateStackedNode<Type0FakeNodeModel>("B", 2);

            var variableNode = GraphModel.CreateVariableNode(
                ScriptableObject.CreateInstance<VariableDeclarationModel>(), Vector2.zero);

            GraphModel.CreateEdge(node2.InputPortModels.First(), variableNode.OutputPortModels.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(node1, Has.IndexInStack(0, stackModel));

                    var unary = stackModel.NodeModels.ElementAt(1);
                    Assert.That(node2, Is.TypeOf<UnaryOperatorNodeModel>());
                    Assert.That(node3, Has.IndexInStack(2, stackModel));
                    Assert.That(variableNode.OutputPortModels[0], Is.ConnectedTo(unary.InputPortModels[0]));
                    return new ChangeStackedNodeAction(unary, stackModel, Vector2.zero);
                },
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(node1, Has.IndexInStack(0, stackModel));
                    Assert.That(node2.IsStacked, Is.False);
                    Assert.That(node3, Has.IndexInStack(2, stackModel));

                    var newNode = GetStackedNode(0, 1);
                    Assert.That(newNode, Is.Not.Null);
                    Assert.That(newNode, Is.InstanceOf<LoopNodeModel>());
                });
        }

        [Test]
        public void Test_MoveStackedNodeAction_ToSameStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = stack.CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = stack.CreateStackedNode<Type0FakeNodeModel>("C", 2);
            var nodeD = stack.CreateStackedNode<Type0FakeNodeModel>("D", 3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new []{nodeA}, stack, 2);
                },
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] { nodeD }, stack, 0);
                },
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                });
        }

        [Test]
        public void Test_MoveMultipleStackedNodesAction_ToSameStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = stack.CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = stack.CreateStackedNode<Type0FakeNodeModel>("C", 2);
            var nodeD = stack.CreateStackedNode<Type0FakeNodeModel>("D", 3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new []{nodeA, nodeC}, stack, 2);
                },
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeD, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeC, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeD, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeC, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new []{nodeD, nodeC}, stack, 0);
                },
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeB, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeB, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new []{nodeA, nodeB, nodeC, nodeD}, stack, 0);
                },
                () =>
                {
                    Assert.That(stack.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                });
        }

        [Test]
        public void Test_MoveStackedNodeAction_ToDifferentStack([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = stack0.CreateStackedNode<Type0FakeNodeModel>("C", 2);
            var stack1 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeD = stack1.CreateStackedNode<Type0FakeNodeModel>("D", 0);
            var nodeE = stack1.CreateStackedNode<Type0FakeNodeModel>("E", 1);
            var nodeF = stack1.CreateStackedNode<Type0FakeNodeModel>("F", 2);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new []{nodeA}, stack1, 1);
                },
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(2));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(nodeC, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    Assert.That(nodeF, Has.IndexInStack(3, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(2));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(nodeC, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    Assert.That(nodeF, Has.IndexInStack(3, stack1));
                    return new MoveStackedNodesAction(new []{nodeF}, stack0, 0);
                },
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                });
        }

        [Test]
        public void Test_MoveMultipleStackedNodesAction_ToDifferentStack([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = stack0.CreateStackedNode<Type0FakeNodeModel>("C", 2);
            var stack1 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeD = stack1.CreateStackedNode<Type0FakeNodeModel>("D", 0);
            var nodeE = stack1.CreateStackedNode<Type0FakeNodeModel>("E", 1);
            var nodeF = stack1.CreateStackedNode<Type0FakeNodeModel>("F", 2);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new []{nodeA, nodeC}, stack1, 1);
                },
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(5));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeC, Has.IndexInStack(2, stack1));
                    Assert.That(nodeE, Has.IndexInStack(3, stack1));
                    Assert.That(nodeF, Has.IndexInStack(4, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(5));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeC, Has.IndexInStack(2, stack1));
                    Assert.That(nodeE, Has.IndexInStack(3, stack1));
                    Assert.That(nodeF, Has.IndexInStack(4, stack1));
                    return new MoveStackedNodesAction(new []{nodeF, nodeD}, stack0, 0);
                },
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeD, Has.IndexInStack(1, stack0));
                    Assert.That(nodeB, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeC, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeD, Has.IndexInStack(1, stack0));
                    Assert.That(nodeB, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeC, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new []{nodeF, nodeC}, stack1, 2);
                },
                () =>
                {
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(2));
                    Assert.That(nodeD, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    Assert.That(nodeC, Has.IndexInStack(3, stack1));
                });
        }
        [Test]
        public void Test_SplitStackAction([Values] TestingMode mode)
        {
            GraphModel.CreateStack("stack0", Vector2.zero);
            GraphModel.CreateStack("stack1", Vector2.zero);
            GraphModel.CreateStack("stack2", Vector2.zero);
            var stack0 = GetStack(0);
            var stack1 = GetStack(1);
            var stack2 = GetStack(2);
            var nodeA = stack1.CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = stack1.CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = stack1.CreateStackedNode<Type0FakeNodeModel>("C", 2);

            GraphModel.CreateEdge(stack1.InputPortModels.First(), stack0.OutputPortModels.First());
            GraphModel.CreateEdge(stack2.InputPortModels.First(), stack1.OutputPortModels.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(stack1, Is.ConnectedToStack(stack2));
                    Assert.That(nodeA, Is.InsideStack(stack1));
                    Assert.That(nodeB, Is.InsideStack(stack1));
                    Assert.That(nodeC, Is.InsideStack(stack1));
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(stack2.NodeModels.Count(), Is.EqualTo(0));
                    return new SplitStackAction(stack1, 1);
                },
                () =>
                {
                    var stack3 = GetStack(3);
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(GetStackCount(), Is.EqualTo(4));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(stack1, Is.ConnectedToStack(stack3));
                    Assert.That(stack3, Is.ConnectedToStack(stack2));
                    Assert.That(nodeA, Is.InsideStack(stack1));
                    Assert.That(nodeB, Is.InsideStack(stack3));
                    Assert.That(nodeC, Is.InsideStack(stack3));
                    Assert.That(stack0.NodeModels.Count(), Is.EqualTo(0));
                    Assert.That(stack1.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(stack2.NodeModels.Count(), Is.EqualTo(0));
                    Assert.That(stack3.NodeModels.Count(), Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_MergeStackAction([Values] TestingMode mode)
        {
            GraphModel.CreateStack("stack0", Vector2.zero);
            GraphModel.CreateStack("stack1", Vector2.zero);
            GraphModel.CreateStack("stack2", Vector2.zero);
            var nodeA = GetStack(0).CreateStackedNode<Type0FakeNodeModel>("A", 0);
            var nodeB = GetStack(0).CreateStackedNode<Type0FakeNodeModel>("B", 1);
            var nodeC = GetStack(1).CreateStackedNode<Type0FakeNodeModel>("C", 0);

            GraphModel.CreateEdge(GetStack(1).InputPortModels.First(), GetStack(0).OutputPortModels.First());
            GraphModel.CreateEdge(GetStack(2).InputPortModels.First(), GetStack(1).OutputPortModels.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    Assert.That(GetStack(0), Is.ConnectedToStack(GetStack(1)));
                    Assert.That(GetStack(1), Is.ConnectedToStack(GetStack(2)));
                    Assert.That(nodeA, Is.InsideStack(GetStack(0)));
                    Assert.That(nodeB, Is.InsideStack(GetStack(0)));
                    Assert.That(nodeC, Is.InsideStack(GetStack(1)));
                    Assert.That(GetStack(0).NodeModels.Count(), Is.EqualTo(2));
                    Assert.That(GetStack(1).NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStack(2).NodeModels.Count(), Is.EqualTo(0));
                    return new MergeStackAction(GetStack(0), GetStack(1));
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetStack(0), Is.ConnectedToStack(GetStack(1)));
                    Assert.That(nodeA, Is.InsideStack(GetStack(0)));
                    Assert.That(nodeB, Is.InsideStack(GetStack(0)));
                    Assert.That(nodeC, Is.InsideStack(GetStack(0)));
                    Assert.That(GetStack(0).NodeModels.Count(), Is.EqualTo(3));
                    Assert.That(GetStack(1).NodeModels.Count(), Is.EqualTo(0));
                });
        }
    }

    class StackActionUITests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        class UniqueInstanceFunctionModel : FunctionModel
        {
            public override bool AllowMultipleInstances => false;
            public override bool IsInstanceMethod => true;
        }

        [UnityTest]
        public IEnumerator Test_DuplicateCommandSkipsSingleInstanceModels()
        {
            GraphModel.CreateStack("stack", new Vector2(200, 50));
            GraphModel.CreateNode<UniqueInstanceFunctionModel>("Start", new Vector2(200, 150));

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(2));

            Helpers.MouseClickEvent(GetGraphElement(0).GetGlobalCenter()); // Ensure we have focus
            yield return null;

            GraphView.selection.Add(GetGraphElement(0));
            GraphView.selection.Add(GetGraphElement(1));

            Helpers.ExecuteCommand("Duplicate");
            yield return null;

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(3), "Duplicate should have skipped the Start event function node");
        }

        [UnityTest]
        public IEnumerator Test_BlackboardLocalScopeShowsAllVariablesFromSelection()
        {
            Stencil stencil = GraphModel.Stencil;

            var functionModel = GraphModel.CreateFunction("function", new Vector2(200, 50));
            var functionVariable  = functionModel.CreateFunctionVariableDeclaration("l", typeof(int).GenerateTypeHandle(stencil));
            var functionParameter = functionModel.CreateFunctionParameterDeclaration("a", typeof(int).GenerateTypeHandle(stencil));
            var functionOutput = functionModel.OutputPortModels[0];

            var stackModel1 = GraphModel.CreateStack("stack1", new Vector2(200, 200));
            var stack1Input = stackModel1.InputPortModels[0];
            var stack1Output = stackModel1.OutputPortModels[0];

            var stackModel2 = GraphModel.CreateStack("stack2", new Vector2(200, 400));
            var stack2Input = stackModel2.InputPortModels[0];

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Store.Dispatch(new CreateEdgeAction(stack1Input, functionOutput));
            yield return null;

            Store.Dispatch(new CreateEdgeAction(stack2Input, stack1Output));
            yield return null;

            GraphView.selection.Add(GetGraphElement(5));
            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Blackboard blackboard = GraphView.UIController.Blackboard;
            var allVariables = GraphView.UIController.GetAllVariableDeclarationsFromSelection(GraphView.selection).ToList();

            Assert.That(allVariables.Find(x => (VariableDeclarationModel)x.Item1 == functionVariable) != null);
            Assert.That(allVariables.Find(x => (VariableDeclarationModel)x.Item1 == functionParameter) != null);

            // Test assumes that section displays
            var blackboardVariableFields = blackboard.Query<BlackboardVariableField>().ToList();

            Assert.That(blackboardVariableFields.Find(x => (VariableDeclarationModel)x.VariableDeclarationModel == functionVariable) != null);
            Assert.That(blackboardVariableFields.Find(x => (VariableDeclarationModel)x.VariableDeclarationModel == functionParameter) != null);
        }
    }
}
