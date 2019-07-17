using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    sealed class ClassForTest {}
    sealed class TypeSearcherDatabaseTests : BaseFixture
    {
        sealed class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            [CanBeNull]
            public override IBuilder Builder => null;
        }

        Stencil m_Stencil;

        protected override bool CreateGraphOnStartup => false;

        [SetUp]
        public new void SetUp()
        {
            m_Stencil = ScriptableObject.CreateInstance<TestStencil>();
        }

        [TestCase(typeof(MacroStencil), 0)]
        [TestCase(typeof(ClassStencil), 1)]
        public void TestGraphs(Type stencilType, int expectedResult)
        {
            const string graphName = "TestGraphs";
            const string path = "Assets/" + graphName + ".asset";

            m_Store.Dispatch(new CreateGraphAssetAction(stencilType, "TestGraph", path));

            var db = new TypeSearcherDatabase(Stencil, new List<ITypeMetadata>())
                .AddGraphs()
                .Build();

            var result = db.Search(graphName, out _);
            Assert.AreEqual(expectedResult, result.Count);

            AssetDatabase.DeleteAsset(path);
        }

        [Test]
        public void TestEnums()
        {
            var source = new List<ITypeMetadata>
            {
                m_Stencil.GenerateTypeHandle(typeof(string)).GetMetadata(m_Stencil),
                m_Stencil.GenerateTypeHandle(typeof(MemberFlags)).GetMetadata(m_Stencil)
            };

            var db = new TypeSearcherDatabase(m_Stencil, source).AddEnums().Build();
            ValidateHierarchy(db.Search("", out _), new[]
            {
                new SearcherItem("Enumerations", "", new List<SearcherItem>
                {
                    new TypeSearcherItem(
                        typeof(MemberFlags).GenerateTypeHandle(m_Stencil),
                        typeof(MemberFlags).FriendlyName()
                    )
                })
            });
        }

        [Test]
        public void TestClasses()
        {
            var source = new List<ITypeMetadata>
            {
                m_Stencil.GenerateTypeHandle(typeof(string)).GetMetadata(m_Stencil),
                m_Stencil.GenerateTypeHandle(typeof(ClassForTest)).GetMetadata(m_Stencil),
                m_Stencil.GenerateTypeHandle(typeof(MemberFlags)).GetMetadata(m_Stencil)
            };

            var db = new TypeSearcherDatabase(m_Stencil, source).AddClasses().Build();
            ValidateHierarchy(db.Search("", out _), new []
            {
                new SearcherItem("Classes", "", new List<SearcherItem>
                {
                    new SearcherItem("System", "", new List<SearcherItem>
                    {
                        new TypeSearcherItem(
                            typeof(string).GenerateTypeHandle(m_Stencil),
                            typeof(string).FriendlyName()
                        )
                    }),
                    new SearcherItem("UnityEditor", "", new List<SearcherItem>
                    {
                        new SearcherItem("VisualScriptingTests", "", new List<SearcherItem>
                        {
                            new SearcherItem("SmartSearch", "", new List<SearcherItem>
                            {
                                new TypeSearcherItem(
                                    typeof(ClassForTest).GenerateTypeHandle(m_Stencil),
                                    typeof(ClassForTest).FriendlyName()
                                )
                            })
                        })
                    })
                })
            });
        }

        static void ValidateHierarchy(IReadOnlyList<SearcherItem> result, IEnumerable<SearcherItem> hierarchy)
        {
            var index = 0;
            TraverseHierarchy(result, hierarchy, ref index);
            Assert.AreEqual(result.Count, index);
        }

        static void TraverseHierarchy(
            IReadOnlyList<SearcherItem> result,
            IEnumerable<SearcherItem> hierarchy,
            ref int index
        )
        {
            foreach (var item in hierarchy)
            {
                Assert.AreEqual(item.Name, result[index].Name);

                if (item.Parent != null)
                    Assert.AreEqual(item.Parent.Name, result[index].Parent.Name);

                index++;

                TraverseHierarchy(result, item.Children, ref index);
            }
        }
    }
}
