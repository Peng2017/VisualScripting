using System;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public class EnumConstantNodeModel : ConstantNodeModel<EnumValueReference>
    {
        public override string Title => value.Value.ToString();

        public override Type Type => EnumType.Resolve(Stencil);

        protected override void OnDefineNode()
        {
            if (!value.IsValid(Stencil))
                value = new EnumValueReference(typeof(KeyCode).GenerateTypeHandle(Stencil));
            base.OnDefineNode();
        }

        public Enum EnumValue => value.ValueAsEnum(Stencil);

        public TypeHandle EnumType => value.EnumType;
    }
}
