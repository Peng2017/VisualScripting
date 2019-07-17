using System;

namespace UnityEditor.VisualScripting.Model
{
    public class StringConstantModel : ConstantNodeModel<String>
    {
        public StringConstantModel()
        {
            value = "";
        }
    }
}
