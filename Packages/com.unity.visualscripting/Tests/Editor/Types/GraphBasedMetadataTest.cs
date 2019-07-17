using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    class GraphBasedMetadataTest
    {
        static readonly List<VSGraphModel> k_GraphsToDelete = new List<VSGraphModel>();
        const string k_Name = "Name";
        static TypeHandle s_GraphTypeHandle;

        Mock<IVSGraphModel> m_MockGraphModel;
        Mock<IVariableDeclarationModel> m_MockNonPublicGraphVariable;
        Mock<IVariableDeclarationModel> m_MockPublicGraphVariable;

        ITypeMetadata m_MockedMetadata;

        [SetUp]
        public void SetUp()
        {
            TypeHandle variableHandle = new TypeHandle(null, "__VARIABLETYPEHANDLE");

            m_MockPublicGraphVariable = new Mock<IVariableDeclarationModel>();
            m_MockPublicGraphVariable.SetupGet(decl => decl.IsExposed).Returns(true);
            m_MockPublicGraphVariable.SetupGet(decl => decl.DataType).Returns(variableHandle);
            m_MockPublicGraphVariable.SetupGet(decl => decl.Name).Returns("publicField");

            m_MockNonPublicGraphVariable = new Mock<IVariableDeclarationModel>();
            m_MockNonPublicGraphVariable.SetupGet(decl => decl.IsExposed).Returns(false);
            m_MockNonPublicGraphVariable.SetupGet(decl => decl.DataType).Returns(variableHandle);
            m_MockNonPublicGraphVariable.SetupGet(decl => decl.Name).Returns("privateField");

            List<IVariableDeclarationModel> graphVariables = new List<IVariableDeclarationModel>
                {m_MockPublicGraphVariable.Object, m_MockNonPublicGraphVariable.Object};

            var assetModel = new Mock<IGraphAssetModel>();
            assetModel.SetupGet(x => x.Name).Returns(k_Name);

            m_MockGraphModel = new Mock<IVSGraphModel>();
            m_MockGraphModel.SetupGet(m => m.AssetModel).Returns(assetModel.Object);
            m_MockGraphModel.SetupGet(meta => meta.GraphVariableModels).Returns(graphVariables);

            s_GraphTypeHandle = CreateNewGraphHandle();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var graph in k_GraphsToDelete)
                Object.DestroyImmediate(graph);
            k_GraphsToDelete.Clear();
        }

        static TypeHandle CreateNewGraphHandle()
        {
            var newGraph = ScriptableObject.CreateInstance<VSGraphModel>();
            k_GraphsToDelete.Add(newGraph);
            return new TypeHandle(newGraph);
        }

        GraphBasedMetadata CreateGraphMetadata()
        {
            return CreateGraphMetadata(s_GraphTypeHandle, m_MockGraphModel.Object);
        }

        static GraphBasedMetadata CreateGraphMetadata(TypeHandle handle, IVSGraphModel graph)
        {
            return new GraphBasedMetadata(new TestGraphTypeSerializer(), handle, graph);
        }

        [Test]
        public void Test_TypeHandle()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            TypeHandle handle = graphMetadata.TypeHandle;

            //Assert
            Assert.That(handle, Is.EqualTo(s_GraphTypeHandle));
        }

        [Test]
        public void Test_Name()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            string graphTypeName = graphMetadata.Name;

            //Assert
            Assert.That(graphTypeName, Is.EqualTo(k_Name));
        }


        [Test]
        public void Test_FriendlyName()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            string friendlyName = graphMetadata.FriendlyName;

            //Assert
            Assert.That(friendlyName, Is.EqualTo(k_Name));
        }

        [Test]
        public void Test_GenericArguments()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            List<TypeHandle> genericArguments = graphMetadata.GenericArguments.ToList();

            //Assert
            CollectionAssert.IsEmpty(genericArguments);
        }



        [Test]
        public void Test_IsAssignableFrom_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsAssignableTo(It.IsAny<IVSGraphModel>())).Returns(true);

            //Act
            bool isAssignableFrom = graphMetadata.IsAssignableFrom(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsAssignableTo(It.IsAny<IVSGraphModel>()), Times.Once());
            Assert.That(isAssignableFrom, Is.True);
        }

//        [Test]
//        public void Test_IsAssignableFrom_UsingType_IsFalse()
//        {
//            //Arrange
//            var graphMetadata = CreateGraphMetadata();
//            var dummyType = typeof(VisualBehaviour);
//
//            //Act
//            bool isAssignableFrom = graphMetadata.IsAssignableFrom(dummyType);
//
//            //Assert
//            Assert.That(isAssignableFrom, Is.False);
//        }

        [Test]
        public void Test_IsAssignableFrom_UsingGraph_IsTrue_WhenSameGraph()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata(s_GraphTypeHandle, s_GraphTypeHandle.GraphModelReference);

            //Act
            bool isAssignableFrom = graphMetadata.IsAssignableFrom( s_GraphTypeHandle.GraphModelReference);

            //Assert
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableFrom_UsingGraph_IsFalse_WhenDifferentGraph()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata(s_GraphTypeHandle, s_GraphTypeHandle.GraphModelReference);

            //Act
            bool isAssignableFrom = graphMetadata.IsAssignableFrom(CreateNewGraphHandle().GraphModelReference);

            //Assert
            Assert.That(isAssignableFrom, Is.False);
        }



        [Test]
        public void Test_IsAssignableTo_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsAssignableFrom(It.IsAny<IVSGraphModel>())).Returns(true);

            //Act
            bool isAssignableTo = graphMetadata.IsAssignableTo(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsAssignableFrom(It.IsAny<IVSGraphModel>()), Times.Once());
            Assert.That(isAssignableTo, Is.True);
        }

