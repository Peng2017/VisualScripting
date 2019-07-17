using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    [VisualScriptingFriendlyName("For Each")]
    [PublicAPI]
    public class ForEachHeaderModel : LoopStackModel
    {
        public VariableDeclarationModel ItemVariableDeclarationModel { get; private set; }

        public VariableDeclarationModel IndexVariableDeclarationModel { get; private set; }

        public VariableDeclarationModel CountVariableDeclarationModel { get; private set; }

        public VariableDeclarationModel CollectionVariableDeclarationModel { get; private set; }

        public override string Title => "For Each Item In List";

        public override string IconTypeString => "typeForEachLoop";
        public override Type MatchingStackedNodeType => typeof(ForEachNodeModel);

        internal const string DefaultCollectionName = "Collection";
        const string k_DefaultItemName = "Item";
        const string k_DefaultIndexName = "Index";
        const string k_DefaultCountName = "Count";

        public override List<TitleComponent> BuildTitle()
        {
            IPortModel loopExecutionInputPortModel = InputPortModels?[0];
            IPortModel insertLoopPortModel = loopExecutionInputPortModel?.ConnectionPortModels?.FirstOrDefault();
            INodeModel insertLoopNodeModel = insertLoopPortModel?.NodeModel;
            IPortModel collectionInputPortModel = insertLoopNodeModel?.InputPortModels?.Count > 0 ? insertLoopNodeModel.InputPortModels[0] : null;

            CollectionVariableDeclarationModel.name = collectionInputPortModel?.Name ?? DefaultCollectionName;

            return new List<TitleComponent>
            {
                new TitleComponent
                {
                    titleComponentType = TitleComponentType.String,
                    titleObject = "For Each"
                },
                ItemVariableDeclarationModel != null ?
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.Token,
                        titleComponentIcon = TitleComponentIcon.Item,
                        titleObject = ItemVariableDeclarationModel
                    } :
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.String,
                        titleObject = "Item"
                    },
                new TitleComponent
                {
                    titleComponentType = TitleComponentType.String,
                    titleObject = "In"
                },
                collectionInputPortModel != null  ?
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.Token,
                        titleComponentIcon = TitleComponentIcon.Collection,
                        titleObject = CollectionVariableDeclarationModel
                    } :
                    new TitleComponent
                    {
                        titleComponentType = TitleComponentType.String,
                        titleObject = DefaultCollectionName
                    }
            };
        }

        public override bool AllowChangesToModel => false;

        // TODO allow for static methods
        public override bool IsInstanceMethod => true;

        protected override void OnDefineNode()
        {
            AddInputExecutionPort(null);
            AddExecutionOutputPort(null);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                if (ItemVariableDeclarationModel)
                    hashCode = (hashCode * 197) ^ (ItemVariableDeclarationModel.GetHashCode());
                if (IndexVariableDeclarationModel)
                    hashCode = (hashCode * 198) ^ (IndexVariableDeclarationModel.GetHashCode());
                if (CountVariableDeclarationModel)
                    hashCode = (hashCode * 199) ^ (CountVariableDeclarationModel.GetHashCode());
                return hashCode;
            }
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.PortType == PortType.Instance && otherConnectedPortModel != null)
            {
                ItemVariableDeclarationModel.DataType = otherConnectedPortModel.DataType.IsVsArrayType(Stencil)
                    ? otherConnectedPortModel.DataType.GetVsArrayElementType(Stencil)
                    : otherConnectedPortModel.DataType;
                foreach (var usage in ((VSGraphModel)GraphModel).FindUsages(ItemVariableDeclarationModel))
                    usage.UpdateTypeFromDeclaration();
            }

            base.OnConnection(selfConnectedPortModel, otherConnectedPortModel);
        }

        protected override void OnCreateLoopVariables(VariableCreator variableCreator, IPortModel connectedPortModel)
        {
            ItemVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(k_DefaultItemName, typeof(Object).GenerateTypeHandle(Stencil), TitleComponentIcon.Item, VariableFlags.Generated | VariableFlags.Hidden);
            IndexVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(k_DefaultIndexName, typeof(int).GenerateTypeHandle(Stencil), TitleComponentIcon.Index, VariableFlags.Generated);
            CountVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(k_DefaultCountName, typeof(int).GenerateTypeHandle(Stencil), TitleComponentIcon.Count, VariableFlags.Generated);
            CollectionVariableDeclarationModel = variableCreator.DeclareVariable<LoopVariableDeclarationModel>(DefaultCollectionName, typeof(VSArray<Object>).GenerateTypeHandle(Stencil), TitleComponentIcon.Collection, VariableFlags.Generated | VariableFlags.Hidden);
        }
    }
}
