using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    public abstract class LoopNodeModel : NodeModel
    {
        public override string Title => InsertLoopNodeTitle;
        public abstract string InsertLoopNodeTitle { get; }
        public virtual Type MatchingStackType => typeof(StackModel);

        public override string IconTypeString
        {
            get
            {
                if (OutputPortModels.Any())
                {
                    var connectedPortNodeModel = OutputPortModels[0].ConnectionPortModels.FirstOrDefault()?.NodeModel as NodeModel;
                    if (connectedPortNodeModel != null)
                        return connectedPortNodeModel.IconTypeString;
                }

                return "typeLoop";
            }
        }

        protected override void OnDefineNode()
        {
            AddLoopOutputPort("");
        }
    }
}
