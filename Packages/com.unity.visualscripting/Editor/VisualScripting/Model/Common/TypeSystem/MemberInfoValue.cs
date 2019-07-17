using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public struct MemberInfoValue : IEquatable<MemberInfoValue>
    {
        public string Name;
        public TypeHandle UnderlyingType;
        TypeHandle m_ReflectedType;
        MemberTypes m_MemberType;

        public MemberInfoValue(TypeHandle reflectedType, TypeHandle underlyingType, string name, MemberTypes memberType)
        {
            Name = name;
            UnderlyingType = underlyingType;
            m_ReflectedType = reflectedType;
            m_MemberType = memberType;
        }

        public bool Equals(MemberInfoValue other)
        {
            return m_ReflectedType.Equals(other.m_ReflectedType) &&
                   string.Equals(Name, other.Name) &&
                   UnderlyingType.Equals(other.UnderlyingType) &&
                   m_MemberType == other.m_MemberType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MemberInfoValue other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_ReflectedType.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UnderlyingType.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) m_MemberType;
                return hashCode;
            }
        }

        public static bool operator ==(MemberInfoValue left, MemberInfoValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MemberInfoValue left, MemberInfoValue right)
        {
            return !left.Equals(right);
        }
    }

    [PublicAPI]
    public static class MemberInfoValueExtensions
    {
        public static MemberInfoValue ToMemberInfoValue(this MemberInfo mi, Stencil stencil)
        {
            return new MemberInfoValue(
                mi.DeclaringType.GenerateTypeHandle(stencil),
                mi.GetUnderlyingType().GenerateTypeHandle(stencil),
                mi.Name,
                mi.MemberType);
        }

        public static MemberInfoValue ToMemberInfoValue(this MemberInfo mi, ITypeBasedTypeHandleSerializer serializer)
        {
            return new MemberInfoValue(
                serializer.GenerateTypeHandle(mi.ReflectedType),
                serializer.GenerateTypeHandle(mi.GetUnderlyingType()),
                mi.Name,
                mi.MemberType);
        }

        public static MemberInfoValue ToMemberInfoValue(this IVariableDeclarationModel decl, Stencil stencil)
        {
            return new MemberInfoValue(
                stencil.GenerateTypeHandle(decl.GraphModel as VSGraphModel),
                decl.DataType,
                decl.Name,
                MemberTypes.Field);
        }

        public static MemberInfoValue ToMemberInfoValue(this IVariableDeclarationModel decl, IGraphBasedTypeHandleSerializer serializer)
        {
            return new MemberInfoValue(
                serializer.GenerateTypeHandle(decl.GraphModel as VSGraphModel),
                decl.DataType,
                decl.Name,
                MemberTypes.Field);
        }
    }
}
