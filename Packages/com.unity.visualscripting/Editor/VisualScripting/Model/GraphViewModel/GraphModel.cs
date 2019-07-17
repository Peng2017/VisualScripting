using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public abstract class GraphModel : ScriptableObject, IGraphModel
    {
        [SerializeField]
        GraphAssetModel m_AssetModel;
        [SerializeField]
        protected List<NodeModel> m_NodeModels;
        [SerializeField]
        protected List<EdgeModel> m_EdgeModels;

        [SerializeField]
        protected List<StickyNoteModel> m_StickyNoteModels;

        [SerializeField]
        Stencil m_Stencil;

        const float k_IOHorizontalOffset = 150;
        const float k_IOVerticalOffset = 40;

        public GraphChangeList LastChanges { get; private set; }
        IGraphChangeList IGraphModel.LastChanges => LastChanges;

        protected GraphModel()
        {
            LastChanges = new GraphChangeList();
        }

        public string Name => name;

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public IReadOnlyList<INodeModel> NodeModels => m_NodeModels;
        public IReadOnlyList<IEdgeModel> EdgeModels => m_EdgeModels;
        public IEnumerable<IStickyNoteModel> StickyNoteModels => m_StickyNoteModels;

        public Stencil Stencil
        {
            get => m_Stencil;
            set => m_Stencil = value;
        }

        public enum DeleteConnections
        {
            False,
            True
        }

        public string GetAssetPath()
        {
            return AssetDatabase.GetAssetPath(m_AssetModel);
        }

        public TNodeType CreateNode<TNodeType>(string nodeName, Vector2 position, Action<TNodeType> setup = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateNode(typeof(TNodeType), nodeName, position, n => setup?.Invoke((TNodeType)n));
            return node;
        }

        public INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, Action<NodeModel> preDefineSetup = null)
        {
            var nodeModel = CreateOrphanNode(nodeTypeToCreate, nodeName, position, preDefineSetup);
            AddNode(nodeModel);

            return nodeModel;
        }

        public TNodeType CreateNodeNoUndo<TNodeType>(string nodeName, Vector2 position, Action<TNodeType> setup = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateNodeNoUndo(typeof(TNodeType), nodeName, position, n => setup?.Invoke((TNodeType)n));
            return node;
        }

        public INodeModel CreateNodeNoUndo(Type nodeTypeToCreate, string nodeName, Vector2 position, Action<NodeModel> preDefineSetup = null)
        {
            var nodeModel = CreateOrphanNode(nodeTypeToCreate, nodeName, position, preDefineSetup);
            AddNodeNoUndo(nodeModel);

            return nodeModel;
        }

        public TNodeType CreateOrphanNode<TNodeType>(string nodeName, Vector2 position, Action<TNodeType> setup = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateOrphanNode(typeof(TNodeType), nodeName, position, n => setup?.Invoke((TNodeType)n));
            return node;
        }

        public INodeModel CreateOrphanNode(Type nodeTypeToCreate, string nodeName, Vector2 position, Action<NodeModel> preDefineSetup = null)
        {
            if (nodeTypeToCreate == null)
                throw new InvalidOperationException("Cannot create node with a null type");
            var nodeModel = (NodeModel)CreateInstance(nodeTypeToCreate);
            nodeModel.name = nodeName ?? nodeTypeToCreate.Name;
            nodeModel.Position = position;
            nodeModel.GraphModel = this;
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.DefineNode();

            return nodeModel;
        }

        public void AddNode(INodeModel nodeModel)
        {
            Utility.SaveAssetIntoObject((NodeModel)nodeModel, (Object)AssetModel);
            Undo.RegisterCompleteObjectUndo(this, "Add Node");
            ((NodeModel)nodeModel).GraphModel = this;
            m_NodeModels.Add((NodeModel)nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        public void AddNodeNoUndo(INodeModel nodeModel)
        {
            Utility.SaveAssetIntoObject((NodeModel)nodeModel, (Object)AssetModel);
            ((NodeModel)nodeModel).GraphModel = this;
            m_NodeModels.Add((NodeModel)nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        public void DeleteNodes(IReadOnlyCollection<INodeModel> nodesToDelete, DeleteConnections deleteConnections)
        {
            // Let's delay stack node models at the very end, otherwise we might end up with
            // orphan stacked nodes during the process
            ILookup<bool, INodeModel> stackNodeLookup = nodesToDelete.ToLookup(x => x is IStackModel);

            foreach (var node in stackNodeLookup[false])
                DeleteNode(node, deleteConnections, false);

            foreach (var node in stackNodeLookup[true])
                DeleteNode(node, deleteConnections);
        }

        public void DeleteNode(INodeModel nodeModel, DeleteConnections deleteConnections, bool deleteWhenEmpty = true)
        {
            Undo.RegisterCompleteObjectUndo(this, "Delete Node");

            var model = (NodeModel)nodeModel;

            if (model.ParentStackModel as Object != null)
            {
                LastChanges?.ChangedElements.Add(nodeModel.ParentStackModel);
                var parentStack = (StackModel)model.ParentStackModel;
                parentStack.RemoveStackedNode(model);
                if (deleteWhenEmpty && parentStack.Capabilities.HasFlag(CapabilityFlags.DeletableWhenEmpty) &&
                    !parentStack.NodeModels.Any())
                    DeleteNode(parentStack, DeleteConnections.True);
            }
            else
            {
                if (LastChanges != null)
                    LastChanges.DeletedElements += 1;
                m_NodeModels.Remove(model);
            }

            if (deleteConnections == DeleteConnections.True)
            {
                List<IEdgeModel> edgeToRemove = new List<IEdgeModel>();
                foreach (var port in nodeModel.InputPortModels.Concat(nodeModel.OutputPortModels))
                {
                    foreach (var edge in m_EdgeModels.Where(x => (x.InputPortModel == port || x.OutputPortModel == port)))
                    {
                        edgeToRemove.Add(edge);
                    }
                }

                DeleteEdges(edgeToRemove);
            }

            model.Destroy();
        }

        internal void MoveNode(INodeModel nodeToMove, Vector2 newPosition)
        {
            ((NodeModel)nodeToMove).Move(newPosition);
        }

        public IEdgeModel CreateEdge(IPortModel inputPort, IPortModel outputPort)
        {
            var existing = EdgesConnectedToPorts(inputPort, outputPort);
            if (existing != null)
                return existing;

            var edgeModel = CreateOrphanEdge(inputPort, outputPort);
            AddEdge(edgeModel, inputPort, outputPort);

            return edgeModel;
        }

        public IEdgeModel CreateOrphanEdge(IPortModel input, IPortModel output)
        {
            Assert.IsNotNull(input);
            Assert.IsNotNull(input.NodeModel);
            Assert.IsNotNull(output);
            Assert.IsNotNull(output.NodeModel);

            var edgeModel = new EdgeModel(this, input, output);

            input.NodeModel.OnConnection(input, output);
            output.NodeModel.OnConnection(output, input);

            return edgeModel;
        }

        void AddEdge(IEdgeModel edgeModel, IPortModel inputPort, IPortModel outputPort)
        {
            Undo.RegisterCompleteObjectUndo(this, "Add Edge");
            ((EdgeModel)edgeModel).GraphModel = this;
            m_EdgeModels.Add((EdgeModel)edgeModel);
            LastChanges?.ChangedElements.Add(edgeModel);
            LastChanges?.ChangedElements.Add(inputPort.NodeModel);
            LastChanges?.ChangedElements.Add(outputPort.NodeModel);
        }

        public void DeleteEdge(IPortModel input, IPortModel output)
        {
            DeleteEdges(m_EdgeModels.Where(x => x.InputPortModel == input && x.OutputPortModel == output));
        }

        public void DeleteEdge(IEdgeModel edgeModel)
        {
            Undo.RegisterCompleteObjectUndo(this, "Delete Edge");
            var model = (EdgeModel)edgeModel;

            edgeModel.InputPortModel?.NodeModel.OnDisconnection(edgeModel.InputPortModel, edgeModel.OutputPortModel);
            edgeModel.OutputPortModel?.NodeModel.OnDisconnection(edgeModel.OutputPortModel, edgeModel.InputPortModel);

            LastChanges?.ChangedElements.Add(edgeModel.InputPortModel?.NodeModel);
            LastChanges?.ChangedElements.Add(edgeModel.OutputPortModel?.NodeModel);

            m_EdgeModels.Remove(model);
            if (LastChanges != null)
            {
                LastChanges.DeleteEdgeModels.Add(model);
                LastChanges.DeletedElements += 1;
            }
        }

        public void DeleteEdges(IEnumerable<IEdgeModel> edgeModels)
        {
            var edgesCopy = edgeModels.ToList();
            foreach (var edgeModel in edgesCopy)
                DeleteEdge(edgeModel);
        }

        public IStickyNoteModel CreateStickyNote(string nodeName, Rect position)
        {
            var stickyNodeModel = (StickyNoteModel)CreateOrphanStickyNote(nodeName, position);
            AddStickyNote(stickyNodeModel);

            return stickyNodeModel;
        }

        public IStickyNoteModel CreateOrphanStickyNote(string nodeName, Rect position)
        {
            var stickyNodeModel = CreateInstance<StickyNoteModel>();
            stickyNodeModel.name = nodeName;
            stickyNodeModel.Position = position;
            stickyNodeModel.GraphModel = this;

            return stickyNodeModel;
        }

        void AddStickyNote(IStickyNoteModel model)
        {
            var stickyNodeModel = (StickyNoteModel)model;

            Utility.SaveAssetIntoObject(stickyNodeModel, (Object)AssetModel);
            Undo.RegisterCompleteObjectUndo(this, "Add Sticky Note");
            LastChanges?.ChangedElements.Add(stickyNodeModel);
            stickyNodeModel.GraphModel = this;
            m_StickyNoteModels.Add(stickyNodeModel);
        }

        void DeleteStickyNote(IStickyNoteModel stickyNoteModel)
        {
            Undo.RegisterCompleteObjectUndo(this, "Delete Sticky Note");
            var model = (StickyNoteModel)stickyNoteModel;

            m_StickyNoteModels.Remove(model);
            if (LastChanges != null)
                LastChanges.DeletedElements += 1;
            model.Destroy();
        }

        protected virtual void OnEnable()
        {
            if (m_NodeModels == null)
                m_NodeModels = new List<NodeModel>();
            if (m_EdgeModels == null)
                m_EdgeModels = new List<EdgeModel>();
            if (m_StickyNoteModels == null)
                m_StickyNoteModels = new List<StickyNoteModel>();
        }

        public virtual void Dispose() { }

        public IEnumerable<IEdgeModel> GetEdgesConnections(IPortModel portModel)
        {
            return EdgeModels.Where(e => portModel.Direction == Direction.Input ? e.InputPortModel == portModel : e.OutputPortModel == portModel);
        }

        public IEnumerable<IEdgeModel> GetEdgesConnections(IEnumerable<IPortModel> portModels)
        {
            var models = new List<IEdgeModel>();
            foreach (var portModel in portModels)
            {
                models.AddRange(GetEdgesConnections(portModel));
            }

            return models;
        }

        public IEnumerable<IPortModel> GetConnections(IPortModel portModel)
        {
            return GetEdgesConnections(portModel).Select(e => portModel.Direction == Direction.Input ? e.OutputPortModel : e.InputPortModel)
                .Where(p => p != null);
        }

        public enum Verbosity
        {
            Errors,
            Verbose
        }

        public string FriendlyScriptName => TypeSystem.CodifyString(AssetModel.Name);

        public void DeleteStickyNotes(IStickyNoteModel[] stickyNotesToDelete)
        {
            foreach (IStickyNoteModel stickyNoteModel in stickyNotesToDelete)
                DeleteStickyNote(stickyNoteModel);
        }

        public void BypassNodes(INodeModel[] actionNodeModels)
        {
            foreach (var model in actionNodeModels)
            {
                var inputEdgeModels = GetEdgesConnections(model.InputPortModels).ToList();
                var outputEdgeModels = GetEdgesConnections(model.OutputPortModels).ToList();

                if (!inputEdgeModels.Any() || !outputEdgeModels.Any())
                    continue;

                DeleteEdges(inputEdgeModels);
                DeleteEdges(outputEdgeModels);

                CreateEdge(outputEdgeModels[0].InputPortModel, inputEdgeModels[0].OutputPortModel);
            }
        }

        public IEdgeModel EdgesConnectedToPorts(IPortModel input, IPortModel output)
        {
            return EdgeModels.FirstOrDefault(e => e.InputPortModel == input && e.OutputPortModel == output);
        }

        public IEnumerable<IEdgeModel> EdgesConnectedToPorts(IPortModel portModels)
        {
            return EdgeModels.Where(e => e.InputPortModel == portModels || e.OutputPortModel == portModels);
        }

        public void ResetChanges()
        {
            LastChanges = new GraphChangeList();
        }

        public void CleanUp()
        {
            m_NodeModels.RemoveAll(n => n == null);
            m_StickyNoteModels.RemoveAll(s => s == null);
            DeleteEdges(m_EdgeModels.Where(e => !e.IsValid()));
            m_EdgeModels.RemoveAll(e => e == null);
        }

        struct DeclarationIndex
        {
            public VariableDeclarationModel declarationModel;
            public int index;
        }

        internal MacroRefNodeModel ExtractNodesAsMacro(VSGraphModel macroGraphModel, Vector2 position, IEnumerable<IGraphElementModel> elementModels)
        {
            Undo.RegisterCompleteObjectUndo(this, "Extract Nodes to Macro");

            var elementModelList = elementModels.ToList();

            // duplicate selected nodes
            VseGraphView.Duplicate(macroGraphModel, elementModelList, out Dictionary<INodeModel, NodeModel> originalToMacro);

            // connect new node, etc.
            List<INodeModel> models = elementModelList.OfType<INodeModel>().ToList();
            int inputIndex = 0;
            int outputIndex = 0;

            // if the same node is connected to multiple extracted ports, only create one input in the macro
            // ie. if the same variable is connected to both ports of an Add node, the resulting macro
            // will have one input and return its double
            Dictionary<IPortModel, DeclarationIndex> existingNodesToCreatedOutputVariables = new Dictionary<IPortModel, DeclarationIndex>();
            Dictionary<IPortModel, DeclarationIndex> existingNodesToCreatedInputVariables = new Dictionary<IPortModel, DeclarationIndex>();

            Dictionary<INodeModel, List<IEdgeModel>> inputEdgesPerNode = models.ToDictionary(m => m, m => GetEdgesConnections(m.InputPortModels).ToList());
            Dictionary<INodeModel, List<IEdgeModel>> outputEdgesPerNode = models.ToDictionary(m => m, m => GetEdgesConnections(m.OutputPortModels).ToList());
            foreach (INodeModel model in models)
            {
                List<IEdgeModel> inputEdgeModels = inputEdgesPerNode[model];
                List<IEdgeModel> outputEdgeModels = outputEdgesPerNode[model];

                foreach (IEdgeModel edge in inputEdgeModels)
                {
                    INodeModel connectedNode = edge.OutputPortModel.NodeModel;
                    if (models.Contains(connectedNode)) // connected to another extracted node
                        continue;

                    // create/reuse declaration in macro graph
                    if (!existingNodesToCreatedInputVariables.TryGetValue(edge.OutputPortModel, out DeclarationIndex macroInputDecl))
                    {
                        macroInputDecl = new DeclarationIndex
                        {
                            declarationModel = macroGraphModel.CreateGraphVariableDeclaration($"Input {inputIndex}", edge.OutputPortModel.DataType, true),
                            index = inputIndex++,
                        };
                        macroInputDecl.declarationModel.Modifiers = ModifierFlags.ReadOnly;

                        existingNodesToCreatedInputVariables.Add(edge.OutputPortModel, macroInputDecl);
                    }

                    // create variable in macro graph, connect with extracted node port
                    NodeModel macroNodeWithInput = originalToMacro[model];
                    IVariableModel macroInputVar = macroGraphModel.CreateVariableNode(macroInputDecl.declarationModel, macroNodeWithInput.Position + new Vector2(- k_IOHorizontalOffset, k_IOVerticalOffset));
                    macroGraphModel.CreateEdge(macroNodeWithInput.InputPortModels[edge.InputPortModel.Index], macroInputVar.OutputPortModels[0]);
                }

                foreach (IEdgeModel edge in outputEdgeModels)
                {
                    INodeModel connectedNode = edge.InputPortModel.NodeModel;
                    if (models.Contains(connectedNode)) // connected to another extracted node
                        continue;

                    // create/reuse declaration in macro graph
                    if (!existingNodesToCreatedOutputVariables.TryGetValue(edge.InputPortModel, out DeclarationIndex macroOutputDecl))
                    {
                        macroOutputDecl = new DeclarationIndex
                        {
                            declarationModel = macroGraphModel.CreateGraphVariableDeclaration($"Output {outputIndex}", edge.InputPortModel.DataType, true),
                            index = outputIndex++,
                        };
                        macroOutputDecl.declarationModel.Modifiers = ModifierFlags.WriteOnly;

                        existingNodesToCreatedOutputVariables.Add(edge.InputPortModel, macroOutputDecl);
                    }

                    // create variable in macro graph, connect with extracted node port
                    NodeModel macroNodeWithOutput = originalToMacro[model];
                    IVariableModel macroOutputVar = macroGraphModel.CreateVariableNode(macroOutputDecl.declarationModel, macroNodeWithOutput.Position + new Vector2(k_IOHorizontalOffset*2, k_IOVerticalOffset));
                    macroGraphModel.CreateEdge(macroOutputVar.InputPortModels[0], macroNodeWithOutput.OutputPortModels[edge.OutputPortModel.Index]);
                }
            }

            // create new macroRefNode
            MacroRefNodeModel macroRefNodeModel =
                CreateNode<MacroRefNodeModel>("MyMacro", position, n =>
                {
                    MacroRefNodeModel macroNode = n;
                    macroNode.Macro = macroGraphModel;
                });

            foreach (INodeModel model in models)
            {
                List<IEdgeModel> inputEdgeModels = inputEdgesPerNode[model];
                List<IEdgeModel> outputEdgeModels = outputEdgesPerNode[model];

                foreach (IEdgeModel edge in inputEdgeModels)
                {
                    if (models.Contains(edge.OutputPortModel.NodeModel))
                        continue;

                    DeclarationIndex decl = existingNodesToCreatedInputVariables[edge.OutputPortModel];
                    if (decl.index == -1) // already done
                        continue;

                    CreateEdge(macroRefNodeModel.InputPortModels[decl.index], edge.OutputPortModel);
                    decl.index = -1;
                    existingNodesToCreatedInputVariables[edge.OutputPortModel] = decl;
                }

                foreach (IEdgeModel edge in outputEdgeModels)
                {
                    if (models.Contains(edge.InputPortModel.NodeModel))
                        continue;

                    DeclarationIndex decl = existingNodesToCreatedOutputVariables[edge.InputPortModel];
                    if (decl.index == -1) // already done
                        continue;
                    CreateEdge(edge.InputPortModel, macroRefNodeModel.OutputPortModels[decl.index]);
                    decl.index = -1;
                    existingNodesToCreatedOutputVariables[edge.InputPortModel] = decl;
                }
            }

            // delete selected nodes
            DeleteNodes(models, DeleteConnections.True);

            return macroRefNodeModel;
        }

        public INodeModel ExtractNodesAsFunction(IList<ISelectable> selection)
        {
            Undo.RegisterCompleteObjectUndo(this, "Extract Nodes to Function");

            // create new functionNode
            FunctionModel functionModel = (this as VSGraphModel).CreateFunction("MyFunction", new Vector2(1300, 0));

            // paste actionNodeModels
            var json = VseGraphView.OnSerializeGraphElements(selection.OfType<GraphElement>());
            TargetInsertionInfo info;
            info.Delta = functionModel.Position;
            info.OperationName = "Paste";
            info.TargetStack = functionModel;
            info.TargetStackInsertionIndex = 0;
            VseGraphView.OnUnserializeAndPaste(this as VSGraphModel, info, null, json);

            // connect new node, etc.
            Dictionary<IGraphElementModel, IHasGraphElementModel> selectedModels = selection
                .OfType<IHasGraphElementModel>()
                .ToDictionary(x => x.GraphElementModel);
            var models = selectedModels.Keys.OfType<INodeModel>().ToList();
            var elementInStack = models.FirstOrDefault(x => x.ParentStackModel != null);
            var newElementInStack = functionModel.NodeModels.FirstOrDefault();

            var inputEdgeModels = GetEdgesConnections(elementInStack?.InputPortModels).ToList();

            foreach (var edge in inputEdgeModels)
            {
                if (models.Contains(edge.OutputPortModel.NodeModel))
                    continue;

                if (edge.InputPortModel.PortType == PortType.Instance)
                    continue;

                // create variables
                var parameterModel = functionModel.CreateFunctionParameterDeclaration(edge.InputPortModel.Name,
                    edge.OutputPortModel.DataType);

                // create variable pill
                var pill = (this as VSGraphModel).CreateVariableNode(parameterModel,
                    functionModel.Position);

                // connect it to the port on the element
                CreateEdge(newElementInStack?.InputPortModels[edge.InputPortModel.Index], pill.OutputPortModels[0]);
            }

            // create new functionRefCallNode
            int index = 0;
            foreach (var node in elementInStack?.ParentStackModel.NodeModels ?? Enumerable.Empty<INodeModel>())
            {
                if (node == elementInStack)
                    break;
                index++;
            }

            var refCallNode = (elementInStack?.ParentStackModel as StackModel).CreateFunctionRefCallNode(functionModel, index);

            // connect nodes to ref call node
            foreach (var edge in inputEdgeModels)
            {
                if (models.Contains(edge.OutputPortModel.NodeModel))
                    continue;

                // connect it to the port on the element
                CreateEdge(refCallNode.InputPortModels[edge.InputPortModel.Index], edge.OutputPortModel);
            }

            // delete selected nodes
            DeleteNodes(models, DeleteConnections.True);

            return functionModel;
        }

        public INodeModel ConvertNodeToFunction(INodeModel nodeToConvert)
        {
            // create new functionNode
            FunctionModel functionModel = (this as VSGraphModel).CreateFunction("MyFunction", nodeToConvert.Position);

            // go through all stack connected to StackNode and connect to new functionModel
            // remove from previous stack
            var parentStack = (StackModel)nodeToConvert.ParentStackModel;
            parentStack.RemoveStackedNode(nodeToConvert);

            // delete old stack
            DeleteNode(parentStack, DeleteConnections.True);

            // add to new functionModel
            functionModel.AddStackedNode(nodeToConvert, 0);

            return functionModel;
        }
    }
}
