using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public abstract class ConstantNodeModel : NodeModel, IVariableModel, IConstantNodeModel
    {
        public abstract IVariableDeclarationModel DeclarationModel { get; }
        public abstract object ObjectValue { get; set; }
        public abstract Type Type { get; }
        public abstract bool IsLocked { get; set; }
    }

    public abstract class ConstantNodeModel<TSerialized, TGenerated> : ConstantNodeModel
    {
        [SerializeField]
        bool m_IsLocked;

        [CanBeNull]
        public override IVariableDeclarationModel DeclarationModel => null;

        //TODO decide if this is gonna be a problem in the long term or not
        public TSerialized value;

        //TODO decide if this is gonna be a problem in the long term or not
        public override Type Type => typeof(TGenerated);
        public override string VariableString => "Constant";
        public override string DataTypeString => Type.FriendlyName();
        public override string Title => string.Empty;

        public override object ObjectValue
        {
            get => value;
            set => this.value = (TSerialized)value;
        }

        public override bool IsLocked
        {
            get => m_IsLocked;
            set
            {
                Undo.RegisterCompleteObjectUndo(this, "Set IsLocked");
                m_IsLocked = value;
            }
        }

        protected override void OnDefineNode()
        {
            AddDataOutputPort(null, typeof(TSerialized).GenerateTypeHandle(Stencil));
        }
    }
    public abstract class ConstantNodeModel<T> : ConstantNodeModel<T, T>
    {

    }
}
