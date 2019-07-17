using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGroupNodeModel : INodeModel
    {
        IEnumerable<INodeModel> NodeModels { get; }
    }
}
