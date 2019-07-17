using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests
{
    class Type1FakeNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            AddDataInput<GameObject>("input0");
            AddDataOutputPort<GameObject>("output0");
        }
    }
}
