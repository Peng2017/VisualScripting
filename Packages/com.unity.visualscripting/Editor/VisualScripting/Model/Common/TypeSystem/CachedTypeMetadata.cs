using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class CachedTypeMetadata : ITypeMetadata
    {
        readonly ITypeMetadata m_Decorated;

        public TypeHandle TypeHandle { get; }

        string m_FriendlyName;
        public string FriendlyName => m_FriendlyName ?? (m_FriendlyName = m_Decorated.FriendlyName);

        string m_Name;
        public string Name => m_Name ?? (m_Name = m_Decorated.Name);

        string m_Namespace;
        public string Namespace => m_Namespace ?? (m_Namespace = m_Decorated.Namespace);

        List<MemberInfoValue> m_PublicMembers;
        public List<MemberInfoValue> PublicMembers => m_PublicMembers ?? (m_PublicMembers = m_Decorated.PublicMembers);

        List<MemberInfoValue> m_NonPublicMembers;
        public List<MemberInfoValue> NonPublicMembers => m_NonPublicMembers ?? (m_NonPublicMembers = m_Decorated.NonPublicMembers);

        IEnumerable<TypeHandle> m_GenericArguments;
        public IEnumerable<TypeHandle> GenericArguments => m_GenericArguments ?? (m_GenericArguments = m_Decorated.GenericArguments);

        public bool IsEnum => m_Decorated.IsEnum;
        public bool IsClass => m_Decorated.IsClass;
        public bool IsValueType => m_Decorated.IsValueType;

        public bool IsAssignableFrom(ITypeMetadata metadata) => m_Decorated.IsAssignableFrom(metadata);
        public bool IsAssignableFrom(Type type) => m_Decorated.IsAssignableFrom(type);
        public bool IsAssignableFrom(IVSGraphModel graph) => m_Decorated.IsAssignableFrom(graph);

        public bool IsAssignableTo(ITypeMetadata metadata) => m_Decorated.IsAssignableTo(metadata);
        public bool IsAssignableTo(Type type) => m_Decorated.IsAssignableTo(type);
        public bool IsAssignableTo(IVSGraphModel graph) => m_Decorated.IsAssignableTo(graph);

        public bool IsSuperclassOf(ITypeMetadata metadata) => m_Decorated.IsSuperclassOf(metadata);
        public bool IsSuperclassOf(Type type) => m_Decorated.IsSuperclassOf(type);
        public bool IsSuperclassOf(IVSGraphModel graph) => m_Decorated.IsSuperclassOf(graph);

        public bool IsSubclassOf(ITypeMetadata metadata) => m_Decorated.IsSubclassOf(metadata);
        public bool IsSubclassOf(Type type) => m_Decorated.IsSubclassOf(type);
        public bool IsSubclassOf(IVSGraphModel graph) => m_Decorated.IsSubclassOf(graph);

        public CachedTypeMetadata(ITypeMetadata decorated)
        {
            m_Decorated = decorated;
            TypeHandle = decorated.TypeHandle;
        }
    }
}
