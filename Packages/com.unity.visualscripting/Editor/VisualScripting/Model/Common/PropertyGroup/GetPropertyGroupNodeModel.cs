using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, k_Title)]
    public class GetPropertyGroupNodeModel : PropertyGroupBaseNodeModel
    {
        const string k_Title = "Get Property";

        public override string Title => k_Title;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddMemberPorts(Members);
        }

        void AddMemberPorts(IEnumerable<TypeMember> membersList)
        {
            foreach (var member in membersList)
            {
                AddDataOutputPort(member.ToString(), member.Type);
            }
        }
    }
}
