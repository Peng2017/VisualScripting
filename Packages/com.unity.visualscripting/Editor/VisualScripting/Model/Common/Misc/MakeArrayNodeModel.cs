using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, "Array/Make")]
    public class MakeArrayNodeModel : NodeModel
    {
        [SerializeField]
        int m_PortCount = 1;

        public int PortCount => m_PortCount;

        public override string Title => "Make Array";

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddDataOutputPort<Unknown>("");

            for (int i = 0; i < PortCount; i++)
                AddDataInput<Unknown>("Element " + i);
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input || otherConnectedPortModel == null)
                return;

            if (((PortModel)selfConnectedPortModel).DataType == otherConnectedPortModel.DataType)
                return;

            ((PortModel)selfConnectedPortModel).DataType = otherConnectedPortModel.DataType;
            //TODO this is horrible.... Opinions wanted on this.
            m_OutputPortModels[0].DataType = typeof(VSArray<>).MakeGenericType(m_InputPortModels[0].DataType.Resolve(Stencil)).GenerateTypeHandle(Stencil);
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            OnConnection(selfConnectedPortModel, otherConnectedPortModel);
        }

        // Pass a negative number to decrease.
        public void IncreasePortCount(int portCountChange)
        {
            Undo.RegisterCompleteObjectUndo(this, "Change Port Count");
            m_PortCount = Math.Max(1, PortCount + portCountChange);
            DefineNode();
        }
    }

    class ChangeMakeArrayNodePortCountAction : IAction
    {
        public readonly MakeArrayNodeModel[] nodeModels;
        public readonly int portCountChange;

        public ChangeMakeArrayNodePortCountAction(int portCountChange, params MakeArrayNodeModel[] nodeModels)
        {
            this.nodeModels = nodeModels;
            this.portCountChange = portCountChange;
        }
    }
}
