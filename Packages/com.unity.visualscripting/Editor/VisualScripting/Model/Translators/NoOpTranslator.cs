using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.VisualScripting;
using UnityEditor.VisualScripting.Editor.Plugins;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public class NoOpTranslator: ITranslator
    {
        public bool SupportsCompilation() => false;
        public CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions)
        {
            throw new NotImplementedException();
        }
    }
}
