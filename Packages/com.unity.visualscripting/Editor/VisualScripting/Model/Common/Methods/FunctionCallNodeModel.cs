using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Mode;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    public class FunctionCallNodeModel : NodeModel
    {
        MethodBase m_MethodInfo;

        [SerializeField]
        TypeHandle m_DeclaringType;
        [SerializeField]
        string m_MethodInfoString;
        [SerializeField]
        TypeHandle[] m_TypeArguments;
        [SerializeField]
        bool m_IsStatic;
        [SerializeField]
        int m_OverloadHashCode;

        bool m_HasSeparateInstancePort;

        public override string Title => $"{VseUtility.GetTitle(m_MethodInfo)}";

        public bool IsConstructor => MethodInfo.IsConstructor;
        public TypeHandle DeclaringType => m_DeclaringType;

        public TypeHandle[] TypeArguments
        {
            get => m_TypeArguments;
            set => m_TypeArguments = value;
        }

        public MethodBase MethodInfo
        {
            get => m_MethodInfo;
            set
            {
                m_MethodInfo = value;
                m_DeclaringType = value?.DeclaringType.GenerateTypeHandle(Stencil) ?? TypeHandle.Unknown;
                m_MethodInfoString = value?.Name;
                m_IsStatic = MethodInfo.IsStatic;
                m_OverloadHashCode = TypeSystem.HashMethodSignature(value);
                if (value == null)
                    return;
                if (value.IsGenericMethod)
                    m_TypeArguments = value.GetGenericArguments().Where(t => String.IsNullOrEmpty(t.AssemblyQualifiedName)).
                        Select(t => t.GenerateTypeHandle(Stencil)).ToArray();
            }
        }

        protected override void OnDefineNode()
        {
            if (m_DeclaringType.IsValid && m_MethodInfoString != null)
            {
                // deprecation of name base search
                if (m_OverloadHashCode == 0)
                {
                    m_MethodInfo = TypeSystem.GetMethod(m_DeclaringType.Resolve(Stencil), m_MethodInfoString, m_IsStatic);
                    if (m_MethodInfo != null)
                        m_OverloadHashCode = TypeSystem.HashMethodSignature(m_MethodInfo);
                }
                else
                {
                    //TODO: Investigate why we cannot use SingleOrDefault here.
                    m_MethodInfo = m_DeclaringType.Resolve(Stencil)
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | (m_IsStatic ? BindingFlags.Static : BindingFlags.Instance))
                        .Cast<MethodBase>()
                        .Concat(m_DeclaringType.Resolve(Stencil).GetConstructors())
                        .FirstOrDefault(m => TypeSystem.HashMethodSignature(m) == m_OverloadHashCode);
                }

                if(m_MethodInfo == null) // only log error if the data is kind-of making sense, not if if it's just empty
                    Debug.LogError("Serialization Error: Could not find method from MethodInfo:" + m_MethodInfoString);
            }

            if (m_MethodInfo != null)
            {
                if (!m_MethodInfo.IsStatic && !m_MethodInfo.IsConstructor)
                {
                    m_HasSeparateInstancePort = true;
                    AddInstanceInput(m_MethodInfo.DeclaringType.GenerateTypeHandle(Stencil));
                }

                foreach (ParameterInfo parameter in m_MethodInfo.GetParameters())
                    AddDataInput(parameter.Name.Nicify(), parameter.ParameterType.GenerateTypeHandle(Stencil));

                if (m_MethodInfo.ContainsGenericParameters)
                {
                    var methodGenericArguments = m_MethodInfo.GetGenericArguments().Concat(m_MethodInfo?.DeclaringType?.GetGenericArguments()??Enumerable.Empty<Type>());
                    if (m_TypeArguments == null)
                        m_TypeArguments = methodGenericArguments.Select(o => o.GenerateTypeHandle(Stencil)).ToArray();
                    else
                    {
                        Type[] methodGenericArgumentsArray = methodGenericArguments.ToArray();
                        if (m_TypeArguments.Length != methodGenericArgumentsArray.Length)
                        {
                            // manual copy because of the implicit cast operator of TypeReference
                            Array.Resize(ref m_TypeArguments, methodGenericArgumentsArray.Length);
                            for (int i = m_TypeArguments.Length; i < methodGenericArgumentsArray.Length; i++)
                                m_TypeArguments[i] = methodGenericArgumentsArray[i].GenerateTypeHandle(Stencil);
                        }
                    }
                    if(m_TypeArguments.Length > 1)
                        Debug.LogWarning("Multiple generic types are not implemented yet");
                }

                var returnType = m_MethodInfo.GetReturnType();
                if (returnType.IsGenericType)
                {
                    returnType = returnType.GetGenericTypeDefinition();
                }

                if (returnType != typeof(void))
                {
                    AddDataOutputPort("result", returnType.GenerateTypeHandle(Stencil));
                }
            }
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            try
            {
                if (selfConnectedPortModel == null || selfConnectedPortModel.Direction != Direction.Input)
                    return;

                Type otherConnectedType = otherConnectedPortModel?.DataType.Resolve(Stencil);
                if (m_MethodInfo.ContainsGenericParameters && otherConnectedType != null)
                {
                    var parameterIndex = selfConnectedPortModel.Index - (m_HasSeparateInstancePort ? 1 : 0);
                    Type[] genericTypes = null;
                    if (!GenericsInferenceSolver.SolveTypeArguments(Stencil, m_MethodInfo, ref genericTypes, ref m_TypeArguments, otherConnectedType, parameterIndex))
                        return;
                    GenericsInferenceSolver.ApplyTypeArgumentsToParameters(Stencil, m_MethodInfo, genericTypes, m_TypeArguments, m_InputPortModels, m_HasSeparateInstancePort);

                    PortModel outputPortModel = m_OutputPortModels.FirstOrDefault();
                    if (outputPortModel == null)
                        return;

                    Type outputType = GenericsInferenceSolver.InferReturnType(Stencil, m_MethodInfo, genericTypes, m_TypeArguments, outputPortModel.DataType.Resolve(Stencil));
                    if (outputType != null)
                    {
                        outputPortModel.DataType = outputType.GenerateTypeHandle(Stencil);
                        outputPortModel.Name = outputType.FriendlyName();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during OnConnection of node: {this}\n{e}");
                throw;
            }
        }
    }
}
