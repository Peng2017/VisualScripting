using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public class InlineExpressionNodeModel : NodeModel, IRenamableModel
    {
        public override string Title => Expression;

        public override CapabilityFlags Capabilities => base.Capabilities | CapabilityFlags.Renamable;

        [SerializeField]
        string m_Expression;

        public string Expression
        {
            get => m_Expression;
            set => m_Expression = value;
        }

        protected override void OnDefineNode()
        {
            Parse();
            AddDataOutputPort<float>(null);
        }

        void Parse()
        {
            var parsed = CSharpSyntaxTree.ParseText(m_Expression, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));
            var root = parsed.GetRoot();
            foreach (var id in root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(x => !(x.Parent is InvocationExpressionSyntax))
                .Select(x => x.Identifier.Text).Distinct())
                AddDataInput<float>(id);
        }

        public void Rename(string newName)
        {
            Expression = newName;
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
            DefineNode();
        }
    }
}
