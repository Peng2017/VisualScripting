using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class PermissiveMemberConstrainer : IMemberConstrainer
    {
        public bool MemberAllowed(MemberInfoValue value)
        {
            return true;
        }
    }
}