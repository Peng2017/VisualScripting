using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    //[SearcherItem(typeof(ClassStencil), SearcherContext.Graph, "Events/On Key Down Event")]
    public class KeyDownEventModel : FunctionModel, IEventFunctionModel
    {
        public override bool IsInstanceMethod => true;

        [PublicAPI]
        public enum EventMode { Held, Pressed, Released }

        const string k_Title = "On Key Event";

        public override string Title => k_Title;
        public override bool AllowMultipleInstances => true;

        public EventMode mode;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddDataInput<KeyCode>("key");
        }
    }
}
