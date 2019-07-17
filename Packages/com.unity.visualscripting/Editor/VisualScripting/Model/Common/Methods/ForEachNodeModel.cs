using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public class ForEachNodeModel : LoopNodeModel
    {
        public override bool IsInsertLoop => true;
        public override LoopConnectionType LoopConnectionType => LoopConnectionType.LoopStack;

        public override string InsertLoopNodeTitle => "For Each Loop";
        public override Type MatchingStackType => typeof(ForEachHeaderModel);

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            var typeHandle = typeof(VSArray<Object>).GenerateTypeHandle(Stencil);
            AddDataInput(ForEachHeaderModel.DefaultCollectionName, typeHandle);
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel != null)
            {
                var output = selfConnectedPortModel.Direction == Direction.Input
                    ? m_OutputPortModels[0].ConnectionPortModels.FirstOrDefault()?.NodeModel
                    : otherConnectedPortModel?.NodeModel;
                if (output is ForEachHeaderModel foreachStack)
                {
                    var input = selfConnectedPortModel.Direction == Direction.Input
                        ? otherConnectedPortModel
                        : m_InputPortModels[0].ConnectionPortModels.FirstOrDefault();
                    foreachStack.CreateLoopVariables(input);

                    ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(foreachStack);
                }
            }
        }
    }
}
