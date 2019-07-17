using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.UI
{
    class ResizableElementTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator TestThatViewportLimitsBlackboardResize()
        {
            yield return TestThatViewportLimitsElementResize(GraphView.UIController.Blackboard);
        }

        IEnumerator TestThatViewportLimitsElementResize(GraphElement element)
        {
            Assert.That(element != null);
            Assert.That(element.IsResizable());

            Rect elementRect = element.GetPosition();
            var layoutMax = new Vector2(Window.rootVisualElement.layout.width, Window.rootVisualElement.layout.height);

            // (-10, +15) corresponds to a delta of (-10, -10) followed by a delta of (0, +25) to account for graphView dock bar at the top
            var delta = new Vector2(-10, 15);

            // Resize the element using lower right Resizer manipulator
            Vector2 start = element.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement, new Vector2(elementRect.xMax, elementRect.yMax) + delta);

            Helpers.MouseDownEvent(start);
            yield return null;

            // Resize much bigger than the actual layout
            Vector2 target = element.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement,
                new Vector2(layoutMax.x + 1000, layoutMax.y + 1000));

            Helpers.MouseMoveEvent(start, target);
            yield return null;

            Helpers.MouseUpEvent(target);
            yield return null;

            // Check that the new element's lower right corner does not exceed the viewport's width and height
            Rect newElementRect =
                element.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement,
                    element.GetPosition());
            var newPosition = new Vector2(newElementRect.xMax, newElementRect.yMax);
            Assert.That(newPosition, Is.EqualTo(layoutMax));
        }
    }
}
