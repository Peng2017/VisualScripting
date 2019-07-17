using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class VSEditorDataModel : IEditorDataModel
    {
        readonly VseWindow m_Win;
        static VSEditorPrefs s_EditorPrefs;

        public Action<RequestCompilationOptions> OnCompilationRequest;

        // We actually serialize these in VseWindow, so going through this interface should as well
        public Rect BlackboardPosition
        {
            get => m_Win.BlackboardPosition;
            set => m_Win.BlackboardPosition = value;
        }

        List<string> BlackboardExpandedRowStates => m_Win.BlackboardExpandedRowStates;
        List<ScriptableObject> ElementModelsToSelectUponCreation => m_Win.ElementModelsToSelectUponCreation;
        List<ScriptableObject> ElementModelsToActivateUponCreation => m_Win.ElementModelsToActivateUponCreation;
        List<ScriptableObject> ElementModelsToExpandUponCreation => m_Win.ElementModelsToExpandUponCreation;

        // IEditorDataModel
        public UpdateFlags UpdateFlags { get; private set; }
        public IGraphElementModel ElementModelToRename { get; set; }
        public int NodeToFrameInstanceId { get; set; } = -1;
        public int CurrentGraphIndex => 0;
        public VSPreferences Preferences => s_EditorPrefs;

        public VSEditorDataModel(VseWindow win)
        {
            m_Win = win;
        }

        static VSEditorDataModel()
        {
            s_EditorPrefs = new VSEditorPrefs();
        }

        // We actually serialize this object in VseWindow, so going through this interface should as well
        public object BoundObject
        {
            get => m_Win.BoundObject;
            set => m_Win.SetBoundObject(value  as GameObject);
        }

        public List<GraphModel> PreviousGraphModels => m_Win.PreviousGraphModels;

        public int UpdateCounter { get; set; }

        public IPluginRepository PluginRepository { get; set; }

        public void SetUpdateFlag(UpdateFlags flag)
        {
            UpdateFlags = flag;
        }

        public void RequestCompilation(RequestCompilationOptions options)
        {
            OnCompilationRequest?.Invoke(options);
        }

        public bool ShouldExpandBlackboardRowUponCreation(string rowName)
        {
            return BlackboardExpandedRowStates.Any(x => x == rowName);
        }

        public void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand)
        {
            if (expand)
            {
                foreach (var rowName in rowNames)
                {
                    if (!BlackboardExpandedRowStates.Any(x => x == rowName))
                        BlackboardExpandedRowStates.Add(rowName);
                }
            }
            else
            {
                foreach (var rowName in rowNames)
                {
                    var foundIndex = BlackboardExpandedRowStates.FindIndex(x => x == rowName);
                    if (foundIndex != -1)
                        BlackboardExpandedRowStates.RemoveAt(foundIndex);
                }
            }
        }

        public bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            if (hasGraphElementModel?.GraphElementModel is ScriptableObject so)
                return ElementModelsToSelectUponCreation.Contains(so);
            return false;
        }

        public void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select)
        {
            if (select)
            {
                ElementModelsToSelectUponCreation.AddRange(graphElementModels
                    .OfType<ScriptableObject>()
                    .Where(x => !ElementModelsToExpandUponCreation.Contains(x)));
            }
            else
            {
                var soRange = graphElementModels.OfType<ScriptableObject>();
                ElementModelsToSelectUponCreation.RemoveAll(x => soRange.Contains(x));
            }
        }

        public void ClearElementsToSelectUponCreation()
        {
            ElementModelsToSelectUponCreation.Clear();
        }

        public bool ShouldActivateElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            if (hasGraphElementModel?.GraphElementModel is ScriptableObject so)
                return ElementModelsToActivateUponCreation.Contains(so);
            return false;
        }

        public void ActivateElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool activate)
        {
            if (activate)
            {
                ElementModelsToActivateUponCreation.AddRange(graphElementModels
                    .OfType<ScriptableObject>()
                    .Where(x => !ElementModelsToActivateUponCreation.Contains(x)));
            }
            else
            {
                var soRange = graphElementModels.OfType<ScriptableObject>();
                ElementModelsToActivateUponCreation.RemoveAll(x => soRange.Contains(x));
            }
        }

        public bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField)
        {
            if (visualScriptingField?.ExpandableGraphElementModel is ScriptableObject so)
                return ElementModelsToExpandUponCreation.Contains(so);
            return false;
        }

        public void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand)
        {
            if (expand)
                ElementModelsToExpandUponCreation.AddRange(visualScriptingFields
                    .Select(x => x.ExpandableGraphElementModel)
                    .OfType<ScriptableObject>()
                    .Where(x => !ElementModelsToExpandUponCreation.Contains(x)));
            else
                ElementModelsToExpandUponCreation.RemoveAll(x => (visualScriptingFields.FirstOrDefault(y =>
                        Equals(y.ExpandableGraphElementModel, x)) != null));
        }
    }
}
