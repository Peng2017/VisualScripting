using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;

namespace UnityEditor.VisualScriptingTests
{
    class Type0FakeNodeModel : NodeModel, IFakeNode
    {
        protected override void OnDefineNode()
        {
            AddDataInput<int>("input0");
            AddDataInput<int>("input1");
            AddDataInput<int>("input2");
            AddDataOutputPort<int>("output0");
            AddDataOutputPort<int>("output1");
            AddDataOutputPort<int>("output2");
        }
    }

    interface IFakeNode: INodeModel{}

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    static class Type0FakeNodeModelExt
    {
        public static IEnumerable<SyntaxNode> BuildGetComponent(this RoslynTranslator translator, IFakeNode model, IPortModel portModel)
        {
            yield return SyntaxFactory.EmptyStatement().WithTrailingTrivia(SyntaxFactory.Comment($"/* {((NodeModel)model).name} */"));
        }
    }
}
