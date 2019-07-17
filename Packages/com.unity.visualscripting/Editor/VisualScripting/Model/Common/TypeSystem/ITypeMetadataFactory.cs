using System;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public interface ITypeMetadataFactory
    {
        ITypeMetadata Create(TypeHandle th);
        bool CanProcessHandle(TypeHandle th);
    }
}
