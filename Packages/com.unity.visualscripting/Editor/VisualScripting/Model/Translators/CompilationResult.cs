using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;

namespace  UnityEditor.VisualScripting.Model
{
    public class CompilerQuickFix
    {
        public string description;
        public Action<Store> quickFix;

        public CompilerQuickFix(string description, Action<Store> quickFix)
        {
            this.description = description;
            this.quickFix = quickFix;
        }
    }

    public class CompilerError
    {
        public string description;
        public object sourceNode;
        public int sourceNodeId;
        public CompilerQuickFix quickFix;

        public override string ToString()
        {
            return $"Compiler error: {description}";
        }
    }

    public class CompilationResult
    {
        public CompilationStatus status;
        public string[] sourceCode;
        public Dictionary<Type, string> pluginSourceCode;
        public List<CompilerError> errors = new List<CompilerError>();

        public void AddError(string description)
        {
            AddError(description, null);
        }

        void AddError(string desc, object node)
        {
            errors.Add(new CompilerError { description = desc, sourceNode = node });
            Debug.LogError(desc);
        }
    }

    [PublicAPI]
    public enum AssemblyType
    {
        None,
        Source,
        Memory,
        ILFile
    };
}
