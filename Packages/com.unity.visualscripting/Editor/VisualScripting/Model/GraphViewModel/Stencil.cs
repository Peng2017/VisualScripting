using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Packages.VisualScripting.Editor.Helpers;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    [Flags]
    [Serializable]
    public enum StencilCapabilityFlags
    {
        SupportsMacros = 1 << 0,
    }

    [PublicAPI]
    public abstract class Stencil : ScriptableObject
    {
        static readonly string[] k_BlackListedAssemblies =
        {
            "boo.lang",
            "castle.core",
            "excss.unity",
            "jetbrains",
            "lucene",
            "microsoft",
            "mono",
            "moq",
            "nunit",
            "system.web",
            "unityscript",
            "visualscriptingassembly-csharp"
        };

        static IEnumerable<Assembly> s_Assemblies;

        internal static IEnumerable<Assembly> CachedAssemblies
        {
            get
            {
                return s_Assemblies ?? (s_Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic
                        && !k_BlackListedAssemblies.Any(b => a.GetName().Name.ToLower().Contains(b)))
                    .ToList());
            }
        }

        public bool RecompilationRequested { get; set; }

        public virtual IEnumerable<string> PropertiesVisibleInGraphInspector() { yield break; }

        List<ITypeMetadata> m_AssembliesTypes;

        protected IBlackboardProvider m_BlackboardProvider;

        public bool addCreateAssetMenuAttribute;
        public string fileName = "";
        public string menuName = "";

        GraphContext m_GraphContext;

        public virtual IExternalDragNDropHandler DragNDropHandler => null;

        public GraphContext GraphContext => m_GraphContext ?? (m_GraphContext = CreateGraphContext());
        protected virtual GraphContext CreateGraphContext()
        {
            return new GraphContext();
        }

        public virtual ITranslator CreateTranslator()
        {
            return new RoslynTranslator(this);
        }

        public virtual TypeHandle GetThisType()
        {
            return typeof(object).GenerateTypeHandle(this);
        }

        public virtual Type GetBaseClass()
        {
            return typeof(object);
        }

        public virtual Type GetDefaultStackModelType()
        {
            return typeof(StackModel);
        }

        public virtual IBlackboardProvider GetBlackboardProvider()
        {
            return m_BlackboardProvider ?? (m_BlackboardProvider = new BlackboardProvider(this));
        }

        public virtual IEnumerable<Assembly> GetAssemblies()
        {
            return CachedAssemblies;
        }

        public virtual List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = GetAssemblies().SelectMany(a => a.GetTypesSafe()).ToList();
            m_AssembliesTypes = TaskUtility.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (!type.IsAbstract && !type.IsInterface && type.IsPublic
                    && !Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                    cb.Add(GenerateTypeHandle(type).GetMetadata(this));
            }).ToList();

            return m_AssembliesTypes;
        }

        [CanBeNull]
        public virtual ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return null;
        }

        public abstract ISearcherDatabaseProvider GetSearcherDatabaseProvider();

        public virtual void OnCompilationSucceeded(VSGraphModel graphModel, CompilationResult results) { }

        public virtual void OnCompilationFailed(VSGraphModel graphModel, CompilationResult results) { }

        public virtual IEnumerable<IPluginHandler> GetCompilationPluginHandlers(CompilationOptions getCompilationOptions)
        {
            if (getCompilationOptions.HasFlag(CompilationOptions.Tracing))
                yield return new DebugInstrumentationHandler();
        }

        public virtual TypeHandle GenerateTypeHandle(Type t)
        {
            return GraphContext.TypeHandleSerializer.GenerateTypeHandle(t);
        }

        public TypeHandle GenerateTypeHandle(VSGraphModel vsGraphAssetModel)
        {
            return GraphContext.TypeHandleSerializer.GenerateTypeHandle(vsGraphAssetModel);
        }

        public virtual IVariableInitializer GetVariableInitializer()
        {
            return GraphContext.VariableInitializer;
        }

        public virtual StencilCapabilityFlags Capabilities => 0;
        public abstract IBuilder Builder { get; }

        public virtual string GetSourceFilePath(VSGraphModel graphModel)
        {
            return Path.Combine(ModelUtility.GetCompileScriptsOutputDirectory(), graphModel.TypeName + ".cs");
        }

        public virtual void RegisterReducers(Store store)
        {
        }

        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;
        public virtual Type GetConstantNodeModelType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantNodeModelTypeCache == null)
            {
                s_TypeToConstantNodeModelTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { typeof(Boolean).GenerateTypeHandle(this), typeof(BooleanConstantNodeModel) },
                    { typeof(Color).GenerateTypeHandle(this), typeof(ColorConstantModel) },
                    { typeof(AnimationCurve).GenerateTypeHandle(this), typeof(CurveConstantNodeModel) },
                    { typeof(Double).GenerateTypeHandle(this), typeof(DoubleConstantModel) },
                    { typeof(Unknown).GenerateTypeHandle(this), typeof(EnumConstantNodeModel) },
                    { typeof(Single).GenerateTypeHandle(this), typeof(FloatConstantModel) },
                    { typeof(InputName).GenerateTypeHandle(this), typeof(InputConstantModel) },
                    { typeof(Int32).GenerateTypeHandle(this), typeof(IntConstantModel) },
                    { typeof(LayerName).GenerateTypeHandle(this), typeof(LayerConstantModel) },
                    { typeof(LayerMask).GenerateTypeHandle(this), typeof(LayerMaskConstantModel) },
                    { typeof(Object).GenerateTypeHandle(this), typeof(ObjectConstantModel) },
                    { typeof(Quaternion).GenerateTypeHandle(this), typeof(QuaternionConstantModel) },
                    { typeof(String).GenerateTypeHandle(this), typeof(StringConstantModel) },
                    { typeof(TagName).GenerateTypeHandle(this), typeof(TagConstantModel) },
                    { typeof(Type).GenerateTypeHandle(this), typeof(TypeConstantModel) },
                    { typeof(Vector2).GenerateTypeHandle(this), typeof(Vector2ConstantModel) },
                    { typeof(Vector3).GenerateTypeHandle(this), typeof(Vector3ConstantModel) },
                    { typeof(Vector4).GenerateTypeHandle(this), typeof(Vector4ConstantModel) },
                    { typeof(SceneAsset).GenerateTypeHandle(this), typeof(ConstantSceneAssetNodeModel) },
                };
            }

            if (s_TypeToConstantNodeModelTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve(this);
            if(t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstantNodeModel);

            return null;
        }

        public virtual void PreProcessGraph(VSGraphModel graphModel)
        {
        }
    }
}
