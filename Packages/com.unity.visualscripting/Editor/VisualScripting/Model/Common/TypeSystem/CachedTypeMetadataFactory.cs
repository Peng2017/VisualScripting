using System;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class CachedTypeMetadataFactory : ITypeMetadataFactory
    {
        readonly ITypeMetadataFactory m_DecoratedFactory;

        public CachedTypeMetadataFactory(ITypeMetadataFactory decoratedFactory)
        {
            m_DecoratedFactory = decoratedFactory;
        }

        public ITypeMetadata Create(TypeHandle th)
        {
            ITypeMetadata metadata = m_DecoratedFactory.Create(th);
            return new CachedTypeMetadata(metadata);
        }

        public bool CanProcessHandle(TypeHandle th)
        {
            return m_DecoratedFactory.CanProcessHandle(th);
        }
    }
}