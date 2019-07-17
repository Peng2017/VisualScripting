using System;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class CSharpTypeBasedMetadataFactory : ITypeBasedMetadataFactory
    {
        readonly ITypeHandleSerializer m_TypeHandleSerializer;
        readonly CSharpTypeBasedMetadata.FactoryMethod m_FactoryMethod;

        public CSharpTypeBasedMetadataFactory(ITypeHandleSerializer typeHandleSerializer, CSharpTypeBasedMetadata.FactoryMethod factoryMethod)
        {
            m_TypeHandleSerializer = typeHandleSerializer;
            m_FactoryMethod = factoryMethod;
        }

        public ITypeMetadata Create(TypeHandle th)
        {
            //TODO resolve if it's a THIS typeHandle and replace with the proper types.
            return m_FactoryMethod(th, m_TypeHandleSerializer.ResolveType(th));
        }

        public bool CanProcessHandle(TypeHandle th)
        {
            return !string.IsNullOrEmpty(th.Identification);
        }
    }

    public interface ITypeBasedMetadataFactory : ITypeMetadataFactory {}
}
