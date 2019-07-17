using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public interface IVariableInitializer
    {
        bool RequiresInitialization(IVariableDeclarationModel decl);
        bool RequiresInspectorInitialization(IVariableDeclarationModel decl);
    }
}
