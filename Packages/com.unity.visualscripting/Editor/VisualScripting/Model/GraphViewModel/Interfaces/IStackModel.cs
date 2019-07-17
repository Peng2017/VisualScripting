using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IStackModel : INodeModel
    {
        IEnumerable<INodeModel> NodeModels { get; }
        IFunctionModel OwningFunctionModel { get; }
        bool AcceptNode(Type nodeType);
        bool DelegatesOutputsToNode(out INodeModel del);
        void CleanUp();
    }
}
