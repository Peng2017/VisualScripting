using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Parsing
{
    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    class DeduplicationTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        void JoinStacks(StackModel a, StackModel b, string newStackName, out StackModel newJoinStack)
        {
            newJoinStack = GraphModel.CreateStack(newStackName, Vector2.zero);
            ConnectTo(a, 0, newJoinStack, 0);
            ConnectTo(b, 0, newJoinStack, 0);
        }

        void CreateIfThenElseStacks(StackModel ifStack, string thenName, string elseName, out StackModel thenStack, out StackModel elseStack)
        {
            var ifNode = ifStack.CreateStackedNode<IfConditionNodeModel>("if", 0);

            thenStack = GraphModel.CreateStack(thenName, Vector2.left);
            ConnectTo(ifNode, 0, thenStack, 0);

            elseStack = GraphModel.CreateStack(elseName, Vector2.right);
            ConnectTo(ifNode, 1, elseStack, 0);
        }

        // A
        //B C
        // D
        [Test]
        public void SimpleIf()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c, d;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            JoinStacks(b, c, "d", out d);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(d));
        }

        //       A
        //      / \
        //     B   C
        //     |  / \
        //     |  D  E
        //      \ | /
        //       \|/
        //        F
        [Test]
        public void ThreeWayIf()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c, d, e, f;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(c, "d", "e", out d, out e);
            JoinStacks(d, e, "f", out f);
            ConnectTo(b, 0, f, 0);

            Assert.That(RoslynTranslator.FindCommonDescendant(d, e), Is.EqualTo(f));
            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(f));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \ /   \ /
        //     H     I
        //      \   /
        //        J
        [Test]
        public void TwoLevelIfs()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackModel b, c, d, e, f, g, h, i, j;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);
            JoinStacks(f, g, "i", out i);
            JoinStacks(h, i, "h", out j);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(j));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(f, g), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, f), Is.EqualTo(j));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \  | |  /
        //     \ | | /
        //      \| |/
        //        H
        [Test]
        public void FourWayJoin()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackModel b, c, d, e, f, g, h;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);

            ConnectTo(f, 0, h, 0);
            ConnectTo(g, 0, h, 0);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(f, g), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, f), Is.EqualTo(h));
        }

        //        A
        //      /   \
        //     |     C
        //      \   /
        //        B
        [Test]
        public void IfNoThen()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);

            ConnectTo(c, 0, b, 0);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(b));
        }

        //        A
        //      /   \
        //     B     C
        [Test]
        public void UnjoinedIfHasNoCommonDescendant()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.Null);
        }

        //        A
        //      /   \
        //     |     C
        //      \   / \
        //        B
        [Test]
        public void IfNoThenElseIfNoThen()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            b.CreateStackedNode<Type0FakeNodeModel>("b", 0);
            c.CreateStackedNode<Type0FakeNodeModel>("c", 0);

            var cIfNode = c.CreateStackedNode<IfConditionNodeModel>("if_c", 1);

            ConnectTo(cIfNode, 0, b, 0);

            // as C has an if node with a disconnect else branch, B cannot be a descendant of both branches
            // so common(b,c) should return null
            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.Null);
        }

        //       A
        //      / \
        //     |   C
        //      \ / \
        //       B   D
        //        \ /
        //         E
        [Test]
        public void NestedIfs()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            var d = GraphModel.CreateStack("d", Vector2.left);
            var e = GraphModel.CreateStack("e", Vector2.left);

            b.CreateStackedNode<Type0FakeNodeModel>("b", 0);
            c.CreateStackedNode<Type0FakeNodeModel>("c", 0);
            d.CreateStackedNode<Type0FakeNodeModel>("d", 0);
            e.CreateStackedNode<Type0FakeNodeModel>("e", 0);

            var cIfNode = c.CreateStackedNode<IfConditionNodeModel>("if_c", 1);

            ConnectTo(cIfNode, 0, b, 0);
            ConnectTo(cIfNode, 1, d, 0);

            ConnectTo(b, 0, e, 0);
            ConnectTo(d, 0, e, 0);

            // as C has an if node, a common descendant of (C,X) must be a descendant of (B,D,X), here E
            Assert.That(RoslynTranslator.FindCommonDescendant(a, c), Is.EqualTo(e));
            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(e));
        }

        //        A
        //      /   \
        //     B     |
        //      \   /
        //        C
        [Test]
        public void IfNoElse()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);


            StackModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);

            ConnectTo(b, 0, c, 0);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(c));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \  | |  /
        //     \ |/  /
        //       H  /
        //       \ /
        //        I
        [Test]
        public void WickedThreeWayJoin()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackModel b, c, d, e, f, g, h, i;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);

            ConnectTo(f, 0, h, 0);
            JoinStacks(h, g, "i", out i);

            Assert.That(RoslynTranslator.FindCommonDescendant(b, c), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(h, g), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(d, f), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(e, f), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(b, g), Is.EqualTo(i));
        }
    }
}
