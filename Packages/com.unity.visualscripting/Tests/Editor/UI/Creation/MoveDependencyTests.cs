using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.UI
{
    class MoveDependencyTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void DeleteNodeDoesRemoveTheDependency()
        {
            var mgr = new PositionDependenciesManager(GraphView, GraphView.window.Preferences);
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            var edge = GraphModel.CreateEdge(operatorModel.InputPortModels[0], intModel.OutputPortModels[0]);
            mgr.AddPositionDependency(edge);
            mgr.Remove(operatorModel, intModel);
            Assert.That(mgr.GetDependencies(operatorModel), Is.Null);
        }

        [UnityTest, Ignore("@theor needs to figure this one out")]
        public IEnumerator EndToEndMoveDependencyWithPanning()
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(100, -100));
            StackModel stackModel1 = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPortModels[0], stackModel0.OutputPortModels[0]);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;
            GraphView.FrameAll();
            yield return null;

            bool needsMouseUp = false;
            try
            {
                using (var scheduler = GraphView.CreateTimerEventSchedulerWrapper())
                {
                    GraphElement stackNode = GraphView.UIController.ModelsToNodeMapping[stackModel0];
                    Vector2 startPos = stackNode.GetPosition().position;
                    Vector2 otherStartPos = stackModel1.Position;
                    Vector2 nodeRect = stackNode.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement, stackNode.layout.center);

                    // Move the movable node.
                    Vector2 pos = nodeRect;
                    Vector2 target = new Vector2(Window.rootVisualElement.layout.xMax - 20, pos.y);
                    needsMouseUp = true;
                    bool changed = false;
                    GraphView.viewTransformChanged += view => changed = true;
                    Helpers.MouseDownEvent(pos);
                    yield return null;


                    Helpers.MouseMoveEvent(pos, target);
                    Helpers.MouseDragEvent(pos, target);
                    yield return null;

                    scheduler.TimeSinceStartup += GraphViewTestHelpers.SelectionDraggerPanInterval;
                    scheduler.UpdateScheduledEvents();

                    Helpers.MouseUpEvent(target);
                    needsMouseUp = false;
                    Assume.That(changed, Is.True);

                    yield return null;

                    Vector2 delta = stackNode.GetPosition().position - startPos;
                    Assert.That(stackModel1.Position, Is.EqualTo(otherStartPos + delta));
                }
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(Vector2.zero);
            }
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesTheConnectedStack([Values] TestingMode mode)
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            StackModel stackModel1 = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPortModels[0], stackModel0.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { stackModel1 }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesTheConnectedLoopStack([Values] TestingMode mode)
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            var loopStack = GraphModel.CreateLoopStack(typeof(WhileHeaderModel), new Vector2(50, 50));
            var whileModel = stackModel0.CreateLoopNode(loopStack, 0);
            GraphModel.CreateEdge(loopStack.InputPortModels[0], whileModel.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { loopStack }
            );
        }

        [UnityTest]
        public IEnumerator MovingAFloatingNodeMovesConnectedToken([Values] TestingMode mode)
        {
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            GraphModel.CreateEdge(operatorModel.InputPortModels[0], intModel.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { operatorModel },
                expectedMovedDependencies: new INodeModel[] { intModel }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesStackedNodeConnectedFloatingNode([Values] TestingMode mode)
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            ReturnNodeModel ret = stackModel0.CreateStackedNode<ReturnNodeModel>("return", 0);
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            GraphModel.CreateEdge(ret.InputPortModels[0], operatorModel.OutputPortModels[0]);
            GraphModel.CreateEdge(operatorModel.InputPortModels[0], intModel.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { operatorModel, intModel }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesConditionStacks([Values] TestingMode mode)
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            IfConditionNodeModel conditionNodeModel = stackModel0.CreateStackedNode<IfConditionNodeModel>("cond", 0);
            StackModel thenStack = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            StackModel elseStack = GraphModel.CreateStack(string.Empty, new Vector2(200, 100));
            StackModel joinStack = GraphModel.CreateStack(string.Empty, new Vector2(-100, 200));
            GraphModel.CreateEdge(thenStack.InputPortModels[0], conditionNodeModel.OutputPortModels[0]);
            GraphModel.CreateEdge(elseStack.InputPortModels[0], conditionNodeModel.OutputPortModels[1]);
            GraphModel.CreateEdge(joinStack.InputPortModels[0], thenStack.OutputPortModels[0]);
            GraphModel.CreateEdge(joinStack.InputPortModels[0], elseStack.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { thenStack, elseStack, joinStack }
            );
        }

        [UnityTest]
        public IEnumerator MovingThenStackDoesntMoveElseOrConditionStacks([Values] TestingMode mode)
        {
            StackModel stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            IfConditionNodeModel conditionNodeModel = stackModel0.CreateStackedNode<IfConditionNodeModel>("cond", 0);
            StackModel thenStack = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            StackModel elseStack = GraphModel.CreateStack(string.Empty, new Vector2(200, 100));
            StackModel joinStack = GraphModel.CreateStack(string.Empty, new Vector2(-100, 200));
            GraphModel.CreateEdge(thenStack.InputPortModels[0], conditionNodeModel.OutputPortModels[0]);
            GraphModel.CreateEdge(joinStack.InputPortModels[0], thenStack.OutputPortModels[0]);
            GraphModel.CreateEdge(joinStack.InputPortModels[0], elseStack.OutputPortModels[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { thenStack },
                expectedMovedDependencies: new INodeModel[] { joinStack },
                expectedUnmovedDependencies: new INodeModel[] { stackModel0, elseStack }
            );
        }

        IEnumerator TestMove(TestingMode mode, Vector2 mouseDelta, INodeModel[] movedNodes,
            INodeModel[] expectedMovedDependencies,
            INodeModel[] expectedUnmovedDependencies = null)
        {
            Vector2 startMousePos = new Vector2(42, 13);
            List<Vector2> initPositions = expectedMovedDependencies.Select(x => x.Position).ToList();
            List<Vector2> initUnmovedPositions = expectedUnmovedDependencies != null ? expectedUnmovedDependencies.Select(x => x.Position).ToList() : new List<Vector2>();

            yield return TestPrereqActionPostreq(mode,
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        INodeModel model = expectedMovedDependencies[i];
                        GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                        Assert.That(model.Position, Is.EqualTo(initPositions[i]));
                        Assert.That(element.GetPosition().position, Is.EqualTo(initPositions[i]));
                    }
                },
                frame =>
                {
                    switch (frame)
                    {
                      case 0:
                          List<ISelectable> selectables = movedNodes.Select(x => GraphView.UIController.ModelsToNodeMapping[x]).Cast<ISelectable>().ToList();
                          GraphView.PositionDependenciesManagers.StartNotifyMove(selectables, startMousePos);
                          GraphView.PositionDependenciesManagers.ProcessMovedNodes(startMousePos + mouseDelta);
                          return TestPhase.WaitForNextFrame;
                      default:
                          GraphView.PositionDependenciesManagers.StopNotifyMove();
                          return TestPhase.Done;
                    }
                },
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        INodeModel model = expectedMovedDependencies[i];
                        GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                        Assert.That(model.Position, Is.EqualTo(initPositions[i] + mouseDelta), () => $"Model {model} was expected to have moved");
                        Assert.That(element.GetPosition().position, Is.EqualTo(initPositions[i] + mouseDelta));
                    }

                    if (expectedUnmovedDependencies != null)
                    {
                        for (int i = 0; i < expectedUnmovedDependencies.Length; i++)
                        {
                            INodeModel model = expectedUnmovedDependencies[i];
                            GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                            Assert.That(model.Position, Is.EqualTo(initUnmovedPositions[i]));
                            Assert.That(element.GetPosition().position, Is.EqualTo(initUnmovedPositions[i]));
                        }
                    }
                }
            );
        }
    }
}
