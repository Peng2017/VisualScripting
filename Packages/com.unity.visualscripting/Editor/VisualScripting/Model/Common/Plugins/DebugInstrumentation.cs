using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Plugins
{
    /// <summary>
    /// Instruments the AST such that when the code runs, it sends instrumentation data back to Unity for live display / debugging.
    /// </summary>
    /// <remarks>
    /// The instrumentation data is the following:
    ///<para>
    /// 1- On nodes that "compute a value", we wrap the call in a RecordValue&lt;T&gt; call that will send the computed value back to Unity for display along
    ///    the edge of SemanticModel node. This also registers an execution steps.
    ///    For instance, this:
    /// <code>
    ///    var a = f(10 + 2);
    /// </code>
    ///    will become
    /// <code>
    ///    var a = RecordValue(f(RecordValue(RecordValue(10, _) + RecordValue(2, _), _), _);
    /// </code>
    ///</para>
    ///<para>
    /// 2- All statements in a block might be void-ish statements (void function call, return , ...). In that case, we can't
    /// use RecordValue. If the statement is a return, it's impossible to register the step after its execution, so it needs
    /// to be done before the call and it must specify the number of expected RecordValue() calls preceding it.
    /// <code>PadAndInsert
    ///   return(10 + 2);
    /// </code>
    ///    will become
    /// <code>
    ///    SetLastCallFrame("return", 3);
    ///    return RecordValue(RecordValue(10, _) + RecordValue(2, _), _), _;
    /// </code>
    /// The first call registers the return step 3 steps from now, which will be filled after that by the 3 RecordValue calls.
    /// See <see cref="UnityEditor.VisualScriptingTests.FrameTests"/> for an example.
    ///</para>
    ///</remarks>
    class InstrumentForInEditorDebugging : CSharpSyntaxRewriter
    {
        Dictionary<int, int> m_SetPropertyIndexes = new Dictionary<int, int>();

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return base.VisitClassDeclaration(node).NormalizeWhitespace();
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return base.VisitMethodDeclaration(node).NormalizeWhitespace();
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            // visit block statements first, then inject
            var visitBlock = base.VisitBlock(node);
            return InjectLastCallFrameStatements((BlockSyntax)visitBlock);
        }

        // CAREFUL don't just return the result of an Instrument() call in a visit override
        // return Instrument(node) ?? node at least
        [CanBeNull]
        SyntaxNode Instrument(ExpressionSyntax astNode)
        {
            if (astNode == null)
                return null;

            if (!astNode.HasAnnotations(Annotations.AnnotationKind))
                return null;

            var annotation = astNode.GetAnnotations(Annotations.AnnotationKind).FirstOrDefault();
            if (annotation == null)
                return null;

            var instanceID = Convert.ToInt32(annotation.Data);
            var node = EditorUtility.InstanceIDToObject(instanceID) as NodeModel;
            Type returnType = null;

            // must handle specific types
            if (node is FunctionCallNodeModel functionCallNodeModel)
            {
                if (functionCallNodeModel.OutputPortModels.Count > 0)
                    returnType = functionCallNodeModel.OutputPortModels[0].DataType.Resolve(node.GraphModel.Stencil);
                else
                    returnType = typeof(void);
            }
            else if (node is SetPropertyGroupNodeModel)
            {
                if (m_SetPropertyIndexes.ContainsKey(instanceID))
                    m_SetPropertyIndexes[instanceID] = m_SetPropertyIndexes[instanceID] + 1;
                else
                    m_SetPropertyIndexes[instanceID] = 1;
            }

            if (node == null || returnType == typeof(void))
            {
                return null;
            }

            return RecordValue(astNode, null, node);
        }

        static InvocationExpressionSyntax RecordValue(ExpressionSyntax expression, Type returnType, NodeModel node)
        {
            var graph = (GraphModel)node.GraphModel;
            SimpleNameSyntax name;
            if (returnType != null)
                name = SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("RecordValue"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(returnType.Name))));
            else
                name = SyntaxFactory.IdentifierName("RecordValue");

            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Debugger"),
                        name))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[]
                            {
                                SyntaxFactory.Argument(
                                    expression),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(node.GetInstanceID()))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(graph.GetInstanceID()))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("Time"),
                                        SyntaxFactory.IdentifierName("frameCount")))
                            })))
                // this is critical
                .WithAdditionalAnnotations(new SyntaxAnnotation(Annotations.RecordValueKind));
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            var visitedNode = base.VisitQualifiedName(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var visitedNode = base.VisitMemberAccessExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            var visitedNode = base.VisitLiteralExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            var annotation = node.GetAnnotations(Annotations.AnnotationKind).FirstOrDefault();
            if (annotation == null)
                return base.VisitWhileStatement(node);

            var instrumentedExpression = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
                node.Condition).WithAdditionalAnnotations(annotation);

            node = node.WithCondition(instrumentedExpression);

            return base.VisitWhileStatement(node);
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var visitedNode = base.VisitBinaryExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        // THIS IS THE RIGHT WAY TO DO IT TO SUPPORT RECURSION
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccessor)
            {
                // do not instrument calls to debugger
                if (memberAccessor.Expression.ToString() == "Debugger")
                    return node;
            }

            var visitedNode = base.VisitInvocationExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            var visitedNode = base.VisitCastExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var visitedNode = base.VisitPostfixUnaryExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // DO NOT instrument the assignment left side - Record(x) = Record(42) won't work
            ExpressionSyntax left = node.Left;
            SyntaxToken operatorToken = VisitToken(node.OperatorToken);
            ExpressionSyntax right = (ExpressionSyntax) Visit(node.Right);
            return Instrument(node.Update(left, operatorToken, right)) ?? node;
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var visitedNode = base.VisitObjectCreationExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            var visitedNode = base.VisitParenthesizedExpression(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitArgument(ArgumentSyntax node)
        {
            if (node.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword || node.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword)
                return node;

            var visitedNode = base.VisitArgument(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var visitedNode = base.VisitIdentifierName(node);
            var instrumentedNode = Instrument(visitedNode as ExpressionSyntax);
            return instrumentedNode ?? visitedNode;
        }

        static ExpressionStatementSyntax BuildLastCallFrameExpression(string instanceId, int recordedValuesCount)
        {
            NodeModel node = EditorUtility.InstanceIDToObject(Convert.ToInt32(instanceId)) as NodeModel;
            Object graph = null;
            if (node)
            {
                graph = node.GraphModel as GraphModel;
            }

            if (!node || !graph)
                return null;
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Debugger"),
                            SyntaxFactory.IdentifierName("SetLastCallFrame")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(node.GetInstanceID()))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(graph.GetInstanceID()))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("Time"),
                                            SyntaxFactory.IdentifierName("frameCount"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(node ? (node.name ?? node.GetType().Name) : "??"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(recordedValuesCount))),
                                }))));
        }

        static BlockSyntax InjectLastCallFrameStatements(BlockSyntax blockNode)
        {
            var allStatements = blockNode.Statements;
            List<StatementSyntax> list = new List<StatementSyntax>();

            SyntaxAnnotation currentBlockAnnotation = blockNode.GetAnnotations(Annotations.AnnotationKind).FirstOrDefault();
            if (currentBlockAnnotation != null)
            {
                var buildLastCallFrameExpression = BuildLastCallFrameExpression(currentBlockAnnotation.Data, 0);
                if (buildLastCallFrameExpression != null)
                    list.Add(buildLastCallFrameExpression);
            }

            foreach (var syntaxNode in allStatements)
            {
                SyntaxAnnotation annotation = syntaxNode.GetAnnotations(Annotations.AnnotationKind).FirstOrDefault();

                // the recordValue annotation is added to expressionSyntax nodes, but stacked nodes are
                // wrapped in ExpressionStatementSyntax nodes when added to a stack's BlockSyntax.
                SyntaxAnnotation recordValueAnnotation = null;
                if (syntaxNode is ExpressionStatementSyntax ess)
                    recordValueAnnotation = ess.Expression.GetAnnotations(Annotations.RecordValueKind).FirstOrDefault();

                // if the node is already wrapped in a recordValue(), we're done
                if (annotation == null || recordValueAnnotation != null)
                {
                    list.Add(syntaxNode);
                    continue;
                }

                int recordedValuesCount = syntaxNode.GetAnnotatedNodes(Annotations.RecordValueKind).Count();
                ExpressionStatementSyntax buildLastCallFrameExpression = BuildLastCallFrameExpression(annotation.Data, recordedValuesCount);
                if (buildLastCallFrameExpression != null)
                    list.Add(buildLastCallFrameExpression);
                list.Add(syntaxNode);
            }

            return blockNode.WithStatements(SyntaxFactory.List(list));
        }
    }
}
