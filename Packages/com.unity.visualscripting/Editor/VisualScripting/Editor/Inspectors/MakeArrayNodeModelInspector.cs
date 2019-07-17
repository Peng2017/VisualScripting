using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(MakeArrayNodeModel))]
    class MakeArrayNodeModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            var nodeModel = target as MakeArrayNodeModel;

            if (nodeModel == null)
                return;

            EditorGUILayout.LabelField("Port Count", nodeModel.PortCount.ToString());

            DisplayPorts(nodeModel);
        }
    }
}
