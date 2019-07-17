using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateStackedTestNodeForDebugAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly TypeHandle NodeTypeHandle;
        public readonly string MethodName;
        public readonly bool IsStatic;

        public CreateStackedTestNodeForDebugAction(IStackModel stackModel, TypeHandle nodeTypeHandle, string methodName, bool isStatic)
        {
            StackModel = stackModel;
            NodeTypeHandle = nodeTypeHandle;
            MethodName = methodName;
            IsStatic = isStatic;
        }
    }

    public class DisconnectNodeAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public DisconnectNodeAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class BypassNodeAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public BypassNodeAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class RemoveNodesAction : IAction
    {
        public readonly INodeModel[] ElementsToRemove;
        public readonly INodeModel[] NodesToBypass;

        public RemoveNodesAction(INodeModel[] nodesToBypass, INodeModel[] elementsToRemove)
        {
            ElementsToRemove = elementsToRemove;
            NodesToBypass = nodesToBypass;
        }
    }

    public class ResetNodeColorAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public ResetNodeColorAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class ChangeNodeColorAction : IAction
    {
        public readonly INodeModel[] NodeModels;
        public readonly Color Color;

        public ChangeNodeColorAction(Color color, params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
            Color = color;
        }
    }

    public class CreateNodeFromSearcherAction : CancellableAction
    {
        public readonly IGraphModel GraphModel;
        public readonly Vector2 GraphPosition;
        public readonly Vector2 MousePosition;

        public CreateNodeFromSearcherAction(IGraphModel graphModel, Vector2 mousePosition, Vector2 graphPosition)
        {
            GraphModel = graphModel;
            GraphPosition = graphPosition;
            MousePosition = mousePosition;
        }
    }

    public class RefactorConvertToFunctionAction : IAction
    {
        public readonly INodeModel NodeToConvert;

        public RefactorConvertToFunctionAction(INodeModel nodeModel)
        {
            NodeToConvert = nodeModel;
        }
    }

    public class RefactorExtractMacroAction : IAction
    {
        public readonly List<IGraphElementModel> Selection;
        public readonly Vector2 Position;
        public readonly string MacroPath;

        public RefactorExtractMacroAction(List<IGraphElementModel> selection, Vector2 position, string macroPath)
        {
            Selection = selection;
            Position = position;
            MacroPath = macroPath;
        }
    }

    public class RefactorExtractFunctionAction : IAction
    {
        public readonly IList<ISelectable> Selection;

        public RefactorExtractFunctionAction(IList<ISelectable> selection)
        {
            Selection = selection;
        }
    }

    [PublicAPI]
    public class CreateMacroRefAction : IAction
    {
        public readonly GraphModel GraphModel;
        public readonly Vector2 Position;

        public CreateMacroRefAction(GraphModel graphModel, Vector2 position)
        {
            GraphModel = graphModel;
            Position = position;
        }
    }
}
