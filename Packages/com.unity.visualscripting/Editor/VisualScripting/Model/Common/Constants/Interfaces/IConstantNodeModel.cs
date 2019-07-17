using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public interface IConstantNodeModel : INodeModel
    {
        object ObjectValue { get; }
        bool IsLocked { get; }
        Type Type { get; }
    }

    public interface IStringWrapperConstantModel : IConstantNodeModel
    {
        List<string> GetAllInputNames();
        void SetValueFromString(string value);
        string Label { get; }
    }
}
