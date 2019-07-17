using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class GraphContext
    {
        public ITypeHandleSerializer TypeHandleSerializer { get; }
        public ITypeMetadataResolver TypeMetadataResolver { get; }
        public IVariableInitializer VariableInitializer  { get; }
        IMemberConstrainer MemberConstrainer { get; }
        CSharpTypeBasedMetadata.FactoryMethod m_CSharpMetadataFactoryMethod;
        GraphBasedMetadata.FactoryMethod m_GraphMetadataFactoryMethod;

        public GraphContext()
        {
            TypeHandleSerializer = CreateTypeHandleSerializer();
            VariableInitializer = CreateVariableInitializer();
            MemberConstrainer = CreateMemberConstrainer();
            TypeMetadataResolver = CreateMetadataResolver();
        }

        protected virtual IMemberConstrainer CreateMemberConstrainer()
        {
            return new PermissiveMemberConstrainer();
        }

        CSharpVariableInitializer CreateVariableInitializer()
        {
            return new CSharpVariableInitializer(TypeHandleSerializer);
        }

        public virtual TypeHandleSerializer CreateTypeHandleSerializer()
        {
            return new TypeHandleSerializer(new CSharpTypeSerializer(), new GraphTypeSerializer());
        }

        ITypeMetadataResolver CreateMetadataResolver()
        {
            m_CSharpMetadataFactoryMethod = (th, t) => new CSharpTypeBasedMetadata(TypeHandleSerializer, MemberConstrainer, th, t);
            var typeBasedMetadataFactory = new CSharpTypeBasedMetadataFactory(TypeHandleSerializer, m_CSharpMetadataFactoryMethod);
            var cachedTypeBasedMetadataFactory = new CachedTypeMetadataFactory(typeBasedMetadataFactory);

            m_GraphMetadataFactoryMethod = (th, t) => new GraphBasedMetadata(TypeHandleSerializer, th, t);
            var graphBasedMetadataFactory = new GraphBasedMetadataFactory(m_GraphMetadataFactoryMethod);

            var typeMetadataFactory = new TypeHandleMetadataFactory(cachedTypeBasedMetadataFactory, graphBasedMetadataFactory);
            return new TypeMetadataResolver(typeMetadataFactory);
        }
    }
}
