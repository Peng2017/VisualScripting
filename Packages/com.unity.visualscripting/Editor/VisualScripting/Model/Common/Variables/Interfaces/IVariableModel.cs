using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public interface IVariableModel : INodeModel
    {
        IVariableDeclarationModel DeclarationModel { get; }
    }
}
