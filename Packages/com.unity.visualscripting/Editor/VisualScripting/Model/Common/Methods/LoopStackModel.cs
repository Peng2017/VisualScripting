using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public abstract class LoopStackModel : FunctionModel
    {
        public override bool IsInstanceMethod => true;

        public override bool IsEntryPoint => false;

        public override CapabilityFlags Capabilities => base.Capabilities & ~CapabilityFlags.Renamable;

        public override string IconTypeString => "typeLoop";

        public abstract Type MatchingStackedNodeType { get; }

        public enum TitleComponentType
        {
            String,
            Token
        }

        public enum TitleComponentIcon
        {
            None,
            Collection,
            Condition,
            Count,
            Index,
            Item
        }

        public struct TitleComponent
        {
            public TitleComponentType titleComponentType;
            public object titleObject;
            public TitleComponentIcon titleComponentIcon;
        }

        public abstract List<TitleComponent> BuildTitle();
    }
}
