using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using UnityEditor.VisualScripting.Model;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public interface IPluginHandler
    {
        void Register(Store store, GraphView graphView);
        void Unregister();
    }

    [PublicAPI]
    public interface IRoslynPluginHandler : IPluginHandler
    {
        bool RequiresSemanticModel { get; }
        void Apply(ref Microsoft.CodeAnalysis.SyntaxTree syntaxTree, SemanticModel semanticModel, UnityEngine.VisualScripting.CompilationOptions options);
    }
}
