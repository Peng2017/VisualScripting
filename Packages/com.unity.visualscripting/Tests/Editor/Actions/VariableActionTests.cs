using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Variable")]
    [Category("Action")]
    class VariableActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationAction("toto", true, typeof(int).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(Stencil), Is.EqualTo(typeof(int)));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateGraphVariableDeclarationAction("foo", true, typeof(float).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(Stencil), Is.EqualTo(typeof(int)));
                    Assert.That(GetVariableDeclaration(1).DataType.Resolve(Stencil), Is.EqualTo(typeof(float)));
                });
        }

        [Test]
        public void Test_DuplicateGraphVariableDeclarationsAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(Stencil), true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    return new DuplicateGraphVariableDeclarationsAction(new List<IVariableDeclarationModel>() { declaration0, declaration1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                });
        }

        [Test]
        public void Test_CreateFunctionVariableDeclarationAction([Values] TestingMode mode)
        {
            var method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(0));
                    return new CreateFunctionVariableDeclarationAction(method, "toto", typeof(int).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(1));
                    return new CreateFunctionVariableDeclarationAction(method, "foo", typeof(float).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_CreateFunctionParameterDeclarationAction([Values] TestingMode mode)
        {
            var method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(0));
                    return new CreateFunctionParameterDeclarationAction(method, "toto", typeof(int).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(1));
                    return new CreateFunctionParameterDeclarationAction(method, "foo", typeof(float).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_DuplicateFunctionVariableDeclarationsAction([Values] TestingMode mode)
        {
            var method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            var declaration0 = method.CreateFunctionVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil));
            var declaration1 = method.CreateFunctionVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(Stencil));

            var declaration2 = method.CreateFunctionParameterDeclaration("decl2", typeof(int).GenerateTypeHandle(Stencil));
            var declaration3 = method.CreateFunctionParameterDeclaration("decl3", typeof(int).GenerateTypeHandle(Stencil));

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(2));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(2));
                    return new DuplicateFunctionVariableDeclarationsAction(method, new List<IVariableDeclarationModel>() { declaration0, declaration1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(4));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(4));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(2));
                    return new DuplicateFunctionVariableDeclarationsAction(method, new List<IVariableDeclarationModel>() { declaration2, declaration3 });
                },
                () =>
                {
                    Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(4));
                    Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(4));
                });
        }

        [Test]
        public void Test_CreateConstantNodeAction([Values] TestingMode mode)
        {
            Tuple<Type, Type>[] constants =
            {
                new Tuple<Type, Type>(typeof(bool),       typeof(BooleanConstantNodeModel)),
                new Tuple<Type, Type>(typeof(Color),      typeof(ColorConstantModel)),
                new Tuple<Type, Type>(typeof(int),        typeof(IntConstantModel)),
                new Tuple<Type, Type>(typeof(float),      typeof(FloatConstantModel)),
                new Tuple<Type, Type>(typeof(double),     typeof(DoubleConstantModel)),
                new Tuple<Type, Type>(typeof(InputName),  typeof(InputConstantModel)),
                new Tuple<Type, Type>(typeof(Object),     typeof(ObjectConstantModel)),
                new Tuple<Type, Type>(typeof(Quaternion), typeof(QuaternionConstantModel)),
                new Tuple<Type, Type>(typeof(string),     typeof(StringConstantModel)),
                new Tuple<Type, Type>(typeof(Type),       typeof(TypeConstantModel)),
                new Tuple<Type, Type>(typeof(Vector2),    typeof(Vector2ConstantModel)),
                new Tuple<Type, Type>(typeof(Vector3),    typeof(Vector3ConstantModel)),
                new Tuple<Type, Type>(typeof(Vector4),    typeof(Vector4ConstantModel)),
            };

            for (var i = 0; i < constants.Length; i++)
            {
                var iCopy = i;
                TestPrereqActionPostreq(mode,
                    () =>
                    {
                        var constant = constants[iCopy];
                        Assert.That(GetNodeCount(), Is.EqualTo(iCopy));
                        Assert.That(GetEdgeCount(), Is.EqualTo(0));
                        return new CreateConstantNodeAction("toto", constant.Item1.GenerateTypeHandle(Stencil), Vector2.zero);
                    },
                    () =>
                    {
                        var constant = constants[iCopy];
                        Assert.That(GetNodeCount(), Is.EqualTo(iCopy + 1));
                        Assert.That(GetEdgeCount(), Is.EqualTo(0));
                        Assert.That(GetNode(iCopy), Is.TypeOf(constant.Item2));
                        Assert.That(((ConstantNodeModel)GetNode(iCopy)).Type, Is.EqualTo(constant.Item1));
                    });
            }
        }

        [Test]
        public void Test_CreateVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateVariableNodesAction(declaration, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateVariableNodesAction(declaration, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });
        }

        [Test]
        public void Test_DeleteElementsAction_VariableUsage([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(Stencil), true);

            var node0 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node2 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node3 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node4 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node5 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(node4.InputPortModels[0], node0.OutputPortModels[0]);
            GraphModel.CreateEdge(node4.InputPortModels[1], node2.OutputPortModels[0]);
            GraphModel.CreateEdge(node5.InputPortModels[0], node1.OutputPortModels[0]);
            GraphModel.CreateEdge(node5.InputPortModels[1], node3.OutputPortModels[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    return new DeleteElementsAction(declaration0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new DeleteElementsAction(declaration1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_RenameGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            var variable = GraphModel.CreateGraphVariableDeclaration("toto", typeof(int).GenerateTypeHandle(Stencil), true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).name, Is.EqualTo("toto"));
                    return new RenameElementAction(variable, "foo");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).name, Is.EqualTo("foo"));
                });
        }

        [Test]
        public void Test_ReorderGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(Stencil), true);
            var declaration2 = GraphModel.CreateGraphVariableDeclaration("decl2", typeof(int).GenerateTypeHandle(Stencil), true);
            var declaration3 = GraphModel.CreateGraphVariableDeclaration("decl3", typeof(int).GenerateTypeHandle(Stencil), true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclaration(0), Is.EqualTo(declaration0));
                    Assert.That(GetVariableDeclaration(1), Is.EqualTo(declaration1));
                    Assert.That(GetVariableDeclaration(2), Is.EqualTo(declaration2));
                    Assert.That(GetVariableDeclaration(3), Is.EqualTo(declaration3));
                    return new ReorderGraphVariableDeclarationAction(declaration0, 3);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclaration(0), Is.EqualTo(declaration1));
                    Assert.That(GetVariableDeclaration(1), Is.EqualTo(declaration2));
                    Assert.That(GetVariableDeclaration(2), Is.EqualTo(declaration0));
                    Assert.That(GetVariableDeclaration(3), Is.EqualTo(declaration3));
                });
        }

        [Test]
        public void Test_ConvertVariableNodeToConstantNodeAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            var node0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            IPortModel outputPort = node1.OutputPortModels[0];
            GraphModel.CreateEdge(node0.InputPortModels[0], outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).InputPortModels[0], Is.ConnectedTo(GetNode(1).OutputPortModels[0]));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());
                    return new ConvertVariableNodesToConstantNodesAction(node1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).InputPortModels[0], Is.ConnectedTo(GetNode(1).OutputPortModels[0]));
                    Assert.That(GetNode(1), Is.TypeOf<IntConstantModel>());
                });
        }

        [Test]
        public void Test_ConvertConstantNodeToVariableNodeAction([Values] TestingMode mode)
        {
            var binary = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var constant = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            IPortModel outputPort = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary.InputPortModels[0], outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetNode(0).InputPortModels[0], Is.ConnectedTo(GetNode(1).OutputPortModels[0]));
                    Assert.That(GetNode(1), Is.TypeOf<IntConstantModel>());
                    return new ConvertConstantNodesToVariableNodesAction(constant);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).InputPortModels[0], Is.ConnectedTo(GetNode(1).OutputPortModels[0]));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());
                    Assert.That(((VariableNodeModel)GetNode(1)).DataType, Is.EqualTo(typeof(int).GenerateTypeHandle(Stencil)));
                });
        }

        [Test]
        public void Test_MoveVariableDeclarationAction([Values] TestingMode mode)
        {
            var function0 = GraphModel.CreateFunction("Function0", Vector2.zero);
            var function1 = GraphModel.CreateFunction("Function1", Vector2.zero);
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);

            // Move from graph to function
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(0));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(0));
                    return new MoveVariableDeclarationAction(declaration, function0);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(1));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(0));
                });

            // Move from function to function
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(1));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(0));
                    return new MoveVariableDeclarationAction(declaration, function1);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(0));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(1));
                });

            // Move from function to graph
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(0));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(1));
                    return new MoveVariableDeclarationAction(declaration, GraphModel);
                },
                () =>
                {
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(function0.FunctionVariableModels.Count(), Is.EqualTo(0));
                    Assert.That(function1.FunctionVariableModels.Count(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_ItemizeVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            var variable = GraphModel.CreateVariableNode(declaration, Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

            IPortModel outputPort = variable.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[0], outputPort);
            IPortModel outputPort1 = variable.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[1], outputPort1);
            IPortModel outputPort2 = variable.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[0], outputPort2);
            IPortModel outputPort3 = variable.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[1], outputPort3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(1));
                    Assert.That(variable.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(variable.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(variable.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(variable.OutputPortModels[0], Is.ConnectedTo(binary1.InputPortModels[1]));
                    return new ItemizeVariableNodeAction(variable);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(variable.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(variable.OutputPortModels[0], Is.Not.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(variable.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[0]));
                    Assert.That(variable.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[1]));
                });
        }

        [Test]
        public void Test_ItemizeConstantNodeAction([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode("Constant", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

            IPortModel outputPort = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[0], outputPort);
            IPortModel outputPort1 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[1], outputPort1);
            IPortModel outputPort2 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[0], outputPort2);
            IPortModel outputPort3 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[1], outputPort3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetAllNodes().OfType<IntConstantModel>().Count(), Is.EqualTo(1));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary1.InputPortModels[1]));
                    return new ItemizeConstantNodeAction(constant);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<IntConstantModel>().Count(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[1]));
                });
        }

        [Test]
        public void Test_ItemizeSystemConstantNodeAction([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateNode<SystemConstantNodeModel>("Constant", Vector2.zero, m =>
                {
                    m.ReturnType = typeof(float).GenerateTypeHandle(Stencil);
                    m.DeclaringType = typeof(Mathf).GenerateTypeHandle(Stencil);
                    m.Identifier = "PI";
                });

            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

            IPortModel outputPort = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[0], outputPort);
            IPortModel outputPort1 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary0.InputPortModels[1], outputPort1);
            IPortModel outputPort2 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[0], outputPort2);
            IPortModel outputPort3 = constant.OutputPortModels[0];
            GraphModel.CreateEdge(binary1.InputPortModels[1], outputPort3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetAllNodes().OfType<SystemConstantNodeModel>().Count(), Is.EqualTo(1));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary1.InputPortModels[1]));
                    return new ItemizeSystemConstantNodeAction(constant);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<SystemConstantNodeModel>().Count(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(constant.OutputPortModels[0], Is.ConnectedTo(binary0.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary0.InputPortModels[1]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[0]));
                    Assert.That(constant.OutputPortModels[0], Is.Not.ConnectedTo(binary1.InputPortModels[1]));
                });
        }

        [Test]
        public void Test_ToggleLockConstantNodeAction([Values] TestingMode mode)
        {
            var constant0 = GraphModel.CreateConstantNode("Constant0", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var constant1 = GraphModel.CreateConstantNode("Constant1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var constant2 = GraphModel.CreateConstantNode("Constant2", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.False);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new ToggleLockConstantNodeAction(constant0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new ToggleLockConstantNodeAction(constant1, constant2);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.True);
                    Assert.That(constant2.IsLocked, Is.True);
                });
        }

        [Test]
        public void Test_UpdateTypeAction([Values] TestingMode mode)
        {
            VariableDeclarationModel declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(int).GenerateTypeHandle(Stencil)));
                    return new UpdateTypeAction(declaration, typeof(float).GenerateTypeHandle(Stencil));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(float).GenerateTypeHandle(Stencil)));
                });
        }

        [Test]
        public void Test_UpdateTypeAction_UpdatesVariableReferences([Values] TestingMode mode)
        {
            TypeHandle intType = typeof(int).GenerateTypeHandle(Stencil);
            TypeHandle floatType = typeof(float).GenerateTypeHandle(Stencil);

            VariableDeclarationModel declaration = GraphModel.CreateGraphVariableDeclaration("decl0", intType, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(intType));
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel).Type, Is.EqualTo(typeof(int)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(intType));

                    return new UpdateTypeAction(declaration, floatType);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(floatType));
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel).Type, Is.EqualTo(typeof(float)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(floatType));
                });
        }

        [Test]
        public void Test_UpdateExposedAction([Values] TestingMode mode)
        {
            VariableDeclarationModel declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                    return new UpdateExposedAction(declaration, false);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                    return new UpdateExposedAction(declaration, true);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                });
        }

        [Test]
        public void Test_UpdateRankAction([Values] TestingMode mode)
        {
            TypeHandle intType = typeof(int).GenerateTypeHandle(Stencil);
            TypeHandle intArrayType = intType.MakeVsArrayType(Stencil);
            VariableDeclarationModel declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType.IsVsArrayType(Stencil), Is.False);
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel).Type, Is.EqualTo(typeof(int)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(intType));
                    return new UpdateTypeRankAction(declaration, true);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType.IsVsArrayType(Stencil), Is.True);
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel), Is.Null, "Array type have no initialization values");

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(intArrayType));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType.IsVsArrayType(Stencil), Is.True);
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel), Is.Null, "Array type have no initialization values");
                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(intArrayType));
                    return new UpdateTypeRankAction(declaration, false);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType.IsVsArrayType(Stencil), Is.False);
                    Assert.That(((ConstantNodeModel)declaration.InitializationModel), Is.Not.Null);
                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPortModels.Single().DataType, Is.EqualTo(intType));
                });
        }

        [Test]
        public void Test_UpdateTooltipAction([Values] TestingMode mode)
        {

            VariableDeclarationModel declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(Stencil), true);
            declaration.Tooltip = "asd";
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                    return new UpdateTooltipAction(declaration, "qwe");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                    return new UpdateTooltipAction(declaration, "asd");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                });
        }
    }
}
