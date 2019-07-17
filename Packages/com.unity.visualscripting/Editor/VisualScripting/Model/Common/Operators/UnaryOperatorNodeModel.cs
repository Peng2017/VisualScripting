using System;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class UnaryOperatorNodeModel : NodeModel, IOperationValidator
    {
        public UnaryOperatorKind kind;

        public override string Title => kind.ToString();

        protected override void OnDefineNode()
        {
            AddDataInput<float>("A");
            if (kind == UnaryOperatorKind.LogicalNot || kind == UnaryOperatorKind.Minus)
            {
                AddDataOutputPort<float>("Out");
            }
        }

        public virtual bool HasValidOperationForInput(IPortModel _, TypeHandle typeHandle)
        {
            var type = typeHandle.Resolve(Stencil);
            return TypeSystem.GetOverloadedUnaryOperators(type).Contains(kind);
        }
    }
}
