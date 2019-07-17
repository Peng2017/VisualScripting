using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class GraphBasedMetadata : ITypeMetadata
    {
        readonly ITypeHandleSerializer m_Serializer;
        readonly ITypeMetadataResolver m_Resolver;
        readonly IVSGraphModel m_GraphModel;
        public TypeHandle TypeHandle { get; }

        public delegate GraphBasedMetadata FactoryMethod(TypeHandle typeHandle, IVSGraphModel graphModel);
        public GraphBasedMetadata(ITypeHandleSerializer serializer, TypeHandle typeHandle, IVSGraphModel graphModel)
        {
            m_Serializer = serializer;
            m_GraphModel = graphModel;
            TypeHandle = typeHandle;
        }

        //TODO implement a better FriendlyName
        public string FriendlyName => Name;
        public string Name => m_GraphModel.AssetModel.Name;
        public string Namespace => string.Empty;
        public IEnumerable<TypeHandle> GenericArguments => Enumerable.Empty<TypeHandle>();
        public bool IsEnum => false;
        public bool IsClass => true;
        public bool IsValueType => false;


        public bool IsAssignableFrom(ITypeMetadata metadata) => metadata.IsAssignableTo(m_GraphModel);
        public bool IsAssignableFrom(Type type) => false;
        public bool IsAssignableFrom(IVSGraphModel graph) => m_GraphModel == graph;

        public bool IsAssignableTo(ITypeMetadata metadata) => metadata.IsAssignableFrom(m_GraphModel);
        public bool IsAssignableTo(Type type) => type.IsAssignableFrom(typeof(object));
        public bool IsAssignableTo(IVSGraphModel graph) => m_GraphModel == graph;

        //Since it is a graph, it cannot be derived and hence not be a superclass.
        public bool IsSuperclassOf(ITypeMetadata metadata) => false;
        public bool IsSuperclassOf(Type type) => false;
        public bool IsSuperclassOf(IVSGraphModel graph) => false;

        public bool IsSubclassOf(ITypeMetadata metadata) => metadata.IsSuperclassOf(m_GraphModel);
        public bool IsSubclassOf(Type type) => type == typeof(object);
        public bool IsSubclassOf(IVSGraphModel graph) => false;

        public List<MemberInfoValue> PublicMembers => MemberInfoDtos(BindingFlags.Public);
        public List<MemberInfoValue> NonPublicMembers => MemberInfoDtos(BindingFlags.NonPublic);

        List<MemberInfoValue> MemberInfoDtos(BindingFlags flags)
        {
            return GetFields(flags)
                //TODO validate that the ordering is required
                .OrderBy(m => m.Name)
                .ToList();
        }

        IEnumerable<MemberInfoValue> GetFields(BindingFlags flags)
        {
            if((flags & BindingFlags.Public) != 0)
                return m_GraphModel.GraphVariableModels.Where(v => v.IsExposed)
                                                      .Select(v => v.ToMemberInfoValue(m_Serializer));

            if ((flags & BindingFlags.NonPublic) != 0)
                return m_GraphModel.GraphVariableModels.Where(v => !v.IsExposed)
                                                      .Select(v => v.ToMemberInfoValue(m_Serializer));
            return Enumerable.Empty<MemberInfoValue>();
        }
    }
}
