using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class TypeHandleSerializer : ITypeHandleSerializer
    {
        readonly ITypeBasedTypeHandleSerializer m_TypeSerializer;
        readonly IGraphBasedTypeHandleSerializer m_GraphSerializer;

        public TypeHandleSerializer(ITypeBasedTypeHandleSerializer typeSerializer, IGraphBasedTypeHandleSerializer graphSerializer)
        {
            m_TypeSerializer = typeSerializer;
            m_GraphSerializer = graphSerializer;
        }

        public VSGraphModel ResolveGraph(TypeHandle th)
        {
            return m_GraphSerializer.ResolveGraph(th);
        }

        public Type ResolveType(TypeHandle th)
        {
            return m_TypeSerializer.ResolveType(th);
        }

        public TypeHandle GenerateTypeHandle(VSGraphModel vsGraphAssetModel)
        {
            return m_GraphSerializer.GenerateTypeHandle(vsGraphAssetModel);
        }

        public TypeHandle GenerateTypeHandle(Type t)
        {
            return m_TypeSerializer.GenerateTypeHandle(t);
        }
    }

    public class GraphTypeSerializer : IGraphBasedTypeHandleSerializer
    {
        public VSGraphModel ResolveGraph(TypeHandle th)
        {
            return th.GraphModelReference != null ? th.GraphModelReference : null;
        }

        public TypeHandle GenerateTypeHandle(VSGraphModel vsGraphAssetModel)
        {
            return new TypeHandle(vsGraphAssetModel);
        }
    }
}
