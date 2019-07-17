using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Model
{
    public class BinaryOperatorNodeModel : NodeModel, IOperationValidator
    {
        public BinaryOperatorKind kind;
        static Type[] s_SortedNumericTypes =
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(float),
            typeof(long),
            typeof(ulong),
            typeof(double),
            typeof(decimal)
        };

        public override string Title => kind.ToString();

        protected override void OnDefineNode()
        {
            AddDataInput<Unknown>("A");
            AddDataInput<Unknown>("B");
            AddDataOutputPort<Unknown>("Out");
        }

        static bool IsBooleanOperatorKind(BinaryOperatorKind kind)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equals:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    return true;
            }
            return false;
        }

        static int GetNumericTypePriority(Type type)
        {
            return Array.IndexOf(s_SortedNumericTypes, type);
        }

        static Type GetBiggestNumericType(Type x, Type y)
        {
            return GetNumericTypePriority(x) > GetNumericTypePriority(y) ? x : y;
        }

        public static Type GetOutputTypeFromInputs(BinaryOperatorKind kind, Type x, Type y)
        {
            List<MethodInfo> operators = TypeSystem.GetBinaryOperators(kind, x, y);
            if (IsBooleanOperatorKind(kind))
                return operators.Any() ? operators[0].ReturnType : typeof(bool);

            // TODO handle multiplying numeric types together: float*float=double? etc.
            // An idea was to use Roslyn to generate a lookup table for arithmetic operations
            if (operators.Count >= 1 && operators.All(o => o.ReturnType == operators[0].ReturnType)) // all operators have the same return type
                return operators[0].ReturnType;
            if (x == null && y == null)                // both null
                return typeof(Unknown);
            if (x == null || y == null)                // one is null
                return x ?? y;
            if (x == y)                                // same type
                return x;
            if (x.IsNumeric() && y.IsNumeric())        // both numeric types
                return GetBiggestNumericType(x, y);

            return typeof(Unknown);
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            var x = InputPortModels[0].ConnectionPortModels.FirstOrDefault()?.DataType;
            if (x != null)
                m_InputPortModels[0].DataType = x.Value;
            var y = InputPortModels[1].ConnectionPortModels.FirstOrDefault()?.DataType;
            if (y != null)
                m_InputPortModels[1].DataType = y.Value;

            //TODO A bit ugly of a hack... evaluate a better approach?
            m_OutputPortModels[0].DataType = GetOutputTypeFromInputs(kind, x?.Resolve(Stencil), y?.Resolve(Stencil)).GenerateTypeHandle(Stencil);
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            OnConnection(selfConnectedPortModel, otherConnectedPortModel);
        }

        public bool HasValidOperationForInput(IPortModel inputPort, TypeHandle typeHandle)
        {
            Assert.IsTrue(InputPortModels.Contains(inputPort));

            var currentPortIndex = InputPortModels.ToList().IndexOf(inputPort);
            var otherPortIndex = currentPortIndex == 0 ? 1 : 0;
            var dataType = typeHandle.Resolve(Stencil);

            if (InputPortModels[otherPortIndex].Connected)
            {
                Type otherPortType = InputPortModels[otherPortIndex].DataType.Resolve(Stencil);

                return currentPortIndex == 1
                    ? TypeSystem.IsBinaryOperationPossible(otherPortType, dataType, kind)
                    : TypeSystem.IsBinaryOperationPossible(dataType, otherPortType, kind);
            }

            return TypeSystem.GetOverloadedBinaryOperators(dataType).Contains(kind);
        }
    }
}
