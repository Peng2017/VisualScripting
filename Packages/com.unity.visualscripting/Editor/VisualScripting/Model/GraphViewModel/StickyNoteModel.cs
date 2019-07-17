using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class StickyNoteModel : ScriptableObject, IStickyNoteModel
    {
        [SerializeField]
        protected string m_Title;

        public StickyNoteModel()
        {
            Title = string.Empty;
            Contents = string.Empty;
            Theme = StickyNoteTheme.Classic;
            TextSize = StickyNoteTextSize.Small;
        }

        public string Title
        {
            get => m_Title;
            private set { if (value != null && m_Title != value) m_Title = value; }
        }

        [SerializeField]
        protected string m_Contents;
        public string Contents
        {
            get => m_Contents;
            private set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        [SerializeField]
        protected StickyNoteTheme m_Theme;
        public StickyNoteTheme Theme
        {
            get => m_Theme;
            private set => m_Theme = value;
        }

        [SerializeField]
        protected StickyNoteTextSize m_TextSize;
        public StickyNoteTextSize TextSize
        {
            get => m_TextSize;
            private set => m_TextSize = value;
        }

        [SerializeField]
        Rect m_Position;

        public Rect Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public void Move(Rect newPosition)
        {
            Undo.RegisterCompleteObjectUndo(this, "Move");
            Position = newPosition;
        }

        public void UpdateBasicSettings(string newTitle, string newContents)
        {
            Undo.RecordObject(this, "Update Basic Settings");
            Title = newTitle;
            Contents = newContents;
        }

        public void UpdateTheme(StickyNoteTheme newTheme)
        {
            Undo.RecordObject(this, "Update theme");
            Theme = newTheme;
        }

        public void UpdateTextSize(StickyNoteTextSize newTextSize)
        {
            Undo.RecordObject(this, "Update Text Size");
            TextSize = newTextSize;
        }

        // Capabilities
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable | CapabilityFlags.Movable;

        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;

        [SerializeField]
        GraphModel m_GraphModel;

        public IGraphModel GraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        public string GetId()
        {
            return GetInstanceID().ToString();
        }

        public void Destroy()
        {
            Undo.DestroyObjectImmediate(this);
        }
    }
}
