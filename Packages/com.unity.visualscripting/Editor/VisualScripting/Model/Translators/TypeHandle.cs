using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    [Serializable]
    [PublicAPI]
    public struct TypeHandle : IEquatable<TypeHandle>, IComparable<TypeHandle>
    {
        static CSharpTypeSerializer CSharpSerializer { get; } = new CSharpTypeSerializer();

        //TODO figure how to implement
        public static TypeHandle ExecutionFlow { get; } = new TypeHandle("__EXECUTIONFLOW");
        public static TypeHandle MissingType { get; } = new TypeHandle("__MISSINGTYPE");
        public static TypeHandle ThisType { get; }  = new TypeHandle("__THISTYPE");
        public static TypeHandle Unknown { get; }  = new TypeHandle("__UNKNOWN");
        public static TypeHandle Bool { get; } = GenerateTypeHandle(typeof(bool));
        public static TypeHandle Void { get; } = GenerateTypeHandle(typeof(void));
        public static TypeHandle Char { get; } = GenerateTypeHandle(typeof(char));
        public static TypeHandle Double { get; } = GenerateTypeHandle(typeof(double));
        public static TypeHandle Float { get; } = GenerateTypeHandle(typeof(float));
        public static TypeHandle Int { get; } = GenerateTypeHandle(typeof(int));
        public static TypeHandle Long { get; } = GenerateTypeHandle(typeof(long));
        public static TypeHandle Object { get; } = GenerateTypeHandle(typeof(object));
        public static TypeHandle String { get; } = GenerateTypeHandle(typeof(string));

        public bool IsValid => GraphModelReference != null || !string.IsNullOrEmpty(Identification);

        public VSGraphModel GraphModelReference;
        public string Identification;

        public TypeHandle(VSGraphModel graphModelReference) : this(graphModelReference, string.Empty){}
        public TypeHandle(string identification) : this(null, identification){}
        public TypeHandle(VSGraphModel graphModelReference, string identification)
        {
            GraphModelReference = graphModelReference;
            Identification = identification;
        }

        [Obsolete("Use TypeHandle.GetMetadata.FriendlyName instead")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string FriendlyName(Stencil stencil)
        {
            if (GraphModelReference != null)
                return GraphModelReference.name;
            if (!string.IsNullOrEmpty(Identification))
                return this.Resolve(stencil).FriendlyName();
            return "";
        }

        public bool IsVsArrayType(Stencil stencil)
        {
            return !string.IsNullOrEmpty(Identification) && this.Resolve(stencil).IsVsArrayType();
        }

        public bool IsVsArrayCompatible(Stencil stencil)
        {
            return !string.IsNullOrEmpty(Identification) && this.Resolve(stencil).IsVsArrayCompatible();
        }

        public string Name(Stencil stencil)
        {
            if (GraphModelReference != null)
                return GraphModelReference.name;
            return this.Resolve(stencil).Name;
        }

        public TypeHandle MakeVsArrayType(Stencil stencil)
        {
            return this.Resolve(stencil).MakeVsArrayType().GenerateTypeHandle(stencil);
        }

        public TypeHandle GetVsArrayElementType(Stencil stencil)
        {
            return this.Resolve(stencil).GetVsArrayElementType().GenerateTypeHandle(stencil);
        }

        public bool Equals(TypeHandle other)
        {
            return GraphModelReference == other.GraphModelReference && string.Equals(Identification, other.Identification);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeHandle th && Equals(th);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return ((GraphModelReference != null ? GraphModelReference.GetHashCode() : 0) * 397) ^ (Identification != null ? Identification.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"TypeName:{Identification} // UnityObjectReference:{GraphModelReference}";
        }

        public static bool operator ==(TypeHandle left, TypeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeHandle left, TypeHandle right)
        {
            return !left.Equals(right);
        }

        public IEnumerable<TypeHandle> GetGenericArguments(Stencil stencil)
        {
            foreach (var t in this.Resolve(stencil).GenericTypeArguments)
                yield return t.GenerateTypeHandle(stencil);
        }

        static TypeHandle GenerateTypeHandle(Type type)
        {
            return CSharpSerializer.GenerateTypeHandle(type);
        }

        public int CompareTo(TypeHandle other)
        {
            return string.Compare(Identification, other.Identification, StringComparison.Ordinal);
        }
    }

    [PublicAPI]
    public static class TypeHandleExtensions
    {
        public static TypeHandle GenerateTypeHandle(this Type t, Stencil stencil)
        {
            return t.GenerateTypeHandle(stencil.GraphContext.TypeHandleSerializer);
        }

        public static TypeHandle GenerateTypeHandle(this Type t, ITypeBasedTypeHandleSerializer serializer)
        {
            return serializer.GenerateTypeHandle(t);
        }

        public static TypeHandle GenerateTypeHandle(this IGraphAssetModel asset, Stencil stencil)
        {
            return asset.GenerateTypeHandle(stencil.GraphContext.TypeHandleSerializer);
        }

        public static TypeHandle GenerateTypeHandle(this IGraphAssetModel asset, ITypeHandleSerializer serializer)
        {
            if (asset is VSGraphAssetModel vsGraphAssetModel)
                return serializer.GenerateTypeHandle(vsGraphAssetModel.GraphModel as VSGraphModel);

            return TypeHandle.Unknown;
        }

        public static TypeHandle GenerateTypeHandle(this IGraphModel graphModel, Stencil stencil)
        {
            return stencil.GenerateTypeHandle(graphModel as VSGraphModel);
        }

        public static TypeHandle GenerateTypeHandle(this IGraphModel graphModel, ITypeHandleSerializer serializer)
        {
            return serializer.GenerateTypeHandle(graphModel as VSGraphModel);
        }

        public static Type Resolve(this TypeHandle th, Stencil stencil)
        {
            return th.Resolve(stencil.GraphContext.TypeHandleSerializer);
        }

        public static Type Resolve(this TypeHandle th, ITypeBasedTypeHandleSerializer serializer)
        {
            return serializer.ResolveType(th);
        }

        public static ITypeMetadata GetMetadata(this Type t, Stencil stencil)
        {
            return t.GenerateTypeHandle(stencil).GetMetadata(stencil);
        }

        public static ITypeMetadata GetMetadata(this IGraphAssetModel asset, Stencil stencil)
        {
            return asset.GenerateTypeHandle(stencil).GetMetadata(stencil);
        }

        public static ITypeMetadata GetMetadata(this IGraphModel graphModel, Stencil stencil)
        {
            return graphModel.GenerateTypeHandle(stencil).GetMetadata(stencil);
        }

        public static ITypeMetadata GetMetadata(this TypeHandle th, Stencil stencil)
        {
            return th.GetMetadata(stencil.GraphContext.TypeMetadataResolver);
        }

        public static ITypeMetadata GetMetadata(this TypeHandle th, ITypeMetadataResolver resolver)
        {
            return resolver.Resolve(th);
        }

        public static ITypeMetadata GetMetadata(this Type t, ITypeBasedTypeHandleSerializer serializer,
            ITypeMetadataResolver resolver)
        {
            return t.GenerateTypeHandle(serializer).GetMetadata(resolver);
        }

        public static TypeSyntax ToTypeSyntax(this TypeHandle handle, Stencil stencil)
        {
            if (handle.GraphModelReference != null)
                return handle.GraphModelReference.ToTypeSyntax();
            return handle.Resolve(stencil).ToTypeSyntax();
        }

        public static bool IsAssignableFrom(this TypeHandle self, TypeHandle other, Stencil stencil)
        {
            var selfMetadata = self.GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsAssignableFrom(otherMetadata);
        }

        public static bool IsSubclassOf(this TypeHandle self, TypeHandle other, Stencil stencil)
        {
            var selfMetadata = self.GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsSubclassOf(otherMetadata);
        }
    }
}
