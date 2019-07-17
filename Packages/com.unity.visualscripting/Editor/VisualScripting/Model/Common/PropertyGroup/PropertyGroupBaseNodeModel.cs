using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public abstract class PropertyGroupBaseNodeModel : NodeModel
    {
        [SerializeField]
        List<TypeMember> m_Members;

        public List<TypeMember> Members => m_Members ?? (m_Members = new List<TypeMember>());

        protected override void OnDefineNode()
        {
            AddInstanceInput(TypeHandle.ThisType);
        }

        public TypeHandle GetConnectedInstanceType()
        {
            var inputPort = InputPortModels.FirstOrDefault();
            if (inputPort == null || !inputPort.Connected)
                return TypeHandle.ThisType;

            return inputPort.DataType;
        }

        public void AddMember(Type type, string memberName)
        {
            Assert.IsNotNull(type);
            Assert.IsNotNull(memberName);
            var member = new TypeMember(type.GenerateTypeHandle(Stencil), new List<string> {memberName});

            Undo.RegisterCompleteObjectUndo(this, "Add Member");
            Members.Add(member);
            DefineNode();
            EditorUtility.SetDirty(this);
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input || otherConnectedPortModel == null)
                return;

            if (((PortModel)selfConnectedPortModel).DataType != otherConnectedPortModel.DataType)
            {
                ((PortModel)selfConnectedPortModel).DataType = otherConnectedPortModel.DataType;
                // TODO member types might have changed (ie. new instance type has a member with the same name
                // as the previous one, but a different type: struct A { int x; } / struct B { float x; }
            }
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input || selfConnectedPortModel.PortType != PortType.Instance)
                return;

            ((PortModel)selfConnectedPortModel).DataType = TypeHandle.ThisType;
        }

        public void AddMember(TypeMember member)
        {
            if (Members.Contains(member))
                return;

            Undo.RegisterCompleteObjectUndo(this, "Add Members");
            Members.Add(member);
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
            DefineNode();
            EditorUtility.SetDirty(this);
        }

        public void RemoveMember(TypeMember member)
        {
            Undo.RegisterCompleteObjectUndo(this, "Remove Members");

            var propertyGroupBase = this;
            string longName = member.ToString();
            var idx = Members.FindIndex(x => x.ToString() == longName);

            // remove member from list
            Members.RemoveAt(idx);

            // disconnect edges
            IReadOnlyList<IPortModel> portModels = propertyGroupBase.OutputPortModels;
            if (propertyGroupBase is SetPropertyGroupNodeModel)
            {
                portModels = propertyGroupBase.InputPortModels;
                idx++; // inputPortModel[0] is used as InputInstancePort
            }

            var portModel = portModels[idx];
            var oppositePortModel = portModel.ConnectionPortModels.FirstOrDefault();

            if (portModel.Direction == Direction.Input)
                ((VSGraphModel)GraphModel).DeleteEdge(portModel, oppositePortModel);
            else
                ((VSGraphModel)GraphModel).DeleteEdge(oppositePortModel, portModel);

            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);

            // now we need to patch all edges connected to ports after the deleted one, as edge serialize tuples (Node, portIndex)
            portModels.Skip(portModel.Index).SelectMany(p => GraphModel.GetEdgesConnections(p)).ToList().ForEach(
                e =>
                {
                    Undo.RegisterCompleteObjectUndo((Object)e, "Patch edge index");
                    if (portModel.Direction == Direction.Output)
                        ((EdgeModel)e).OutputIndex--;
                    else
                        ((EdgeModel)e).InputIndex--;
                });

            DefineNode();
            EditorUtility.SetDirty(this);
        }
    }

    class EditPropertyGroupNodeAction : IAction
    {
        public enum EditType
        {
            Add,
            Remove
        }

        public readonly EditType editType;
        public readonly INodeModel nodeModel;
        public readonly TypeMember member;

        public EditPropertyGroupNodeAction(EditType editType, INodeModel nodeModel, TypeMember member)
        {
            this.editType = editType;
            this.nodeModel = nodeModel;
            this.member = member;
        }
    }
}
