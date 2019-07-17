using System;
using System.Collections.Concurrent;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class TypeMetadataResolver : ITypeMetadataResolver
    {
        readonly ITypeMetadataFactory m_Factory;

        readonly ConcurrentDictionary<TypeHandle, ITypeMetadata> m_MetadataCache
            = new ConcurrentDictionary<TypeHandle, ITypeMetadata>();

        public TypeMetadataResolver(ITypeMetadataFactory factory)
        {
            m_Factory = factory;
        }

        public ITypeMetadata Resolve(TypeHandle th)
        {
            if (!m_MetadataCache.TryGetValue(th, out ITypeMetadata metadata))
            {
                metadata = m_MetadataCache[th] = m_Factory.Create(th);
            }
            return metadata;
        }
    }

    public interface ITypeMetadataResolver
    {
        ITypeMetadata Resolve(TypeHandle th);
    }
}
