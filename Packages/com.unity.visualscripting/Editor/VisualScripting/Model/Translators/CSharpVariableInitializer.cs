using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class CSharpVariableInitializer : IVariableInitializer
    {
        ITypeHandleSerializer m_TypeHandleSerializer;

        public CSharpVariableInitializer(ITypeHandleSerializer typeHandleSerializer)
        {
            m_TypeHandleSerializer = typeHandleSerializer;
        }

        public bool RequiresInitialization(IVariableDeclarationModel decl)
        {
            if (decl == null)
                return false;

            VariableType variableType = decl.VariableType;
            Type dataType = m_TypeHandleSerializer.ResolveType(decl.DataType);

            return (variableType == VariableType.FunctionVariable || variableType == VariableType.GraphVariable) &&
                   (dataType.IsValueType || dataType == typeof(string) || dataType.IsVsArrayType());
        }

        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl)
        {
            Type dataType = m_TypeHandleSerializer.ResolveType(decl.DataType);
            return RequiresInitialization(decl) && !dataType.IsVsArrayType();
        }
    }
}
