using System;
using System.Reflection;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public enum NodeCreationMode
    {
        GraphRoot, // added to graph.nodes list
        Orphan, // used as a sub value somewhere (variable init values, stacked nodes, ...)
    }

    public static class VSCreationExtensions
    {
        public static GroupNodeModel CreateGroup(this VSGraphModel graphModel, string name, Vector2 position,
            NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<GroupNodeModel>(name, position)
                : graphModel.CreateNode<GroupNodeModel>(name, position);
            return nodeModel;
        }

        public static StackModel CreateStack(this VSGraphModel graphModel, string name, Vector2 position,
            NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var stackType = graphModel.Stencil.GetDefaultStackModelType();
            if (!typeof(StackModel).IsAssignableFrom(stackType))
                stackType = typeof(StackModel);
            var nodeModel = mode == NodeCreationMode.Orphan
                ? (StackModel)graphModel.CreateOrphanNode(stackType, name, position)
                : (StackModel)graphModel.CreateNode(stackType, name, position);
            return nodeModel;
        }

        public static FunctionModel CreateFunction(this VSGraphModel graphModel, string methodName, Vector2 position,
            NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            graphModel.Stencil.GetSearcherDatabaseProvider().ClearReferenceItemsSearcherDatabases();

            methodName = graphModel.GetUniqueName(methodName);
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<FunctionModel>(methodName, position)
                : graphModel.CreateNode<FunctionModel>(methodName, position);
            return nodeModel;
        }

        public static LoopStackModel CreateLoopStack(this VSGraphModel graphModel, Type loopStackType, Vector2 position,
            NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            INodeModel nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode(loopStackType, "loopStack", position, node => ((LoopStackModel)node).CreateLoopVariables(null))
                : graphModel.CreateNode(loopStackType, "loopStack", position, node => ((LoopStackModel)node).CreateLoopVariables(null));
            return (LoopStackModel)nodeModel;
        }

        public static FunctionRefCallNodeModel CreateFunctionRefCallNode(this StackModel graphModel,
            FunctionModel methodInfo, int index = -1, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanStackedNode<FunctionRefCallNodeModel>(methodInfo.Title, n => n.Function = methodInfo)
                : graphModel.CreateStackedNode<FunctionRefCallNodeModel>(methodInfo.Title, index, n => n.Function = methodInfo);
            return nodeModel;
        }

        public static FunctionRefCallNodeModel CreateFunctionRefCallNode(this VSGraphModel graphModel,
            FunctionModel methodInfo, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<FunctionRefCallNodeModel>(methodInfo.Title, position, n => n.Function = methodInfo)
                : graphModel.CreateNode<FunctionRefCallNodeModel>(methodInfo.Title, position, n => n.Function = methodInfo);
            return nodeModel;
        }

        public static FunctionCallNodeModel CreateFunctionCallNode(this VSGraphModel graphModel, MethodBase methodInfo,
            Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<FunctionCallNodeModel>(methodInfo.Name, position, n => n.MethodInfo = methodInfo)
                : graphModel.CreateNode<FunctionCallNodeModel>(methodInfo.Name, position, n => n.MethodInfo = methodInfo);
            return nodeModel;
        }

        public static FunctionCallNodeModel CreateFunctionCallNode(this StackModel stackModel, MethodInfo methodInfo,
            int index = -1, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            return CreateStackedNode<FunctionCallNodeModel>(stackModel, methodInfo.Name, index, mode,
                n => n.MethodInfo = methodInfo);
        }

        public static InlineExpressionNodeModel CreateInlineExpressionNode(this VSGraphModel graphModel,
            string expression, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            const string nodeName = "inline";
            void Setup(InlineExpressionNodeModel m) => m.Expression = expression;
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<InlineExpressionNodeModel>(nodeName, position, Setup)
                : graphModel.CreateNode<InlineExpressionNodeModel>(nodeName, position, Setup);

            return nodeModel;
        }

        public static UnaryOperatorNodeModel CreateUnaryStatementNode(this StackModel stackModel,
            UnaryOperatorKind kind, int index, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            return CreateStackedNode<UnaryOperatorNodeModel>(stackModel, kind.ToString(), index, mode,
                n => n.kind = kind);
        }

        public static UnaryOperatorNodeModel CreateUnaryOperatorNode(this VSGraphModel graphModel,
            UnaryOperatorKind kind, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<UnaryOperatorNodeModel>(kind.ToString(), position, n => n.kind = kind)
                : graphModel.CreateNode<UnaryOperatorNodeModel>(kind.ToString(), position, n => n.kind = kind);
            return nodeModel;
        }

        public static BinaryOperatorNodeModel CreateBinaryOperatorNode(this VSGraphModel graphModel,
            BinaryOperatorKind kind, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<BinaryOperatorNodeModel>(kind.ToString(), position, n => n.kind = kind)
                : graphModel.CreateNode<BinaryOperatorNodeModel>(kind.ToString(), position, n => n.kind = kind);
            return nodeModel;
        }

        public static IVariableModel CreateVariableNode(this VSGraphModel graphModel,
            IVariableDeclarationModel declarationModel, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            if (declarationModel == null)
                return graphModel.CreateNode<ThisNodeModel>("this", position);

            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<VariableNodeModel>(declarationModel.Title, position, v => v.DeclarationModel = declarationModel)
                : graphModel.CreateNode<VariableNodeModel>(declarationModel.Title, position, v => v.DeclarationModel = declarationModel);
            return nodeModel;
        }

        public static IVariableModel CreateVariableNodeNoUndo(this VSGraphModel graphModel,
            IVariableDeclarationModel declarationModel, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            if (declarationModel == null)
                return graphModel.CreateNodeNoUndo<ThisNodeModel>("this", position);

            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<VariableNodeModel>(declarationModel.Title, position, v => v.DeclarationModel = declarationModel)
                : graphModel.CreateNodeNoUndo<VariableNodeModel>(declarationModel.Title, position, v => v.DeclarationModel = declarationModel);
            return nodeModel;
        }

        public static IConstantNodeModel CreateConstantNode(this VSGraphModel graphModel, string constantName,
            TypeHandle constantTypeHandle, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeType = graphModel.Stencil.GetConstantNodeModelType(constantTypeHandle);
            return CreateConstantNodeModel(graphModel, constantName, nodeType, constantTypeHandle ,position, mode);
        }

        static IConstantNodeModel CreateConstantNodeModel(GraphModel graphModel, string constantName, Type nodeType,
            TypeHandle constantTypeHandle, Vector2 position, NodeCreationMode mode)
        {
            void PreDefineSetup(NodeModel model)
            {
                if (model is EnumConstantNodeModel enumModel)
                {
                    enumModel.value = new EnumValueReference(constantTypeHandle);
                }
            }

            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode(nodeType, constantName, position, PreDefineSetup)
                : graphModel.CreateNode(nodeType, constantName, position, PreDefineSetup);
            return (IConstantNodeModel)nodeModel;
        }

        public static ISystemConstantNodeModel CreateSystemConstantNode(this VSGraphModel graphModel, Type type,
            PropertyInfo propertyInfo, Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            void Setup(SystemConstantNodeModel n)
            {
                n.ReturnType = propertyInfo.PropertyType.GenerateTypeHandle(graphModel.Stencil);
                n.DeclaringType = propertyInfo.DeclaringType.GenerateTypeHandle(graphModel.Stencil);
                n.Identifier = propertyInfo.Name;
            }

            var name = $"{type.FriendlyName(false)} > {propertyInfo.Name}";
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<SystemConstantNodeModel>(name, position, Setup)
                : graphModel.CreateNode<SystemConstantNodeModel>(name, position, Setup);
            return nodeModel;
        }

        public static EventFunctionModel CreateEventFunction(this VSGraphModel graphModel, MethodInfo methodInfo,
            Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            graphModel.Stencil.GetSearcherDatabaseProvider().ClearReferenceItemsSearcherDatabases();

            void Setup(EventFunctionModel e)
            {
                e.EventType = methodInfo.DeclaringType.GenerateTypeHandle(graphModel.Stencil);
            }

            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode(methodInfo.Name, position, (Action<EventFunctionModel>)Setup)
                : graphModel.CreateNode(methodInfo.Name, position, (Action<EventFunctionModel>)Setup);
            return nodeModel;
        }

        public static SetPropertyGroupNodeModel CreateSetPropertyGroupNode(this StackModel stackModel, int index)
        {
            var nodeModel = stackModel.CreateStackedNode<SetPropertyGroupNodeModel>("Set Property", index);
            return nodeModel;
        }

        public static GetPropertyGroupNodeModel CreateGetPropertyGroupNode(this VSGraphModel graphModel,
            Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = mode == NodeCreationMode.Orphan
                ? graphModel.CreateOrphanNode<GetPropertyGroupNodeModel>("Get Property", position):
                graphModel.CreateNode<GetPropertyGroupNodeModel>("Get Property", position);
            return nodeModel;
        }

        public static MakeArrayNodeModel CreateMakeArrayNode(this VSGraphModel graphModel, Vector2 position)
        {
            var nodeModel = graphModel.CreateNode<MakeArrayNodeModel>("Make Array", position);
            return nodeModel;
        }

        public static GroupNodeModel CreateGroupNode(this VSGraphModel graphModel, string name, Vector2 position)
        {
            var nodeModel = graphModel.CreateNode<GroupNodeModel>(name, position);
            return nodeModel;
        }

        public static INodeModel CreateLoopNode(this StackModel stackModel, LoopStackModel loopStackModel, int index,
            NodeCreationMode mode = NodeCreationMode.GraphRoot)
        {
            var nodeModel = CreateStackedNode(stackModel, loopStackModel.MatchingStackedNodeType, loopStackModel.Title, index, mode);
            return nodeModel;
        }

        static INodeModel CreateStackedNode(this StackModel stackModel, Type type, string nodeName, int index,
            NodeCreationMode mode, Action<NodeModel> setup = null)
        {
            return mode == NodeCreationMode.Orphan
                ? stackModel.CreateOrphanStackedNode(type, nodeName, setup)
                : stackModel.CreateStackedNode(type, nodeName, index, setup);
        }

        static T CreateStackedNode<T>(this StackModel stackModel, string nodeName, int index,
            NodeCreationMode mode, Action<T> setup = null) where T : NodeModel
        {
            return mode == NodeCreationMode.Orphan
                ? stackModel.CreateOrphanStackedNode(nodeName, setup)
                : stackModel.CreateStackedNode(nodeName, index, setup);
        }

       public static MacroRefNodeModel CreateMacroRefNode(this VSGraphModel self, VSGraphModel graphModel,
           Vector2 position, NodeCreationMode mode = NodeCreationMode.GraphRoot)
       {
           return mode == NodeCreationMode.Orphan
               ? self.CreateOrphanNode<MacroRefNodeModel>(graphModel.AssetModel.Name, position, n => n.Macro = graphModel)
               : self.CreateNode<MacroRefNodeModel>(graphModel.AssetModel.Name, position, n => n.Macro = graphModel);
       }
    }
}
