using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, k_Title)]
    public class GetInputNodeModel : HighLevelNodeModel
    {
        const string k_Title = "Get Input";

        InputName m_InputName;

        public string MethodName(IPortModel portModel) => portModel.Name;

        public override string Title => k_Title;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddDataInput<InputName>("Input Choice");
            AddDataOutputPort<bool>(nameof(Input.GetButton));
            AddDataOutputPort<float>(nameof(Input.GetAxis));
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public static class GetInputTranslator
    {
        public static IEnumerable<SyntaxNode> BuildGetInput(this RoslynTranslator translator, GetInputNodeModel model, IPortModel portModel)
        {
            var method = BuildCall(translator, model, model.MethodName(portModel), out _);

            yield return method;
        }

        public static ExpressionSyntax BuildCall(RoslynTranslator translator, GetInputNodeModel model, string methodName, out ExpressionSyntax inputName)
        {
            if (model.InputPortModels[0].Connected || model.InputPortModels[0].EmbeddedValue != null)
                inputName = translator.BuildPort(model.InputPortModels[0]).FirstOrDefault() as ExpressionSyntax;
            else
                inputName = RoslynBuilder.EmptyStringLiteralExpression();

            var methodParameters = new[] { SyntaxFactory.Argument(inputName) };

            var method = RoslynBuilder.MethodInvocation(methodName, typeof(Input).ToTypeSyntax(), methodParameters, Enumerable.Empty<TypeSyntax>());
            return method;
        }
    }
}
