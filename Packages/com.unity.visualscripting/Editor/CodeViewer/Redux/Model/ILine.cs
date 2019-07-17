using System;
using System.Collections.Generic;

namespace UnityEditor.CodeViewer {
    interface ILine
    {
        int LineNumber { get; }
        string Text { get; }
        IReadOnlyList<ILineDecorator> Decorators  { get; }
    }
}