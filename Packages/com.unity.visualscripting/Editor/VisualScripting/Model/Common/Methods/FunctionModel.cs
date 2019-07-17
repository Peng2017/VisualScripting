using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Model
{
    public class FunctionModel : StackModel, IFunctionModel, IRenamableModel
    {
        [SerializeField]
        protected List<VariableDeclarationModel> m_FunctionVariableModels;

        [SerializeField]
        protected List<VariableDeclarationModel> m_FunctionParameterModels;

        [SerializeField]
        TypeHandle m_ReturnType;

        [SerializeField]
        bool m_EnableProfiling;

        public IEnumerable<IVariableDeclarationModel> FunctionVariableModels => m_FunctionVariableModels;
        public IEnumerable<IVariableDeclarationModel> FunctionParameterModels => m_FunctionParameterModels;

        public IList<VariableDeclarationModel> VariableDeclarations => m_FunctionVariableModels;

        public IList<VariableDeclarationModel> FunctionParameters => m_FunctionParameterModels;

        public override string Title => name;
        public string CodeTitle => TypeSystem.CodifyString(Title);

        public override string IconTypeString => "typeFunction";

        public TypeHandle ReturnType
        {
            get => m_ReturnType;
            set => m_ReturnType = value;
        }

        public virtual bool IsEntryPoint => true;
        public virtual bool AllowChangesToModel => !(this is IEventFunctionModel);
        public virtual bool AllowMultipleInstances => true;

        public bool EnableProfiling
        {
            get => m_EnableProfiling;
            set => m_EnableProfiling = value;
        }

        // TODO : Refactor needed. Use AccessibilityFlags instead
        public virtual bool IsInstanceMethod => false;

        public VariableDeclarationModel CreateFunctionVariableDeclaration(string variableName, TypeHandle variableType)
        {
            VariableDeclarationModel decl = VariableDeclarationModel.Create(variableName, variableType, false, (GraphModel)GraphModel, VariableType.FunctionVariable, ModifierFlags.None, this);
            Undo.RegisterCompleteObjectUndo(this, "Create Function Variable");
            m_FunctionVariableModels.Add(decl);
            return decl;
        }

        protected void UpdateOrCreateFunctionParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            VariableDeclarationModel existing = m_FunctionParameterModels.FirstOrDefault(p => p.VariableName == parameterName);
            if (!existing)
            {
                CreateAndRegisterParameterDeclaration(parameterName, parameterType);
                return;
            }

            existing.DataType = parameterType;
        }

        public virtual VariableDeclarationModel CreateFunctionParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            return CreateAndRegisterParameterDeclaration(parameterName, parameterType);
        }

        VariableDeclarationModel CreateAndRegisterParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            VariableDeclarationModel decl = VariableDeclarationModel.Create(parameterName, parameterType, false,
                (GraphModel) GraphModel, VariableType.FunctionParameter, ModifierFlags.None, this);
            Undo.RegisterCompleteObjectUndo(this, "Create Function Parameter");
            m_FunctionParameterModels.Add(decl);

            return decl;
        }

        public List<VariableDeclarationModel> DuplicateFunctionVariableDeclarations(List<IVariableDeclarationModel> variableDeclarationModels)
        {
            Undo.RegisterCompleteObjectUndo(this, "Create Function Declarations");

            List<VariableDeclarationModel> duplicatedModels = new List<VariableDeclarationModel>();
            foreach (VariableDeclarationModel variableDeclarationModel in variableDeclarationModels.Cast<VariableDeclarationModel>())
            {
                string uniqueName = ((VSGraphModel)GraphModel).GetUniqueName(variableDeclarationModel.Name);

                if (variableDeclarationModel.VariableType == VariableType.FunctionParameter)
                {
                    VariableDeclarationModel decl = VariableDeclarationModel.CreateNoUndoRecord(
                        uniqueName,
                        variableDeclarationModel.DataType,
                        false,
                        (GraphModel) GraphModel,
                        VariableType.FunctionParameter,
                        ModifierFlags.None,
                        this,
                        variableDeclarationModel.variableFlags,
                        variableDeclarationModel.InitializationModel);
                    m_FunctionParameterModels.Add(decl);
                    duplicatedModels.Add(decl);
                }
                else
                {
                    VariableDeclarationModel decl = VariableDeclarationModel.CreateNoUndoRecord(
                        uniqueName,
                        variableDeclarationModel.DataType,
                        false,
                        (GraphModel) GraphModel,
                        VariableType.FunctionVariable,
                        ModifierFlags.None,
                        this,
                        VariableFlags.None,
                        variableDeclarationModel.InitializationModel);
                    m_FunctionVariableModels.Add(decl);
                    duplicatedModels.Add(decl);
                }
            }

            return duplicatedModels;
        }

        public void RemoveFunctionVariableDeclaration(VariableDeclarationModel decl)
        {
            Assert.AreEqual(decl.FunctionModel, this);
            Undo.RegisterCompleteObjectUndo(this, "Remove Function Variable");
            m_FunctionVariableModels.Remove(decl);
        }

        public void RemoveFunctionParameterDeclaration(VariableDeclarationModel param)
        {
            Assert.AreEqual(param.FunctionModel, this);
            Undo.RegisterCompleteObjectUndo(this, "Remove Function Parameter");
            m_FunctionParameterModels.Remove(param);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_FunctionVariableModels == null)
                m_FunctionVariableModels = new List<VariableDeclarationModel>();
            if (m_FunctionParameterModels == null)
                m_FunctionParameterModels = new List<VariableDeclarationModel>();
        }

        protected override void OnDefineNode()
        {
            if (!m_ReturnType.IsValid)
                ReturnType = typeof(void).GenerateTypeHandle(Stencil);

            AddExecutionOutputPort(null);


            CreateLoopVariables(null);
        }

        public void CreateLoopVariables(IPortModel connectedPortModel)
        {
            VariableCreator c = new VariableCreator(this);
            OnCreateLoopVariables(c, connectedPortModel);
            c.Flush();
        }

        protected virtual void OnCreateLoopVariables(VariableCreator variableCreator, IPortModel connectedPortModel){}

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                foreach (var parameterModel in m_FunctionParameterModels)
                {
                    if(parameterModel)
                        hashCode = (hashCode * 397) ^ (parameterModel.GetHashCode());
                }
                hashCode = (hashCode * 397) ^ m_ReturnType.GetHashCode();
                return hashCode;
            }
        }

        public void Rename(string newName)
        {
            Undo.RegisterCompleteObjectUndo(this, "Rename Function");
            name = ((VSGraphModel)GraphModel).GetUniqueName(newName);
            ((VSGraphModel)GraphModel).LastChanges.RequiresRebuild = true;
        }

        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
                                                        CapabilityFlags.Movable | CapabilityFlags.Renamable;

        public void ClearVariableDeclarations()
        {
            m_FunctionVariableModels.Clear();
        }

        public void ClearParameterDeclarations()
        {
            m_FunctionParameterModels.Clear();
        }
    }
}
