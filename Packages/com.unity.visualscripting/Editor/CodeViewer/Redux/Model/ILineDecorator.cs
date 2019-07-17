using System;
using UnityEngine;

namespace UnityEditor.CodeViewer {
    interface ILineDecorator
    {
        Texture2D Icon { get; }
        string Tooltip { get; }
    }
}