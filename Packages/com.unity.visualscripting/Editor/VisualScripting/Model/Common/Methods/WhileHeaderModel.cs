using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model
{
    [VisualScriptingFriendlyName("While")]
    public class WhileHeaderModel : LoopStackModel
    {
        public override string Title => "While Condition is True";

        public override string IconTypeString => "typeWhileLoop";
        public override Type MatchingStackedNodeType => typeof(WhileNodeModel);

        internal const string DefaultConditionName = "Condition";
        const string k_DefaultIndexName = "Index";

        public override List<TitleComponent> BuildTitle()
        {
            IPortModel loopExecutionInputPortModel = InputPortModels?[0];
            IPortModel insertLoopPortModel = loopExecutionInputPortModel?.ConnectionPortModels?.FirstOrDefault();
            INodeModel insertLoopNodeModel = insertLoopPortModel?.NodeModel;
            IPortModel conditionInputPortModel = insertLoopNodeModel?.InputPortModels?.Count > 0 ? insertLoopNodeModel.InputPortModels[0] : null;

            ConditionVariableDeclarationModel.name = conditionInputPortModel?.Name ?? DefaultConditionName;

            return new List<TitleComponent>
            {
                new TitleComponent
                {
                    titleComponentType = TitleComponentType.String,
                    titleObject = "While"
                },
                conditionInputPortModel != null  ?
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.Token,
                        titleComponentIcon = TitleComponentIcon.Condition,
                        titleObject = ConditionVariableDeclarationModel
                    } :
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.String,
                        titleObject = DefaultConditionName
                    },
                new TitleComponent
                {
                    titleComponentType = TitleComponentType.String,
                    titleObject = "is True"
                }
            };
        }

        [SerializeField]
        VariableDeclarationModel m_IndexVariableDeclarationModel;
        public VariableDeclarationModel IndexVariableDeclarationModel => m_IndexVariableDeclarationModel;

        [SerializeField]
        VariableDeclarationModel m_ConditionVariableDeclarationModel;
        VariableDeclarationModel ConditionVariableDeclarationModel => m_ConditionVariableDeclarationModel;

        // TODO allow for static methods
        public override bool IsInstanceMethod => true;

        public override bool AllowChangesToModel => false;

        protected override void OnDefineNode()
        {
            AddInputExecutionPort(null);
            AddExecutionOutputPort(null);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                if (IndexVariableDeclarationModel)
                    hashCode = (hashCode * 397) ^ (IndexVariableDeclarationModel.GetHashCode());
                return hashCode;
            }
        }

        protected override void OnCreateLoopVariables(VariableCreator variableCreator, IPortModel connectedPortModel)
        {
            m_IndexVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(
                k_DefaultIndexName,
                typeof(int).GenerateTypeHandle(Stencil),
                TitleComponentIcon.Index,
                VariableFlags.Generated);
            m_ConditionVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(
                DefaultConditionName,
                typeof(bool).GenerateTypeHandle(Stencil),
                TitleComponentIcon.Condition,
                VariableFlags.Generated);
        }
    }
}
