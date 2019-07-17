using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScriptingTests
{
    class Type2FakeNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            AddDataInput<VSArray<int>>("input0");
            AddDataOutputPort<VSArray<int>>("output0");
        }
    }
}
