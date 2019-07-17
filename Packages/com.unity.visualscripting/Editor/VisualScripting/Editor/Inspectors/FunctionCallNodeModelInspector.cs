using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(FunctionCallNodeModel))]
    class FunctionCallNodeModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            var decl = target as FunctionCallNodeModel;
            if (decl == null)
                return;

            var index = 0;
            if (decl.TypeArguments != null)
            {
                var graph = ((IGraphElementModel)target)?.GraphModel;
                if (graph != null)
                {
                    foreach (var typeArgument in decl.TypeArguments)
                    {
                        var closureIndex = index;
                        graph.Stencil.TypeEditor(typeArgument,
                            (theType, i) =>
                            {
                                decl.TypeArguments[closureIndex] = theType;
                                decl.OnConnection(null, null);
                                refreshUI();
                            });
                        index++;
                    }
                }
            }

            DisplayPorts(decl);
        }
    }
}
