using System;
using System.Threading;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Roslyn
{
    class InstrumentationTests : BaseFixture
    {
        protected override Type CreatedGraphType => typeof(ClassStencil);

        protected override bool CreateGraphOnStartup => true;

        class TestStencil : ClassStencil
        {
            [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
            internal abstract class TestArchetype
            {
                [ModelType(Type = typeof(EventFunctionModel))]
                [CreatedByDefault]
                public abstract void Start();
            }
        }

        [Test]
        public void InstrumentCooldownNodeModelDoesNotThrow()
        {
            var start = GraphModel.CreateEventFunction(typeof(TestStencil.TestArchetype).GetMethod("Start"), Vector2.zero);
            var cooldown = GraphModel.CreateLoopStack(typeof(ForEachHeaderModel), Vector2.down);
            var loopNode = start.CreateLoopNode(cooldown, -1);
            GraphModel.CreateEdge(cooldown.InputPortModels[0], loopNode.OutputPortModels[0]);
            var result = GraphModel.CreateTranslator().TranslateAndCompile(GraphModel, AssemblyType.None, CompilationOptions.Tracing);
            Assert.That(result.status, Is.EqualTo(CompilationStatus.Succeeded));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
