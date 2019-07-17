using System;

namespace UnityEditor.VisualScripting.Model
{
    public interface IMemberConstrainer
    {
        bool MemberAllowed(MemberInfoValue value);
    }
}