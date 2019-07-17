using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateSetPropertyGroupNodeAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly int Index;

        public CreateSetPropertyGroupNodeAction(IStackModel stackModel, int index)
        {
            StackModel = stackModel;
            Index = index;
        }
    }

    public class CreateGetPropertyGroupNodeAction : IAction
    {
        public readonly Vector2 Position;

        public CreateGetPropertyGroupNodeAction(Vector2 position)
        {
            Position = position;
        }
    }
}
