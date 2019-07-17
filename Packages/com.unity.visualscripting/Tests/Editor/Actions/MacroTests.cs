using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Macro")]
    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    class MacroTests : BaseFixture
    {
        class IO
        {
            IPortModel[] m_Ports;
            int Count => m_Ports.Length;

            public IO(params IPortModel[] ports)
            {
                m_Ports = ports;
            }

            public void Check(VSGraphModel macroGraphModel, IReadOnlyList<IPortModel> macroRefPorts, ModifierFlags modifierFlags)
            {
                Assert.That(macroRefPorts.Count, Is.EqualTo(macroGraphModel.VariableDeclarations.Count(v => v.Modifiers == modifierFlags)));
                Assert.That(macroRefPorts.Count, Is.EqualTo(Count));
                for (int i = 0; i < Count; i++)
                {
                    if (m_Ports[i] == null)
                        Assert.That(macroRefPorts[i].Connected, Is.False);
                    else
                        Assert.That(macroRefPorts[i], Is.ConnectedTo(m_Ports[i]));
                }
            }
        }

        static readonly MethodInfo k_LogMethodInfo = typeof(Debug).GetMethod("Log", new[] { typeof(object) });
        VariableDeclarationModel m_ADecl;
        VariableDeclarationModel m_BDecl;
        VariableDeclarationModel m_CDecl;
        VSGraphModel m_MacroGraphModel;
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        public override void SetUp()
        {
            base.SetUp();
            m_ADecl = GraphModel.CreateGraphVariableDeclaration("A", typeof(float).GenerateTypeHandle(Stencil), true);
            m_BDecl = GraphModel.CreateGraphVariableDeclaration("B", typeof(float).GenerateTypeHandle(Stencil), true);
            m_CDecl = GraphModel.CreateGraphVariableDeclaration("C", typeof(float).GenerateTypeHandle(Stencil), true);
            var macroAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("macro", null, typeof(VSGraphAssetModel));
            m_MacroGraphModel = macroAssetModel.CreateVSGraph<MacroStencil>("macro");
        }

        [Test]
        public void ExtractMacroIsUndoable()
        {
            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            ConnectTo(binOp, 0, log[0], 0);
            ConnectTo(varA, 0, binOp, 0);
            ConnectTo(varA, 0, binOp, 1);
            Undo.IncrementCurrentGroup();
            TestPrereqActionPostreq(TestingMode.UndoRedo, () =>
            {
                binOp = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().Single();
                Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(binOp.InputPortModels[0], Is.ConnectedTo(varA.OutputPortModels[0]));
                Assert.That(binOp.InputPortModels[1], Is.ConnectedTo(varA.OutputPortModels[0]));
                Assert.That(binOp.OutputPortModels[0], Is.ConnectedTo(log[0].InputPortModels[0]));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { binOp }, Vector2.zero, null);
            }, () =>
            {
                Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));
                var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                Assert.That(macroRef, Is.Not.Null);
                Assert.That(macroRef.InputPortModels[0], Is.ConnectedTo(varA.OutputPortModels[0]));
                Assert.That(macroRef.OutputPortModels[0], Is.ConnectedTo(log[0].InputPortModels[0]));
            });
        }

        [Test]
        public void ExtractTwoNodesConnectedToTheSameNodeDifferentPorts([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateFunction("F", Vector2.zero);
            var set = stack.CreateStackedNode<SetVariableNodeModel>("set", -1);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            ConnectTo(varA, 0, set, 0);
            ConnectTo(varB, 0, set, 1);
            Undo.IncrementCurrentGroup();
            TestPrereqActionPostreq(mode, () =>
            {
                set = stack.NodeModels.OfType<SetVariableNodeModel>().Single();
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(set.InputPortModels[0], Is.ConnectedTo(varA.OutputPortModels[0]));
                Assert.That(set.InputPortModels[1], Is.ConnectedTo(varB.OutputPortModels[0]));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { varA, varB }, Vector2.zero, null);
            }, () =>
            {
                var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                Assert.That(macroRef, Is.Not.Null);
                Assert.That(macroRef.OutputPortModels.Count, Is.EqualTo(2));
                Assert.That(macroRef.OutputPortModels[0], Is.ConnectedTo(set.InputPortModels[0]));
                Assert.That(macroRef.OutputPortModels[1], Is.ConnectedTo(set.InputPortModels[1]));
                Assert.That(macroRef.Macro.Stencil, Is.TypeOf<MacroStencil>());
                Assert.That(((MacroStencil)macroRef.Macro.Stencil).ParentType, Is.EqualTo(GraphModel.Stencil.GetType()));
            });
        }

        [Test]
        public void ExtractSingleNode()
        {
            // a + b

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            ConnectTo(binOp, 0, log[0], 0);
            ConnectTo(varA, 0, binOp, 0);
            ConnectTo(varB, 0, binOp, 1);

            TestExtractMacro(new[] { binOp },
                inputs: new IO(varA.OutputPortModels[0], varB.OutputPortModels[0]),
                outputs: new IO(log[0].InputPortModels[0]));
        }

        [Test]
        public void ExtractSingleNodeWithSameInputsCreatesOnlyOneMacroInput()
        {
            // a + a

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            ConnectTo(binOp, 0, log[0], 0);
            ConnectTo(varA, 0, binOp, 0);
            ConnectTo(varA, 0, binOp, 1);

            TestExtractMacro(new[] { binOp },
                inputs: new IO(varA.OutputPortModels[0]),
                outputs: new IO(log[0].InputPortModels[0]));
        }

        [Test]
        public void ExtractTwoUnrelatedNodes()
        {
            // a/b and a%b

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log, 2);
            var divide = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Divide, Vector2.zero);
            var modulo = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Modulo, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            ConnectTo(divide, 0, log[0], 0);
            ConnectTo(modulo, 0, log[1], 0);
            ConnectTo(varA, 0, divide, 0);
            ConnectTo(varA, 0, modulo, 0);
            ConnectTo(varB, 0, divide, 1);
            ConnectTo(varB, 0, modulo, 1);

            TestExtractMacro(new[] { divide, modulo },
                inputs: new IO(varA.OutputPortModels[0], varB.OutputPortModels[0]),
                outputs: new IO(log[0].InputPortModels[0], log[1].InputPortModels[0]));
        }

        [Test]
        public void ExtractLinkedThreeNodesWithOneSharedInput()
        {
            // a > b && a < c

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var greater = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.GreaterThan, Vector2.zero);
            var lower = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.LessThan, Vector2.zero);
            var and = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.LogicalAnd, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            var varC = GraphModel.CreateVariableNode(m_CDecl, Vector2.zero);

            List<IGraphElementModel> extract = new List<IGraphElementModel>
            {
                greater, lower, and,
            };

            ConnectTo(and, 0, log[0], 0);
            extract.Add(ConnectTo(greater, 0, and, 0));
            extract.Add(ConnectTo(lower, 0, and, 1));

            ConnectTo(varA, 0, greater, 0);
            ConnectTo(varB, 0, greater, 1);

            ConnectTo(varA, 0, lower, 0);
            ConnectTo(varC, 0, lower, 1);

            TestExtractMacro(extract,
                inputs: new IO(varA.OutputPortModels[0], varB.OutputPortModels[0], varC.OutputPortModels[0]),
                outputs: new IO(log[0].InputPortModels[0]));
        }

        void TestExtractMacro(IEnumerable<IGraphElementModel> toExtract, IO inputs, IO outputs)
        {
            MacroRefNodeModel macroRef = GraphModel.ExtractNodesAsMacro(m_MacroGraphModel, Vector2.zero, toExtract);

            Assert.That(macroRef.Macro, Is.EqualTo(m_MacroGraphModel));

            inputs.Check(m_MacroGraphModel, macroRef.InputPortModels, ModifierFlags.ReadOnly);
            outputs.Check(m_MacroGraphModel, macroRef.OutputPortModels, ModifierFlags.WriteOnly);

            CompilationResult compilationResult = GraphModel.Compile(AssemblyType.None, GraphModel.CreateTranslator(), CompilationOptions.Default);
            Assert.That(
                compilationResult.status,
                Is.EqualTo(CompilationStatus.Succeeded));
            Debug.Log(compilationResult.sourceCode[0]);
        }

        void CreateStackAndLogs(out StackModel stack, out FunctionCallNodeModel[] log, int logCount = 1)
        {
            stack = GraphModel.CreateFunction("F", Vector2.zero);
            log = new FunctionCallNodeModel[logCount];
            for (int i = 0; i < logCount; i++)
                log[i] = stack.CreateFunctionCallNode(k_LogMethodInfo);
        }
    }
}
