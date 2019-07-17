using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.VisualScripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Reflection;

namespace UnityEditor.VisualScripting.Model.Compilation
{
    public class RoslynMonoTranslator : RoslynTranslator
    {
        List<MemberDeclarationSyntax> m_AllFields {
            get {
                var instance = (RoslynTranslator)this;
                var fieldname = "m_AllFields";
                BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(RoslynTranslator);
                FieldInfo field = type.GetField(fieldname, flag);
                return (List<MemberDeclarationSyntax>)field.GetValue(instance);
            }
            set {
                var instance = (RoslynTranslator)this ;
                var fieldname = "m_AllFields";
                BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(RoslynTranslator);
                FieldInfo field = type.GetField(fieldname, flag);
                field.SetValue(instance, value);
            }
        }
        List<StatementSyntax> m_EventRegistrations {
            get {
                var instance = (RoslynTranslator)this;
                var fieldname = "m_EventRegistrations";
                BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(RoslynTranslator);
                FieldInfo field = type.GetField(fieldname, flag);
                return (List<StatementSyntax>)field.GetValue(instance);
            }
            set {
                var instance = (RoslynTranslator)this;
                var fieldname = "m_EventRegistrations";
                BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(RoslynTranslator);
                FieldInfo field = type.GetField(fieldname, flag);
                field.SetValue(instance, value);
            }
        }

        public RoslynMonoTranslator(Stencil stencil) : base(stencil) {
 
        }

        protected override Microsoft.CodeAnalysis.SyntaxTree ToSyntaxTree(VSGraphModel graphModel, UnityEngine.VisualScripting.CompilationOptions options) {
            return GenerateStandardSyntaxTree(graphModel, options);
        }

        private Microsoft.CodeAnalysis.SyntaxTree GenerateStandardSyntaxTree(VSGraphModel graphModel, UnityEngine.VisualScripting.CompilationOptions options) {
            //TODO fix graph name, do not use the asset name
            var className = graphModel.TypeName;
            var baseClass = graphModel.Stencil.GetBaseClass().Name;
            var classDeclaration = ClassDeclaration(className)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)));

            if (!String.IsNullOrEmpty(baseClass)) {
                classDeclaration = classDeclaration.WithBaseList(
                        BaseList(
                            SingletonSeparatedList<BaseTypeSyntax>(
                                SimpleBaseType(
                                    IdentifierName(baseClass)))));
            }

            if (graphModel.Stencil.addCreateAssetMenuAttribute) {
                classDeclaration = classDeclaration.WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        IdentifierName("CreateAssetMenu"))
                                    .WithArgumentList(
                                        AttributeArgumentList(
                                            SeparatedList<AttributeArgumentSyntax>(
                                                new SyntaxNodeOrToken[]
                {
                    AttributeArgument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(graphModel.Stencil.fileName)))
                    .WithNameEquals(
                        NameEquals(
                            IdentifierName("fileName"))),
                    Token(SyntaxKind.CommaToken),
                    AttributeArgument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(graphModel.Stencil.menuName)))
                    .WithNameEquals(
                        NameEquals(
                            IdentifierName("menuName")))
                })))))));
            }

            var allMembers = new List<MemberDeclarationSyntax>();
            m_AllFields = new List<MemberDeclarationSyntax>();
            var allRemainingMembers = new List<MemberDeclarationSyntax>();

            foreach (var fieldDecl in graphModel.GraphVariableModels) {
                var fieldSyntaxNode = fieldDecl.DeclareField(this);
                m_AllFields.Add(fieldSyntaxNode);
            }

            var entryPoints = GetEntryPointStacks(graphModel);

            Dictionary<string, MethodDeclarationSyntax> declaredMethods = new Dictionary<string, MethodDeclarationSyntax>();
            foreach (var stack in entryPoints) {
                var entrySyntaxNode = BuildNode(stack);
                foreach (var memberDeclaration in entrySyntaxNode.Cast<MemberDeclarationSyntax>()) {
                    if (memberDeclaration is MethodDeclarationSyntax methodDeclarationSyntax) {
                        string key = methodDeclarationSyntax.Identifier.ToString();
                        declaredMethods.Add(key, methodDeclarationSyntax);
                    } else
                        allRemainingMembers.Add(memberDeclaration);
                }
            }

            allMembers.AddRange(m_AllFields);
            m_AllFields = null;
            allMembers.AddRange(allRemainingMembers);

            if (m_EventRegistrations.Any()) {
                if (!declaredMethods.TryGetValue("Update", out var method)) {
                    method = RoslynBuilder.DeclareMethod("Update", AccessibilityFlags.Public, typeof(void))
                        .WithParameterList(
                            ParameterList(
                                SeparatedList(
                                    Enumerable.Empty<ParameterSyntax>())))
                        .WithBody(Block());

                }

                BlockSyntax blockSyntax = Block(m_EventRegistrations.Concat(method.Body.Statements));

                method = method.WithBody(blockSyntax);
                declaredMethods["Update"] = method;
            }

            allMembers.AddRange(declaredMethods.Values);

            classDeclaration = classDeclaration.AddMembers(allMembers.ToArray());

            var referencedNamespaces = new[]
            {
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Dynamic",
                "System.Linq",
                "Microsoft.CSharp",
                "UnityEngine",
            }.Select(namespaceName => UsingDirective(ParseName(namespaceName)));

            var namespaceAliases = new Dictionary<string, string> {

            }.Select(pair =>
                UsingDirective(ParseName(pair.Key))
                    .WithAlias(NameEquals(
                        IdentifierName(pair.Value))));

            UsingDirectiveSyntax[] usings = referencedNamespaces.Concat(namespaceAliases).ToArray();

            var compilationUnit = CompilationUnit()
                .WithUsings(
                    List(usings))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(classDeclaration)).NormalizeWhitespace();

            return compilationUnit.SyntaxTree;
        }
    }

}
