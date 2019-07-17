using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Constraints;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScriptingTests
{
    sealed class TypeHandleCollectionEquivalentConstraint : CollectionItemsEqualConstraint
    {
        readonly List<ITypeMetadata> m_Expected;

        public TypeHandleCollectionEquivalentConstraint(IEnumerable<ITypeMetadata> expected)
            : base(expected)
        {
            m_Expected = expected.ToList();
        }

        protected override bool Matches(IEnumerable actual)
        {
            if (m_Expected == null)
            {
                Description = "Expected is not a valid collection";
                return false;
            }

            if (!(actual is IEnumerable<ITypeMetadata> actualCollection))
            {
                Description = "Actual is not a valid collection";
                return false;
            }

            var actualList = actualCollection.ToList();
            if (actualList.Count != m_Expected.Count)
            {
                Description = $"Collections lengths are not equal. \nExpected length: {m_Expected.Count}, " +
                    $"\nBut was: {actualList.Count}";
                return false;
            }

            for (var i = 0; i < m_Expected.Count; ++i)
            {
                var res1 = m_Expected[i].TypeHandle.ToString();
                var res2 = actualList[i].TypeHandle.ToString();
                if (!string.Equals(res1, res2))
                {
                    Description = $"Object at index {i} are not the same.\nExpected: {res1},\nBut was: {res2}";
                    return false;
                }
            }

            return true;
        }
    }

    sealed class SearcherItemCollectionEquivalentConstraint : CollectionItemsEqualConstraint
    {
        readonly List<SearcherItem> m_Expected;

        public SearcherItemCollectionEquivalentConstraint(IEnumerable<SearcherItem> expected)
            : base(expected)
        {
            m_Expected = expected.ToList();
        }

        protected override bool Matches(IEnumerable actual)
        {
            if (m_Expected == null)
            {
                Description = "Expected is not a valid collection";
                return false;
            }

            if (!(actual is IEnumerable<SearcherItem> actualCollection))
            {
                Description = "Actual is not a valid collection";
                return false;
            }

            var actualList = actualCollection.ToList();
            if (actualList.Count != m_Expected.Count)
            {
                Description = $"Collections lengths are not equal. \nExpected length: {m_Expected.Count}, " +
                    $"\nBut was: {actualList.Count}";
                return false;
            }

            for (var i = 0; i < m_Expected.Count; ++i)
            {
                var res1 = m_Expected[i].ToString();
                var res2 = actualList[i].ToString();
                if (!string.Equals(res1, res2))
                {
                    Description = $"Object at index {i} are not the same.\nExpected: {res1},\nBut was: {res2}";
                    return false;
                }

                var constraint = new SearcherItemCollectionEquivalentConstraint(m_Expected[i].Children);
                if (constraint.Matches(actualList[i].Children))
                    continue;

                Description = constraint.Description;
                return false;
            }

            return true;
        }
    }

    class ConnectedToStackConstraint : Constraint
    {
        readonly StackModel m_ExpectedStack;

        public ConnectedToStackConstraint(IStackModel expected)
            : base(expected)
        {
            m_ExpectedStack = (StackModel)expected;
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (m_ExpectedStack == null)
            {
                Description = "Expected is not a valid stack.";
                return new ConstraintResult(this, actual, false);
            }

            var isConnected = false;
            var actualStack = (StackModel)actual;

            if (actualStack == null)
            {
                Description = "Actual is not a valid stack.";
                return new ConstraintResult(this, actual, false);
            }

            var graphModel = m_ExpectedStack.GraphModel;
            isConnected |= graphModel.EdgeModels.Any(x => Equals((StackModel)x.InputPortModel.NodeModel, actualStack) && Equals((StackModel)x.OutputPortModel.NodeModel, m_ExpectedStack));
            isConnected |= graphModel.EdgeModels.Any(x => Equals((StackModel)x.InputPortModel.NodeModel, m_ExpectedStack) && Equals((StackModel)x.OutputPortModel.NodeModel, actualStack));

            if (!isConnected)
                Description = $"Actual stack [{actualStack.name}] is not connected to expected stack [{m_ExpectedStack.name}].";

            return new ConstraintResult(this, actual, isConnected);
        }
    }

    class ConnectedToConstraint : Constraint
    {
        readonly PortModel m_ExpectedPort;

        public ConnectedToConstraint(IPortModel expected)
            : base(expected)
        {
            m_ExpectedPort = (PortModel)expected;
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (m_ExpectedPort == null)
            {
                Description = "Expected is not a valid port.";
                return new ConstraintResult(this, actual, false);
            }

            var actualPort = (PortModel)actual;

            if (actualPort == null)
            {
                Description = "Actual is not a valid port.";
                return new ConstraintResult(this, actual, false);
            }

            var isConnected = m_ExpectedPort.GraphModel.GetConnections(actualPort).Any(x => x == m_ExpectedPort);

            if (!isConnected)
                Description = $"Actual port [{actualPort}] is not connected to expected port [{m_ExpectedPort}].";

            return new ConstraintResult(this, actual, isConnected);
        }
    }

    class InsideStackConstraint : Constraint
    {
        readonly StackModel m_ExpectedStack;

        public InsideStackConstraint(IStackModel expected)
            : base(expected)
        {
            m_ExpectedStack = (StackModel)expected;
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (m_ExpectedStack == null)
            {
                Description = "Expected is not a valid stack.";
                return new ConstraintResult(this, actual, false);
            }

            var actualNode = (NodeModel)actual;

            if (actualNode == null)
            {
                Description = "Actual is not a valid node.";
                return new ConstraintResult(this, actual, false);
            }

            if (m_ExpectedStack.NodeModels.Contains(actualNode))
            {
                return new ConstraintResult(this, actual, true);
            }

            Description = $"Actual node [{actualNode.name}] is not inside to expected stack [{m_ExpectedStack.name}].";
            return new ConstraintResult(this, actual, false);
        }
    }

    class IndexInStackConstraint : Constraint
    {
        readonly StackModel m_ExpectedStack;
        readonly int m_ExpectedIndex;

        public IndexInStackConstraint(int expectedIndex, IStackModel expectedStack)
            : base(expectedIndex, expectedStack)
        {
            m_ExpectedIndex = expectedIndex;
            m_ExpectedStack = (StackModel)expectedStack;
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (m_ExpectedStack == null)
            {
                Description = "Expected is not a valid stack.";
                return new ConstraintResult(this, actual, false);
            }

            var actualNode = (NodeModel)actual;

            if (actualNode == null)
            {
                Description = "Actual is not a valid node.";
                return new ConstraintResult(this, actual, false);
            }

            if ((NodeModel)m_ExpectedStack.NodeModels.ElementAt(m_ExpectedIndex) == actualNode)
            {
                return new ConstraintResult(this, actual, true);
            }

            Description = $"Actual node [{actualNode.name}] is not at index [{m_ExpectedIndex}] in expected stack [{m_ExpectedStack.name}].";
            return new ConstraintResult(this, actual, false);
        }
    }

    [PublicAPI]
    static class CustomConstraintExtensions
    {
        public static SearcherItemCollectionEquivalentConstraint SearcherItemCollectionEquivalent(
            this ConstraintExpression expression, IEnumerable<SearcherItem> expected)
        {
            var constraint = new SearcherItemCollectionEquivalentConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }

        public static ConnectedToStackConstraint ConnectedToStack(this ConstraintExpression expression, IStackModel expected)
        {
            var constraint = new ConnectedToStackConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }

        public static InsideStackConstraint InsideStack(this ConstraintExpression expression, IStackModel expected)
        {
            var constraint = new InsideStackConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }

        public static IndexInStackConstraint IndexInStack(this ConstraintExpression expression, int expectedIndex, IStackModel expectedStack)
        {
            var constraint = new IndexInStackConstraint(expectedIndex, expectedStack);
            expression.Append(constraint);
            return constraint;
        }

        public static ConnectedToConstraint ConnectedTo(this ConstraintExpression expression, IPortModel expectedPort)
        {
            var constraint = new ConnectedToConstraint(expectedPort);
            expression.Append(constraint);
            return constraint;
        }
    }

    [PublicAPI]
    class Is : NUnit.Framework.Is
    {
        public static TypeHandleCollectionEquivalentConstraint TypeHandleCollectionEquivalent(
            IEnumerable<ITypeMetadata> expected)
        {
            return new TypeHandleCollectionEquivalentConstraint(expected);
        }

        public static SearcherItemCollectionEquivalentConstraint SearcherItemCollectionEquivalent(
            IEnumerable<SearcherItem> expected)
        {
            return new SearcherItemCollectionEquivalentConstraint(expected);
        }

        public static ConnectedToStackConstraint ConnectedToStack(IStackModel expected)
        {
            return new ConnectedToStackConstraint(expected);
        }

        public static ConnectedToConstraint ConnectedTo(IPortModel expected)
        {
            return new ConnectedToConstraint(expected);
        }

        public static InsideStackConstraint InsideStack(IStackModel expected)
        {
            return new InsideStackConstraint(expected);
        }
    }

    [PublicAPI]
    class Has : NUnit.Framework.Has
    {
        public static IndexInStackConstraint IndexInStack(int expectedIndex, IStackModel expectedStack)
        {
            return new IndexInStackConstraint(expectedIndex, expectedStack);
        }
    }
}
