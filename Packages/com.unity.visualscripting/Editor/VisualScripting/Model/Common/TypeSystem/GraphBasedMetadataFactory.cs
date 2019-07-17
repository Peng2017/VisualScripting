using System;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class GraphBasedMetadataFactory : IGraphBasedMetadataFactory
    {
        readonly GraphBasedMetadata.FactoryMethod m_FactoryMethod;

        public GraphBasedMetadataFactory(GraphBasedMetadata.FactoryMethod factoryMethod)
        {
            m_FactoryMethod = factoryMethod;
        }

        public ITypeMetadata Create(TypeHandle th)
        {
            return m_FactoryMethod(th, th.GraphModelReference);
        }

        public bool CanProcessHandle(TypeHandle th)
        {
            return th.GraphModelReference != null;
        }
    }

    public interface IGraphBasedMetadataFactory : ITypeMetadataFactory {}
}
