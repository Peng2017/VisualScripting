using System.Collections;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    [SearcherItem(typeof(MonoStencil), SearcherContext.Graph, "Events/Update")]
    public class UpdateNodeModel : EventFunctionModel
    {
    }
}
