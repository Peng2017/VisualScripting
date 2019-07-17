using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    static class MakeArrayReducers
    {
        public static void Register(Store store)
        {
            store.Register<ChangeMakeArrayNodePortCountAction>(ChangeMakeArrayNodePortCount);
        }

        static State ChangeMakeArrayNodePortCount(State previousState, ChangeMakeArrayNodePortCountAction action)
        {
            foreach (var model in action.nodeModels)
            {
                model.IncreasePortCount(action.portCountChange);
            }

            return previousState;
        }
    }
}
