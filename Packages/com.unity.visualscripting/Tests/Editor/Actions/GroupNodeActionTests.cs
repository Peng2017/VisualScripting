using System;
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
    [Category("GroupNode")]
    [Category("Action")]
    class GroupNodeActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateGroupedNodeFromSearcherAction([Values] TestingMode mode)
        {
            ((TestState)m_Store.GetState()).queryGraphNodeModelMockResult = data => new IGraphElementModel[]
            {
                ((VSGraphModel)data.graphModel).CreateConstantNode("const", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero)
            };

            var group = GraphModel.CreateGroup("group", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    return new CreateGroupedNodeFromSearcherAction(GraphModel, group, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));

                    var nodes = GraphModel.NodeModels.ToList();
                    Assert.That(nodes[0], Is.EqualTo(group));
                    Assert.That(nodes[1], Is.TypeOf<IntConstantModel>());
                    Assert.That(nodes[1].IsGrouped);
                    Assert.That(group.NodeModels.Contains(nodes[1]));
                }
            );
        }

        [Test]
        public void Test_CreateGroupNodeAction([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var node1 = GraphModel.CreateConstantNode("const1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(node0.IsGrouped, Is.False);
                    Assert.That(node1.IsGrouped, Is.False);
                    return new CreateGroupNodeAction("Group", new Vector2(50, 50), node0, node1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(node0.IsGrouped, Is.True);
                    Assert.That(node1.IsGrouped, Is.True);

                    var group = GetAllNodes().OfType<GroupNodeModel>().FirstOrDefault();
                    Assert.NotNull(group);
                    Assert.That(group.NodeModels.Contains(node0), Is.True);
                    Assert.That(group.NodeModels.Contains(node1), Is.True);
                });
        }

        [Test]
        public void Test_DeleteElementsAction_GroupNode([Values] TestingMode mode)
        {
            GraphModel.CreateGroupNode("Group", Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<GroupNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_RenameGroupNodeAction([Values] TestingMode mode)
        {
            const string originalName = "Vimes";
            const string newName = "Vetinari";
            GraphModel.CreateGroupNode(originalName, Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<GroupNodeModel>());
                    Assert.That(GetNode(0).name, Is.EqualTo(originalName));
                    return new RenameGroupNodeAction((GroupNodeModel)GetNode(0), newName);
                },
                () =>
                {
                    Assert.That(GetNode(0).name, Is.EqualTo(newName));
                });
        }

        [Test]
        public void Test_AddToGroupNodeAction_FromFloating([Values] TestingMode mode)
        {
            GraphModel.CreateGroupNode("Group", Vector2.zero);
            var node0 = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var node1 = GraphModel.CreateConstantNode("const1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<GroupNodeModel>());
                    var group = (GroupNodeModel)GetNode(0);
                    Assert.That(group.NodeModels.Count(), Is.EqualTo(0));
                    return new AddToGroupNodeAction(group, node0, node1);
                },
                () =>
                {
                    var group = (GroupNodeModel)GetNode(0);
                    Assert.That(group.NodeModels.Count(), Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_AddToGroupNodeAction_FromOtherGroup([Values] TestingMode mode)
        {
            var g0 = GraphModel.CreateGroupNode("Group0", Vector2.zero);
            GraphModel.CreateGroupNode("Group1", Vector2.zero);
            var node0 = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var node1 = GraphModel.CreateConstantNode("const1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            g0.AddNodes(new []{node0, node1});
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0), Is.TypeOf<GroupNodeModel>());
                    Assert.That(GetNode(1), Is.TypeOf<GroupNodeModel>());
                    var group0 = (GroupNodeModel)GetNode(0);
                    Assert.That(group0.NodeModels.Count(), Is.EqualTo(2));

                    var group1 = (GroupNodeModel)GetNode(1);
                    Assert.That(group1.NodeModels.Count(), Is.EqualTo(0));
                    return new AddToGroupNodeAction(group1, node0);
                },
                () =>
                {
                    var group0 = (GroupNodeModel)GetNode(0);
                    Assert.That(group0.NodeModels.Count(), Is.EqualTo(1));
                    var group1 = (GroupNodeModel)GetNode(0);
                    Assert.That(group1.NodeModels.Count(), Is.EqualTo(1));

                });
        }

        [Test]
        public void Test_RemoveFromGroupNodeAction([Values] TestingMode mode)
        {
            var g0 = GraphModel.CreateGroupNode("Group", Vector2.zero);
            var node0 = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var node1 = GraphModel.CreateConstantNode("const1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            g0.AddNodes(new []{node0, node1});
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<GroupNodeModel>());
                    var group = (GroupNodeModel)GetNode(0);
                    Assert.That(group.NodeModels.Count(), Is.EqualTo(2));
                    return new RemoveFromGroupNodeAction(group, node0);
                },
                () =>
                {
                    var group = (GroupNodeModel)GetNode(0);
                    Assert.That(group.NodeModels.Count(), Is.EqualTo(1));
                });
        }
    }
}
