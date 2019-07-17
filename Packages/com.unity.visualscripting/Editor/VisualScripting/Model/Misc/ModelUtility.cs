using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public static class ModelUtility
    {
        static readonly string k_CompileScriptsOutputDirectory = Path.Combine(Environment.CurrentDirectory, "Library", "VisualScripting");
        static readonly string k_AssemblyRelativePath = Path.Combine("Assets", "Runtime", "VisualScripting");
        static readonly string k_AssemblyOutputDirectory = Path.Combine(Environment.CurrentDirectory, k_AssemblyRelativePath);

        public static string GetCompileScriptsOutputDirectory()
        {
            return k_CompileScriptsOutputDirectory;
        }

        public static string GetAssemblyOutputDirectory()
        {
            return k_AssemblyOutputDirectory;
        }

        public static string GetAssemblyRelativePath()
        {
            return k_AssemblyRelativePath;
        }

        public static class ExtensionMethodCache<TExtendedType>
        {
            // ReSharper disable once StaticMemberInGenericType
            static Dictionary<Type, MethodInfo> s_FactoryMethods;

            public static MethodInfo GetExtensionMethod(Type targetType, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
            {
                var extendedType = typeof(TExtendedType);

                return GetExtensionMethodOf(extendedType, targetType, filterMethods, keySelector);
            }

            static MethodInfo GetExtensionMethodOf(Type extendedType, Type targetType, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
            {
                if (targetType == typeof(ScriptableObject))
                    return null;

                if (s_FactoryMethods == null)
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.StartsWith("Microsoft"));
                    var methodInfos = TypeSystem.GetExtensionMethods(assemblies)[extendedType].Where(filterMethods);

                    s_FactoryMethods = new Dictionary<Type, MethodInfo>();
                    foreach (var methodInfo in methodInfos)
                    {
                        var key = keySelector(methodInfo);
                        if (s_FactoryMethods.TryGetValue(key, out var prevValue))
                        {
                            Debug.LogError($"Duplicate extension methods for type {key}, previous value: {prevValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                        }
                        s_FactoryMethods[key] = methodInfo;
                    }
                }


                if (s_FactoryMethods.ContainsKey(targetType))
                    return s_FactoryMethods[targetType];

                MethodInfo extension = null;

                foreach (var type in GetInterfaces(targetType, false))
                {
                    extension = GetExtensionMethod(type, filterMethods, keySelector);
                    if (extension != null)
                        break;
                }


                if (extension == null && targetType.BaseType != null)
                    extension = GetExtensionMethod(targetType.BaseType, filterMethods, keySelector);

                //Did we find a builder for one of our base class?
                //If so, add it to the dictionary, this will optimize the next call
                //for this type
                if (extension != null)
                    s_FactoryMethods[targetType] = extension;
//                else
//                {
//                    if (extendedType.BaseType != null)
//                        return GetExtensionMethodOf(extendedType.BaseType, t, filterMethods, keySelector);
//                }

                return extension;
            }

            static IEnumerable<Type> GetInterfaces(Type type, bool includeInherited)
            {
                if (includeInherited || type.BaseType == null)
                    return type.GetInterfaces();

                return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
            }
        }
    }
}
