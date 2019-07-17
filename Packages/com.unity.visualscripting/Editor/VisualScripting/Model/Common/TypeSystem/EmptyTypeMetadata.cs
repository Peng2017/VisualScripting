using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class EmptyTypeMetadata : ITypeMetadata
    {
        public static ITypeMetadata Instance { get; } = new EmptyTypeMetadata();
        static readonly List<MemberInfoValue> k_Empty = new List<MemberInfoValue>();

        public TypeHandle TypeHandle => default(TypeHandle);
        public string FriendlyName => string.Empty;
        public string Name => string.Empty;
        public string Namespace => null;
        public List<MemberInfoValue> PublicMembers => k_Empty;
        public List<MemberInfoValue> NonPublicMembers => k_Empty;
        public IEnumerable<TypeHandle> GenericArguments => Enumerable.Empty<TypeHandle>();
        public bool IsEnum => false;
        public bool IsClass => false;
        public bool IsValueType => false;

        public bool IsAssignableFrom(ITypeMetadata metadata) => false;
        public bool IsAssignableFrom(Type type) => false;
        public bool IsAssignableFrom(IVSGraphModel graph) => false;

        public bool IsAssignableTo(ITypeMetadata metadata) => false;
        public bool IsAssignableTo(Type type) => false;
        public bool IsAssignableTo(IVSGraphModel graph) => false;

        public bool IsSuperclassOf(ITypeMetadata metadata) => false;
        public bool IsSuperclassOf(Type type) => false;
        public bool IsSuperclassOf(IVSGraphModel graph) => false;

        public bool IsSubclassOf(ITypeMetadata metadata) => false;
        public bool IsSubclassOf(Type type) => false;
        public bool IsSubclassOf(IVSGraphModel graph) => false;
    }
}
