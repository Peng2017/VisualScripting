using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEditor.VisualScripting.Model.Services
{
    static class UniqueNameGenerator
    {
        public static string CreateUniqueVariableName(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, string baseName)
        {
            var contextNode = syntaxTree.GetRoot();

            var symbols = contextNode.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(n => n.Identifier.ValueText).ToList();
            symbols.AddRange(contextNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Select(n => n.Declaration.Variables.FirstOrDefault().Identifier.ValueText).ToList());
            symbols.AddRange(contextNode.DescendantNodes().OfType<FieldDeclarationSyntax>().Select(n => n.Declaration.Variables.FirstOrDefault().Identifier.ValueText).ToList());
            symbols.AddRange(contextNode.DescendantNodes().OfType<ParameterSyntax>().Select(n => n.Identifier.ValueText).ToList());

            return GenerateUniqueName(baseName, string.Empty,
                n => symbols.Where(x => x.Equals(n)).ToArray().Length == 0);
        }

        static string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
        {
            if (!string.IsNullOrEmpty(extension) && extension[0] != '.')
            {
                extension = "." + extension;
            }

            var name = baseName + extension;
            var index = 1;

            // Check for collisions
            while (!canUse(name))
            {
                name = baseName + index + extension;
                index++;
            }

            return name;
        }
    }
}
