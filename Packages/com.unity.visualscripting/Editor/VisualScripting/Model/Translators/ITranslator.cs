using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public interface ITranslator
    {
        bool SupportsCompilation();
        CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions);
    }
}
