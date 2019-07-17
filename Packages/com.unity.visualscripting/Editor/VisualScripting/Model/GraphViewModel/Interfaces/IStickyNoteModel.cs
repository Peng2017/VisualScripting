using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [PublicAPI]
    public enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    [PublicAPI]
    public enum StickyNoteTheme
    {
        Classic,
        Dark,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal
    }

    public interface IStickyNoteModel : IGraphElementModel
    {
        string Title { get; }
        string Contents { get; }
        Rect Position { get; }
        StickyNoteTheme Theme { get; }
        StickyNoteTextSize TextSize { get; }
    }
}
