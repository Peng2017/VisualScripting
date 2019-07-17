using System;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class TypeHandleMetadataFactory : ITypeMetadataFactory
    {
        ITypeMetadataFactory[] m_Factories;

        public TypeHandleMetadataFactory(params ITypeMetadataFactory[] factories)
        {
            m_Factories = factories;
        }

        public ITypeMetadata Create(TypeHandle th)
        {
            foreach (var factory in m_Factories)
                if (factory.CanProcessHandle(th))
                    return factory.Create(th);

            return EmptyTypeMetadata.Instance;
        }

        public bool CanProcessHandle(TypeHandle th) => true;
    }
}