//        [Test]
//        public void Test_IsAssignableTo_UsingType_IsTrue_WhenSuperclassOfGraph()
//        {
//            //Arrange
//            var graphMetadata = CreateGraphMetadata();
//
//            //Act
//            bool isAssignableTo = graphMetadata.IsAssignableTo(typeof(VisualBehaviour));
//
//            //Assert
//            Assert.That(isAssignableTo, Is.True);
//        }

        [Test]
        public void Test_IsAssignableTo_UsingType_IsFalse_WhenNotSuperclassOfGraph()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            bool isAssignableTo = graphMetadata.IsAssignableTo(typeof(Vector3));

            //Assert
            Assert.That(isAssignableTo, Is.False);
        }

        [Test]
        public void Test_IsAssignableTo_UsingGraph_IsTrue_WhenSameGraph()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata(s_GraphTypeHandle, s_GraphTypeHandle.GraphModelReference);

            //Act
            bool isAssignableTo = graphMetadata.IsAssignableTo(s_GraphTypeHandle.GraphModelReference);

            //Assert
            Assert.That(isAssignableTo, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_UsingGraph_IsFalse_WhenDifferentGraph()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata(s_GraphTypeHandle, s_GraphTypeHandle.GraphModelReference);

            //Act
            bool isAssignableTo = graphMetadata.IsAssignableTo(CreateNewGraphHandle().GraphModelReference);

            //Assert
            Assert.That(isAssignableTo, Is.False);
        }



        [Test]
        public void Test_IsSuperclassOf_UsingMetadata_IsFalse()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();
            var mockMetadata = new Mock<ITypeMetadata>();

            //Act
            bool isSuperclassOf = graphMetadata.IsSuperclassOf(mockMetadata.Object);

            //Assert
            Assert.That(isSuperclassOf, Is.False);
        }

//        [Test]
//        public void Test_IsSuperclassOf_UsingType_IsFalse()
//        {
//            //Arrange
//            var graphMetadata = CreateGraphMetadata();
//
//            //Act
//            bool isSuperclassOf = graphMetadata.IsSuperclassOf(typeof(VisualBehaviour));
//
//            //Assert
//            Assert.That(isSuperclassOf, Is.False);
//        }

        [Test]
        public void Test_IsSuperclassOf_UsingGraph_IsFalse()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            bool isSuperclassOf = graphMetadata.IsSuperclassOf(graphMetadata.TypeHandle.GraphModelReference);

            //Assert
            Assert.That(isSuperclassOf, Is.False);
        }



        [Test]
        public void Test_IsSubclassOf_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsSuperclassOf(It.IsAny<IVSGraphModel>())).Returns(true);

            //Act
            bool isSubclassOf = graphMetadata.IsSubclassOf(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsSuperclassOf(It.IsAny<IVSGraphModel>()), Times.Once());
            Assert.That(isSubclassOf, Is.True);
        }

//        [Test]
//        public void Test_IsSubclassOf_UsingType_IsTrue_WhenUsingSuperclassOfGraph()
//        {
//            //Arrange
//            var graphMetadata = CreateGraphMetadata();
//
//            //Act
//            bool isSubclassOfSuperClass = graphMetadata.IsSubclassOf(typeof(VisualBehaviour));
//            bool isSubclassOfClassRoot = graphMetadata.IsSubclassOf(typeof(UnityEngine.Object));
//
//            //Assert
//            Assert.That(isSubclassOfSuperClass, Is.True);
//            Assert.That(isSubclassOfClassRoot, Is.True);
//        }

        [Test]
        public void Test_IsSubclassOf_UsingGraph_IsFalse()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            bool isSubclassOf = graphMetadata.IsSubclassOf(graphMetadata.TypeHandle.GraphModelReference);

            //Assert
            Assert.That(isSubclassOf, Is.False);
        }

        [Test]
        public void Test_GetPublicMembers()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            List<MemberInfoValue> members = graphMetadata.PublicMembers;

            //Assert
            var graphVariable = m_MockPublicGraphVariable.Object;
            var publicMemberInfoDTO = new MemberInfoValue(s_GraphTypeHandle, graphVariable.DataType, graphVariable.Name, MemberTypes.Field);
            CollectionAssert.Contains(members, publicMemberInfoDTO);
        }

        [Test]
        public void Test_GetNonPublicMembers()
        {
            //Arrange
            var graphMetadata = CreateGraphMetadata();

            //Act
            List<MemberInfoValue> members = graphMetadata.NonPublicMembers;

            //Assert
            var graphVariable = m_MockNonPublicGraphVariable.Object;
            var publicMemberInfoDTO = new MemberInfoValue(s_GraphTypeHandle, graphVariable.DataType, graphVariable.Name, MemberTypes.Field);
            CollectionAssert.Contains(members, publicMemberInfoDTO);
        }

        class TestGraphTypeSerializer : ITypeHandleSerializer
        {
            CSharpTypeSerializer m_TypeSerializer = new CSharpTypeSerializer();
            public VSGraphModel ResolveGraph(TypeHandle th)
            {
                throw new NotImplementedException();
            }

            public TypeHandle GenerateTypeHandle(VSGraphModel vsGraphAssetModel)
            {
                return s_GraphTypeHandle;
            }

            public Type ResolveType(TypeHandle th)
            {
                return m_TypeSerializer.ResolveType(th);
            }

            public TypeHandle GenerateTypeHandle(Type t)
            {
                return m_TypeSerializer.GenerateTypeHandle(t);
            }
        }
    }
}
