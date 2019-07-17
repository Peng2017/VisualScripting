using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using static UnityEditor.VisualScripting.Model.PropertyGroupBaseNodeModel;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Properties")]
    [Category("Action")]
    class PropertiesActionTest : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateSetPropertyGroupNodeAction([Values] TestingMode mode)
        {
            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(0));
                    return new CreateSetPropertyGroupNodeAction(method, 0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                });
        }

        [Test]
        public void Test_DisconnectSetPropertyKeepsPortType([Values] TestingMode mode)
        {
            VariableDeclarationModel trDecl = GraphModel.CreateGraphVariableDeclaration("tr", typeof(Transform).GenerateTypeHandle(Stencil), true);
            IVariableModel trVar = GraphModel.CreateVariableNode(trDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            GraphModel.CreateEdge(setters.InputPortModels.Single(), trVar.OutputPortModels.Single());

            setters.AddMember(
                new TypeMember
                {
                    Path = new List<string> { "position" },
                    Type = Stencil.GenerateTypeHandle(typeof(Vector3))
                }
            );

            IConstantNodeModel constToken = GraphModel.CreateConstantNode("v3", typeof(Vector3).GenerateTypeHandle(Stencil), Vector2.left * 200);
            IEdgeModel edge = GraphModel.CreateEdge(setters.InputPortModels.ElementAt(1), constToken.OutputPortModels.Single());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.InputPortModels.ElementAt(1).DataType.Resolve(Stencil), Is.EqualTo(typeof(Vector3)));
                    return new DeleteElementsAction(edge);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.InputPortModels.ElementAt(1).DataType.Resolve(Stencil), Is.EqualTo(typeof(Vector3)));
                });
        }

        [Test]
        public void Test_RemoveSetPropertyMemberKeepsEdges([Values] TestingMode mode)
        {
            // test that removing a SetProperty member doesn't break the edge with it's Instance
            // i.e. here: check that removing Position keeps the link with Transform

            //               |_TestFunction_____|
            // (Transform)---+-o Set Property   |
            //               | o   Position     |

            VariableDeclarationModel trDecl = GraphModel.CreateGraphVariableDeclaration("tr", typeof(Transform).GenerateTypeHandle(Stencil), true);
            IVariableModel trVar = GraphModel.CreateVariableNode(trDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            GraphModel.CreateEdge(setters.InputPortModels.Single(), trVar.OutputPortModels.Single());

            PropertyInfo propertyToAdd = typeof(Transform).GetProperty("position");

            var newMember = new TypeMember
            {
                Path = new List<string>{propertyToAdd?.Name},
                Type = propertyToAdd.GetUnderlyingType().GenerateTypeHandle(Stencil)
            };

            setters.AddMember(newMember);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.InputPortModels.Count, Is.EqualTo(2));
                    Assert.That(setters.Members.Count, Is.EqualTo(1));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Remove, setters, newMember);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.InputPortModels.Count, Is.EqualTo(1));
                    Assert.That(setters.Members.Count, Is.EqualTo(0));
                });
        }

        [TestCase(TestingMode.Action, 0, typeof(Unknown))]
        [TestCase(TestingMode.UndoRedo, 0, typeof(Unknown))]
        [TestCase(TestingMode.Action, 1, typeof(Transform))]
        [TestCase(TestingMode.UndoRedo, 1, typeof(Transform))]
        public void Test_DisconnectSetPropertyInstanceSetsRightPortType(TestingMode mode, int inputIndex, Type expectedPortType)
        {
            VariableDeclarationModel goDecl = GraphModel.CreateGraphVariableDeclaration("go", typeof(GameObject).GenerateTypeHandle(Stencil), true);
            IVariableModel goVar = GraphModel.CreateVariableNode(goDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            IEdgeModel edge = GraphModel.CreateEdge(setters.InputPortModels.Single(), goVar.OutputPortModels.Single());

            PropertyInfo propertyInfo = typeof(Transform).GetProperty("position");

            var newMember = new TypeMember
            {
                Path = new List<string>{propertyInfo?.Name},
                Type = typeof(Transform).GenerateTypeHandle(Stencil)
            };


            setters.AddMember(newMember);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.InputPortModels.ElementAt(1).DataType, Is.EqualTo(typeof(Transform).GenerateTypeHandle(Stencil)));
                    return new DeleteElementsAction(edge);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.InputPortModels.ElementAt(0).DataType, Is.EqualTo(TypeHandle.ThisType));
                    Assert.That(setters.InputPortModels.ElementAt(1).DataType, Is.EqualTo(typeof(Transform).GenerateTypeHandle(Stencil)));
                });
        }

        [Test]
        public void Test_CreateGetPropertyGroupNodeAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateGetPropertyGroupNodeAction(Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetNode(0), Is.TypeOf<GetPropertyGroupNodeModel>());
                });
        }

        [Test]
        public void Test_EditPropertyGroupNodeAction_AddRemove([Values] TestingMode mode)
        {
            IConstantNodeModel constant = GraphModel.CreateConstantNode("toto", typeof(Vector3).GenerateTypeHandle(Stencil), Vector2.zero);
            GetPropertyGroupNodeModel property = GraphModel.CreateGetPropertyGroupNode(Vector2.zero);
            GraphModel.CreateEdge(property.InputPortModels[0], constant.OutputPortModels[0]);

            Type type = typeof(GameObject);
            MemberInfo memberInfo = type.GetMembers()[0];
            var newMember = new TypeMember
            {
                Path = new List<string>{memberInfo.Name},
                Type = memberInfo.GetUnderlyingType().GenerateTypeHandle(Stencil)
            };


            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.OutputPortModels.Count, Is.EqualTo(0));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Add,
                        property,
                        newMember);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.OutputPortModels.Count, Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.OutputPortModels.Count, Is.EqualTo(1));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Remove,
                        property,
                        newMember);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.OutputPortModels.Count, Is.EqualTo(0));
                });
        }
    }
}
