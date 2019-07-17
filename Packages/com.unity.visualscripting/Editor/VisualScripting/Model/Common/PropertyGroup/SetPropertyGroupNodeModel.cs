using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, k_Title)]
    public class SetPropertyGroupNodeModel : PropertyGroupBaseNodeModel
    {
        const string k_Title = "Set Property";

        public override string Title => k_Title;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            foreach (var member in Members)
            {
                AddDataInput(member.ToString(), member.Type);
            }
        }
    }
}
