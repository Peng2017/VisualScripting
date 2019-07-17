using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, "Control Flow/Return")]
    [BranchedNode]
    public class ReturnNodeModel : NodeModel
    {
        const string k_Title = "Return";

        public override string Title => k_Title;

        protected override void OnDefineNode()
        {
            AddDataInput<Unknown>("value");
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            base.OnConnection(selfConnectedPortModel, otherConnectedPortModel);
            var returnType = ParentStackModel?.OwningFunctionModel?.ReturnType ?? TypeHandle.Unknown;
            ((PortModel)InputPortModels[0]).DataType = returnType;
        }
    }
}
