using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [BranchedNode]
    public class IfConditionNodeModel : NodeModel
    {
        public override string Title => "If";
        public override bool IsCondition => true;

        public override string IconTypeString => "typeIfCondition";

        protected override void OnDefineNode()
        {
            AddDataInput<bool>("Condition");
            AddExecutionOutputPort("Then");
            AddExecutionOutputPort("Else");
        }
    }
}
