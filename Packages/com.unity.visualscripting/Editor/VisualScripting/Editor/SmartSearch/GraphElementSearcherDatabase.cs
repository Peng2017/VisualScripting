using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    [Flags]
    public enum MemberFlags
    {
        Constructor,
        Extension,
        Field,
        Method,
        Property
    }

    [PublicAPI]
    public class GraphElementSearcherDatabase
    {
        enum IfConditionMode
        {
            Basic,
            Advanced,
            Complete
        }

        const string k_Constant = "Constant";
        const string k_ControlFlow = "Control Flow";
        const string k_LoopStack = "... Loop Stack";
        const string k_Operator = "Operator";
        const string k_InlineExpression = "Inline Expression";
        const string k_InlineLabel = "10+y";
        const string k_Stack = "Stack";
        const string k_Group = "Group";
        const string k_NewFunction = "Create New Function";
        const string k_FunctionName = "My Function";
        const string k_Sticky = "Sticky Note";
        const string k_Then = "then";
        const string k_Else = "else";
        const string k_IfCondition = "If Condition";
        const string k_FunctionMembers = "Function Members";
        const string k_GraphVariables = "Graph Variables";
        const string k_Function = "Function";
        const string k_Graphs = "Graphs";
        const string k_Macros = "Macros";
        const string k_Macro = "Macro";

        static readonly Vector2 k_StackOffset = new Vector2(320, 120);
        static readonly Vector2 k_ThenStackOffset = new Vector2(-220, 300);
        static readonly Vector2 k_ElseStackOffset = new Vector2(170, 300);
        static readonly Vector2 k_ClosedFlowStackOffset = new Vector2(-25, 450);

        // TODO: our builder methods ("AddStack",...) all use this field. Users should be able to create similar methods. making it public until we find a better solution
        public readonly List<SearcherItem> items;
        readonly Stencil m_Stencil;

        public GraphElementSearcherDatabase(Stencil stencil)
        {
            m_Stencil = stencil;
            items = new List<SearcherItem>();
        }

        public GraphElementSearcherDatabase AddMacros()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(VSGraphAssetModel).Name}");
            List<VSGraphAssetModel> macros = assetGUIDs.Select(assetGuid =>
                AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(AssetDatabase.GUIDToAssetPath(assetGuid)))
                .Where(x =>
                {
                    if (x.GraphModel == null)
                    {
                        Debug.Log("No GraphModel");
                    }else if (x.GraphModel.Stencil == null)
                    {
                        Debug.Log("No Stencil");
                    }else
                        return x.GraphModel.Stencil.GetType() == typeof(MacroStencil);

                    return false;
                })
                .ToList();

            if (macros.Count == 0)
                return this;

            SearcherItem parent = SearcherItemUtility.GetItemFromPath(items, k_Macros);

            foreach (VSGraphAssetModel macro in macros)
            {
                parent.AddChild(new GraphNodeModelSearcherItem(
                    new GraphAssetSearcherItemData(macro),
                    data => ((VSGraphModel)data.graphModel).CreateMacroRefNode(
                        macro.GraphModel as VSGraphModel,
                        data.position,
                        data.creationMode
                    ),
                    $"{k_Macro} {macro.name}"
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddGraphsMethods()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(VSGraphAssetModel).Name}");
            List<Tuple<IGraphModel, FunctionModel>> methods = assetGUIDs.SelectMany(assetGuid =>
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                VSGraphAssetModel graphAssetModel = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(assetPath);

                if (!graphAssetModel || ((Object)graphAssetModel.GraphModel) == null)
                    return Enumerable.Empty<Tuple<IGraphModel, FunctionModel>>();

                var functionModels = graphAssetModel.GraphModel.NodeModels.OfExactType<FunctionModel>()
                        .Select(fm => new Tuple<IGraphModel, FunctionModel>(fm.GraphModel, fm));

                return functionModels.Concat(graphAssetModel.GraphModel.NodeModels.OfExactType<EventFunctionModel>()
                        .Select(fm => new Tuple<IGraphModel, FunctionModel>(fm.GraphModel, fm)));
            }).ToList();

            if (methods.Count == 0)
                return this;

            TypeHandle voidTypeHandle = typeof(void).GenerateTypeHandle(m_Stencil);

            foreach (Tuple<IGraphModel, FunctionModel> method in methods)
            {
                IGraphModel graphModel = method.Item1;
                FunctionModel functionModel = method.Item2;
                string graphName = graphModel.AssetModel.Name;
                var name = $"{k_Function} {functionModel.Title}";
                SearcherItem graphRoot = SearcherItemUtility.GetItemFromPath(items, $"{k_Graphs}/{graphName}");

                if (functionModel.ReturnType == voidTypeHandle)
                {
                    graphRoot.AddChild(new StackNodeModelSearcherItem(
                        new FunctionRefSearcherItemData(graphModel, functionModel),
                        data => ((StackModel)data.stackModel).CreateFunctionRefCallNode(
                            functionModel,
                            data.index,
                            data.creationMode
                        ),
                        name
                    ));
                    continue;
                }

                graphRoot.AddChild(new GraphNodeModelSearcherItem(
                    new FunctionRefSearcherItemData(graphModel, functionModel),
                    data => ((VSGraphModel)data.graphModel).CreateFunctionRefCallNode(
                        functionModel,
                        data.position,
                        data.creationMode
                    ),
                    name
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddGraphAssetMembers(IGraphModel graph)
        {
            SearcherItem parent = null;
            TypeHandle voidTypeHandle = m_Stencil.GenerateTypeHandle(typeof(void));

            foreach (var functionModel in graph.NodeModels.OfType<FunctionModel>())
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, graph.Name);
                }

                if (functionModel.ReturnType == voidTypeHandle)
                {
                    parent.AddChild(new StackNodeModelSearcherItem(
                        new GraphAssetSearcherItemData(graph.AssetModel),
                        data => ((StackModel)data.stackModel).CreateFunctionRefCallNode(
                            functionModel,
                            data.index,
                            data.creationMode
                        ),
                        functionModel.Title
                    ));
                    continue;
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new GraphAssetSearcherItemData(graph.AssetModel),
                    data => ((VSGraphModel)data.graphModel).CreateFunctionRefCallNode(
                        functionModel,
                        data.position,
                        data.creationMode
                    ),
                    functionModel.Title
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddNodesWithSearcherItemAttribute()
        {
            var types = TypeCache.GetTypesWithAttribute<SearcherItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<SearcherItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(m_Stencil))
                        continue;

                    var name = attribute.Path.Split('/').Last();
                    var path = attribute.Path.Remove(attribute.Path.LastIndexOf('/') + 1);

                    switch (attribute.Context)
                    {
                        case SearcherContext.Graph:
                        {
                            var node = new GraphNodeModelSearcherItem(
                                new NodeSearcherItemData(type),
                                data =>
                                {
                                    var vsGraphModel = (VSGraphModel)data.graphModel;

                                    return data.creationMode == NodeCreationMode.Orphan
                                        ? vsGraphModel.CreateOrphanNode(type, name, data.position)
                                        : vsGraphModel.CreateNode(type, name, data.position);
                                },
                                name
                            );
                            items.AddAtPath(node, path);
                            break;
                        }

                        case SearcherContext.Stack:
                        {
                            var node = new StackNodeModelSearcherItem(
                                new NodeSearcherItemData(type),
                                data =>
                                {
                                    var stackModel = (StackModel)data.stackModel;

                                    return data.creationMode == NodeCreationMode.Orphan
                                        ? stackModel.CreateOrphanStackedNode(type, name)
                                        : stackModel.CreateStackedNode(type, name, data.index);
                                },
                                name
                            );
                            items.AddAtPath(node, path);
                            break;
                        }

                        default:
                            Debug.LogWarning($"The node {type} is not a {SearcherContext.Stack} or " +
                                $"{SearcherContext.Graph} node, so it cannot be add in the Searcher");
                            break;
                    }

                    break;
                }
            }

            return this;
        }

        public GraphElementSearcherDatabase AddStickyNote()
        {
            var node = new GraphNodeModelSearcherItem(
                new SearcherItemData(SearcherItemTarget.StickyNote),
                data =>
                {
                    var rect = new Rect(data.position, StickyNote.defaultSize);
                    var vsGraphModel = (VSGraphModel)data.graphModel;

                    return data.creationMode == NodeCreationMode.Orphan
                        ? vsGraphModel.CreateOrphanStickyNote(k_Sticky, rect)
                        : vsGraphModel.CreateStickyNote(k_Sticky, rect);
                },
                k_Sticky
            );
            items.AddAtPath(node);

            return this;
        }

        public GraphElementSearcherDatabase AddEmptyFunction()
        {
            var node = new GraphNodeModelSearcherItem(
                new SearcherItemData(SearcherItemTarget.EmptyFunction),
                data => ((VSGraphModel)data.graphModel).CreateFunction(
                    k_FunctionName,
                    data.position,
                    data.creationMode
                ),
                k_NewFunction
            );
            items.AddAtPath(node);

            return this;
        }

        public GraphElementSearcherDatabase AddGroup()
        {
            var node = new GraphNodeModelSearcherItem(
                new SearcherItemData(SearcherItemTarget.Group),
                data => ((VSGraphModel)data.graphModel).CreateGroup(
                    k_Group,
                    data.position,
                    data.creationMode
                ),
                k_Group
            );
            items.AddAtPath(node);

            return this;
        }

        public GraphElementSearcherDatabase AddStack()
        {
            var node = new GraphNodeModelSearcherItem(
                new SearcherItemData(SearcherItemTarget.Stack),
                data => ((VSGraphModel)data.graphModel).CreateStack(
                    string.Empty,
                    data.position,
                    data.creationMode
                ),
                k_Stack
            );
            items.AddAtPath(node);

            return this;
        }

        public GraphElementSearcherDatabase AddInlineExpression()
        {
            var node = new GraphNodeModelSearcherItem(
                new SearcherItemData(SearcherItemTarget.InlineExpression),
                data => ((VSGraphModel)data.graphModel).CreateInlineExpressionNode(
                    k_InlineLabel,
                    data.position,
                    data.creationMode
                ),
                k_InlineExpression
            );
            items.AddAtPath(node, k_Constant);

            return this;
        }

        public GraphElementSearcherDatabase AddBinaryOperators()
        {
            SearcherItem parent = SearcherItemUtility.GetItemFromPath(items, k_Operator);

            foreach (BinaryOperatorKind kind in Enum.GetValues(typeof(BinaryOperatorKind)))
            {
                parent.AddChild(new GraphNodeModelSearcherItem(
                    new BinaryOperatorSearcherItemData(kind),
                    data => ((VSGraphModel)data.graphModel).CreateBinaryOperatorNode(
                        kind,
                        data.position,
                        data.creationMode
                    ),
                    kind.ToString()
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddUnaryOperators()
        {
            SearcherItem parent = SearcherItemUtility.GetItemFromPath(items, k_Operator);

            foreach (UnaryOperatorKind kind in Enum.GetValues(typeof(UnaryOperatorKind)))
            {
                if (kind == UnaryOperatorKind.PostDecrement || kind == UnaryOperatorKind.PostIncrement)
                {
                    parent.AddChild(new StackNodeModelSearcherItem(
                        new UnaryOperatorSearcherItemData(kind),
                        data => ((StackModel)data.stackModel).CreateUnaryStatementNode(
                            kind,
                            data.index,
                            data.creationMode
                        ),
                        kind.ToString()
                    ));
                    continue;
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new UnaryOperatorSearcherItemData(kind),
                    data => ((VSGraphModel)data.graphModel).CreateUnaryOperatorNode(
                        kind,
                        data.position,
                        data.creationMode
                    ),
                    kind.ToString()
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddControlFlows()
        {
            AddIfCondition(IfConditionMode.Basic);
            AddIfCondition(IfConditionMode.Advanced);
            AddIfCondition(IfConditionMode.Complete);

            SearcherItem parent = null;
            var loopTypes = TypeCache.GetTypesDerivedFrom<LoopStackModel>();

            foreach (var loopType in loopTypes.Where(t => !t.IsAbstract))
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, k_ControlFlow);
                }

                var name = $"{VseUtility.GetTitle(loopType)}{k_LoopStack}";
                parent.AddChild(new StackNodeModelSearcherItem(
                    new ControlFlowSearcherItemData(loopType),
                    data =>
                    {
                        var stackModel = (StackModel)data.stackModel;
                        var elements = new List<IGraphElementModel>();

                        var graphModel = (VSGraphModel)stackModel.GraphModel;
                        var stackPosition = new Vector2(
                            stackModel.Position.x + k_StackOffset.x,
                            stackModel.Position.y + k_StackOffset.y
                        );

                        LoopStackModel loopStack = graphModel.CreateLoopStack(
                            loopType,
                            stackPosition,
                            data.creationMode
                        );

                        var node = stackModel.CreateLoopNode(loopStack, data.index, data.creationMode);

                        elements.Add(node);
                        elements.Add(loopStack);

                        var edge = data.creationMode == NodeCreationMode.Orphan
                            ? graphModel.CreateOrphanEdge(loopStack.InputPortModels[0], node.OutputPortModels[0])
                            : graphModel.CreateEdge(loopStack.InputPortModels[0], node.OutputPortModels[0]);
                        elements.Add(edge);

                        return elements.ToArray();
                    },
                    name
                ));
            }

            return this;
        }

        void AddIfCondition(IfConditionMode mode)
        {
            var nodeName = $"{k_IfCondition} {mode}";
            var node = new StackNodeModelSearcherItem(
                new ControlFlowSearcherItemData(typeof(IfConditionNodeModel)),
                data =>
                {
                    var isOrphan = data.creationMode == NodeCreationMode.Orphan;
                    var elements = new List<IGraphElementModel>();
                    var stackModel = (StackModel)data.stackModel;

                    // Create If node
                    var ifConditionNode = isOrphan
                        ? stackModel.CreateOrphanStackedNode<IfConditionNodeModel>(nodeName)
                        : stackModel.CreateStackedNode<IfConditionNodeModel>(nodeName, stackModel.NodeModels.Count());
                    elements.Add(ifConditionNode);

                    if (mode <= IfConditionMode.Basic)
                        return elements.ToArray();

                    // Create Then and Else stacks
                    var graphModel = (VSGraphModel)stackModel.GraphModel;

                    var thenPosition = new Vector2(stackModel.Position.x + k_ThenStackOffset.x,
                        stackModel.Position.y + k_ThenStackOffset.y);
                    StackModel thenStack = graphModel.CreateStack(k_Then, thenPosition, data.creationMode);
                    elements.Add(thenStack);

                    var elsePosition = new Vector2(stackModel.Position.x + k_ElseStackOffset.x,
                        stackModel.Position.y + k_ElseStackOffset.y);
                    StackModel elseStack = graphModel.CreateStack(k_Else, elsePosition, data.creationMode);
                    elements.Add(elseStack);

                    // Connect Then and Else stacks to If node
                    elements.Add(isOrphan
                        ? graphModel.CreateOrphanEdge(thenStack.InputPortModels[0],
                            ifConditionNode.OutputPortModels[0])
                        : graphModel.CreateEdge(thenStack.InputPortModels[0],
                            ifConditionNode.OutputPortModels[0])
                    );

                    elements.Add(isOrphan
                        ? graphModel.CreateOrphanEdge(elseStack.InputPortModels[0],
                            ifConditionNode.OutputPortModels[1])
                        : graphModel.CreateEdge(elseStack.InputPortModels[0],
                            ifConditionNode.OutputPortModels[1])
                    );

                    if (mode != IfConditionMode.Complete)
                        return elements.ToArray();

                    // Create End of Condition stack
                    var completeStackPosition = new Vector2(stackModel.Position.x + k_ClosedFlowStackOffset.x,
                        stackModel.Position.y + k_ClosedFlowStackOffset.y);

                    StackModel completeFlowStack = graphModel.CreateStack("", completeStackPosition, data.creationMode);
                    elements.Add(completeFlowStack);

                    // Connect to Then and Else stacks
                    elements.Add(isOrphan
                        ? graphModel.CreateOrphanEdge(completeFlowStack.InputPortModels[0],
                            thenStack.OutputPortModels[0])
                        : graphModel.CreateEdge(completeFlowStack.InputPortModels[0],
                            thenStack.OutputPortModels[0])
                    );

                    elements.Add(isOrphan
                        ? graphModel.CreateOrphanEdge(completeFlowStack.InputPortModels[0],
                            elseStack.OutputPortModels[0])
                        : graphModel.CreateEdge(completeFlowStack.InputPortModels[0],
                            elseStack.OutputPortModels[0])
                    );

                    return elements.ToArray();
                },
                nodeName);

            items.AddAtPath(node, k_ControlFlow);
        }

        public GraphElementSearcherDatabase AddConstants(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                AddConstants(type);
            }

            return this;
        }

        public GraphElementSearcherDatabase AddConstants(Type type)
        {
            TypeHandle handle = type.GenerateTypeHandle(m_Stencil);

            SearcherItem parent = SearcherItemUtility.GetItemFromPath(items, k_Constant);
            parent.AddChild(new GraphNodeModelSearcherItem(
                new TypeSearcherItemData(handle, SearcherItemTarget.Constant),
                data => ((VSGraphModel)data.graphModel).CreateConstantNode("", handle, data.position, data.creationMode),
                $"{type.FriendlyName().Nicify()} {k_Constant}"
            ));

            return this;
        }

        public GraphElementSearcherDatabase AddMembers(
            IEnumerable<Type> types,
            MemberFlags memberFlags,
            BindingFlags bindingFlags,
            Dictionary<string, List<Type>> blackList = null
        )
        {
            foreach (Type type in types)
            {
                if (memberFlags.HasFlag(MemberFlags.Constructor))
                {
                    AddConstructors(type, bindingFlags);
                }

                if (memberFlags.HasFlag(MemberFlags.Field))
                {
                    AddFields(type, bindingFlags);
                }

                if (memberFlags.HasFlag(MemberFlags.Property))
                {
                    AddProperties(type, bindingFlags);
                }

                if (memberFlags.HasFlag(MemberFlags.Method))
                {
                    AddMethods(type, bindingFlags, blackList);
                }

                if (memberFlags.HasFlag(MemberFlags.Extension))
                {
                    AddExtensionMethods(type);
                }
            }

            return this;
        }

        public GraphElementSearcherDatabase AddExtensionMethods(Type type)
        {
            Dictionary<Type, List<MethodInfo>> extensions = TypeSystem.GetExtensionMethods(m_Stencil.GetAssemblies());

            if (!extensions.TryGetValue(type, out var methodInfos))
                return this;

            SearcherItem parent = null;

            foreach (MethodInfo methodInfo in methodInfos
                .Where(m => !m.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOut)))
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, type.FriendlyName(false));
                }

                MethodDetails details = methodInfo.GetMethodDetails();

                if (methodInfo.ReturnType != typeof(void))
                {
                    parent.AddChild(new GraphNodeModelSearcherItem(
                        new MethodSearcherItemData(methodInfo),
                        data => ((VSGraphModel)data.graphModel).CreateFunctionCallNode(
                            methodInfo,
                            data.position,
                            data.creationMode
                        ),
                        details.MethodName,
                        details.Details
                    ));
                    continue;
                }

                parent.AddChild(new StackNodeModelSearcherItem(
                    new MethodSearcherItemData(methodInfo),
                    data => ((StackModel)data.stackModel).CreateFunctionCallNode(
                        methodInfo,
                        data.index,
                        data.creationMode
                    ),
                    details.MethodName,
                    details.Details
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddConstructors(Type type, BindingFlags bindingFlags)
        {
            SearcherItem parent = null;

            foreach (ConstructorInfo constructorInfo in type.GetConstructors(bindingFlags))
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, type.FriendlyName(false));
                }

                MethodDetails details = constructorInfo.GetMethodDetails();
                parent.AddChild(new GraphNodeModelSearcherItem(
                    new ConstructorSearcherItemData(constructorInfo),
                    data => ((VSGraphModel)data.graphModel).CreateFunctionCallNode(
                        constructorInfo,
                        data.position,
                        data.creationMode
                    ),
                    details.MethodName,
                    details.Details
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddMethods(
            Type type,
            BindingFlags bindingFlags,
            Dictionary<string, List<Type>> blackList = null
        )
        {
            SearcherItem parent = null;

            foreach (MethodInfo methodInfo in type.GetMethods(bindingFlags)
                .Where(m => !m.IsSpecialName
                    && m.GetCustomAttribute<ObsoleteAttribute>() == null
                    && m.GetCustomAttribute<HiddenAttribute>() == null
                    && !m.Name.StartsWith("get_", StringComparison.Ordinal)
                    && !m.Name.StartsWith("set_", StringComparison.Ordinal)
                    && !m.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOut)
                    && !SearcherItemCollectionUtility.IsMethodBlackListed(m, blackList))
                .OrderBy(m => m.Name))
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, type.FriendlyName(false));
                }

                MethodDetails details = methodInfo.GetMethodDetails();

                if (methodInfo.ReturnType == typeof(void))
                {
                    parent.AddChild(new StackNodeModelSearcherItem(
                        new MethodSearcherItemData(methodInfo),
                        data => ((StackModel)data.stackModel).CreateFunctionCallNode(
                            methodInfo,
                            data.index,
                            data.creationMode
                        ),
                        details.MethodName,
                        details.Details
                    ));
                    continue;
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new MethodSearcherItemData(methodInfo),
                    data => ((VSGraphModel)data.graphModel).CreateFunctionCallNode(
                        methodInfo,
                        data.position,
                        data.creationMode
                    ),
                    details.MethodName,
                    details.Details
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddProperties(Type type, BindingFlags bindingFlags)
        {
            SearcherItem parent = null;

            foreach (PropertyInfo propertyInfo in type.GetProperties(bindingFlags)
                .OrderBy(p => p.Name)
                .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null
                    && p.GetCustomAttribute<HiddenAttribute>() == null))
            {
                var children = new List<SearcherItem>();

                if (propertyInfo.GetIndexParameters().Length > 0) // i.e : Vector2.this[int]
                {
                    children.Add(new GraphNodeModelSearcherItem(
                        new PropertySearcherItemData(propertyInfo),
                        data => ((VSGraphModel)data.graphModel).CreateFunctionCallNode(
                            propertyInfo.GetMethod,
                            data.position,
                            data.creationMode
                        ),
                        propertyInfo.Name
                    ));
                }
                else
                {
                    if (propertyInfo.CanRead)
                    {
                        if (propertyInfo.GetMethod.IsStatic)
                        {
                            if (propertyInfo.CanWrite)
                            {
                                children.Add(new GraphNodeModelSearcherItem(
                                    new PropertySearcherItemData(propertyInfo),
                                    data => ((VSGraphModel)data.graphModel).CreateFunctionCallNode(
                                        propertyInfo.GetMethod,
                                        data.position,
                                        data.creationMode
                                    ),
                                    propertyInfo.Name
                                ));
                            }
                            else
                            {
                                children.Add(new GraphNodeModelSearcherItem(
                                    new PropertySearcherItemData(propertyInfo),
                                    data => ((VSGraphModel)data.graphModel).CreateSystemConstantNode(
                                        type,
                                        propertyInfo,
                                        data.position,
                                        data.creationMode
                                    ),
                                    propertyInfo.Name
                                ));
                            }
                        }
                        else
                        {
                            children.Add(new GraphNodeModelSearcherItem(
                                new PropertySearcherItemData(propertyInfo),
                                data =>
                                {
                                    INodeModel nodeModel = ((VSGraphModel)data.graphModel).CreateGetPropertyGroupNode(
                                        data.position,
                                        data.creationMode
                                    );
                                    ((GetPropertyGroupNodeModel)nodeModel).AddMember(
                                        propertyInfo.GetUnderlyingType(),
                                        propertyInfo.Name
                                    );
                                    return nodeModel;
                                },
                                propertyInfo.Name
                            ));
                        }
                    }

                    if (propertyInfo.CanWrite)
                    {
                        children.Add(new StackNodeModelSearcherItem(
                            new PropertySearcherItemData(propertyInfo),
                            data => ((StackModel)data.stackModel).CreateFunctionCallNode(
                                propertyInfo.SetMethod,
                                data.index,
                                data.creationMode
                            ),
                            propertyInfo.Name
                        ));
                    }
                }

                if (children.Count == 0)
                {
                    continue;
                }

                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, type.FriendlyName(false));
                }

                foreach (SearcherItem child in children)
                {
                    parent.AddChild(child);
                }
            }

            return this;
        }

        public GraphElementSearcherDatabase AddFields(Type type, BindingFlags bindingFlags)
        {
            SearcherItem parent = null;

            foreach (FieldInfo fieldInfo in type.GetFields(bindingFlags)
                .OrderBy(f => f.Name)
                .Where(f => f.GetCustomAttribute<ObsoleteAttribute>() == null
                    && f.GetCustomAttribute<HiddenAttribute>() == null))
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, type.FriendlyName(false));
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new FieldSearcherItemData(fieldInfo),
                    data =>
                    {
                        INodeModel nodeModel = ((VSGraphModel)data.graphModel).CreateGetPropertyGroupNode(
                            data.position,
                            data.creationMode
                        );
                        ((GetPropertyGroupNodeModel)nodeModel).AddMember(fieldInfo.GetUnderlyingType(), fieldInfo.Name);
                        return nodeModel;
                    },
                    fieldInfo.Name
                ));

                if (fieldInfo.CanWrite())
                {
                    parent.AddChild(new StackNodeModelSearcherItem(
                        new FieldSearcherItemData(fieldInfo),
                        data =>
                        {
                            INodeModel nodeModel = ((StackModel)data.stackModel).CreateSetPropertyGroupNode(
                                data.index
                            );
                            ((SetPropertyGroupNodeModel)nodeModel).AddMember(
                                fieldInfo.GetUnderlyingType(),
                                fieldInfo.Name
                            );
                            return nodeModel;
                        },
                        fieldInfo.Name
                    ));
                }
            }

            return this;
        }

        public GraphElementSearcherDatabase AddFunctionMembers(IFunctionModel functionModel)
        {
            if (functionModel == null)
                return this;

            SearcherItem parent = null;
            IEnumerable<IVariableDeclarationModel> members = functionModel.FunctionParameterModels.Union(
                functionModel.FunctionVariableModels);

            foreach (IVariableDeclarationModel declarationModel in members)
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, k_FunctionMembers);
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new TypeSearcherItemData(declarationModel.DataType, SearcherItemTarget.Variable),
                    data => ((VSGraphModel)data.graphModel).CreateVariableNode(
                        declarationModel,
                        data.position,
                        data.creationMode
                    ),
                    declarationModel.Name.Nicify()
                ));
            }

            return this;
        }

        public GraphElementSearcherDatabase AddGraphVariables(IGraphModel graphModel)
        {
            SearcherItem parent = null;
            var vsGraphModel = (VSGraphModel)graphModel;

            foreach (IVariableDeclarationModel declarationModel in vsGraphModel.GraphVariableModels)
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(items, k_GraphVariables);
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new TypeSearcherItemData(declarationModel.DataType, SearcherItemTarget.Variable),
                    data => ((VSGraphModel)data.graphModel).CreateVariableNode(
                        declarationModel,
                        data.position,
                        data.creationMode
                    ),
                    declarationModel.Name.Nicify()
                ));
            }

            return this;
        }

        public SearcherDatabase Build()
        {
            return SearcherDatabase.Create(items, "", false);
        }
    }
}
