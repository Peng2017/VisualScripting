using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public interface ITypeHandleSerializer : ITypeBasedTypeHandleSerializer, IGraphBasedTypeHandleSerializer{}

    public interface ITypeBasedTypeHandleSerializer
    {
        Type ResolveType(TypeHandle th);
        TypeHandle GenerateTypeHandle(Type t);
    }

    public interface IGraphBasedTypeHandleSerializer
    {
        VSGraphModel ResolveGraph(TypeHandle th);
        TypeHandle GenerateTypeHandle(VSGraphModel vsGraphAssetModel);
    }
}
