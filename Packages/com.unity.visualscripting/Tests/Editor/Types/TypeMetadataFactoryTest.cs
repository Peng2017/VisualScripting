using System;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    class TypeMetadataFactoryTest
    {
        static readonly TypeHandle k_TypeBasedHandle = new TypeHandle(null,"__TYPE");
        static TypeHandle s_GraphBasedHandle;

        static readonly ITypeMetadata k_TypeBasedMetadata = new Mock<ITypeMetadata>().Object;
        static readonly ITypeMetadata k_GraphBasedMetadata = new Mock<ITypeMetadata>().Object;

        [SetUp]
        public void SetUp()
        {
            s_GraphBasedHandle = new TypeHandle(ScriptableObject.CreateInstance<VSGraphModel>(), null);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(s_GraphBasedHandle.GraphModelReference);
        }

        static Mock<ITypeBasedMetadataFactory> GetMockedTypeMetadataFactory()
        {
            Mock<ITypeBasedMetadataFactory> mock = new Mock<ITypeBasedMetadataFactory>();
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th == k_TypeBasedHandle))).Returns(k_TypeBasedMetadata);
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th != k_TypeBasedHandle))).Throws<ArgumentException>();
            mock.Setup(factory => factory.CanProcessHandle(It.Is<TypeHandle>(th => th == k_TypeBasedHandle))).Returns(true);
            mock.Setup(factory => factory.CanProcessHandle(It.Is<TypeHandle>(th => th != k_TypeBasedHandle))).Returns(false);
            return mock;
        }

        static Mock<IGraphBasedMetadataFactory> GetMockedGraphMetadataFactory()
        {
            Mock<IGraphBasedMetadataFactory> mock = new Mock<IGraphBasedMetadataFactory>();
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th == s_GraphBasedHandle))).Returns(k_GraphBasedMetadata);
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th != s_GraphBasedHandle))).Throws<ArgumentException>();
            mock.Setup(factory => factory.CanProcessHandle(It.Is<TypeHandle>(th => th == s_GraphBasedHandle))).Returns(true);
            mock.Setup(factory => factory.CanProcessHandle(It.Is<TypeHandle>(th => th != s_GraphBasedHandle))).Returns(false);
            return mock;
        }

        [Test]
        public void Should_CreateTypeMetadata_WhenTypeBasedTypeHandle()
        {
            //Arrange
            var typeMetadataFactoryMock = GetMockedTypeMetadataFactory();
            var graphMetadataFactoryMock = GetMockedGraphMetadataFactory();
            var factory = new TypeHandleMetadataFactory(typeMetadataFactoryMock.Object, graphMetadataFactoryMock.Object);

            //Act
            var typeBasedMetadata = factory.Create(k_TypeBasedHandle);

            //Assert
            typeMetadataFactoryMock.Verify(f => f.Create(It.IsAny<TypeHandle>()), Times.Exactly(1));
            Assert.That(typeBasedMetadata, Is.SameAs(k_TypeBasedMetadata));
        }

        [Test]
        public void Should_CreateGraphMetadata_WhenGraphBasedTypeHandle()
        {
            //Arrange
            var typeMetadataFactoryMock = GetMockedTypeMetadataFactory();
            var graphMetadataFactoryMock = GetMockedGraphMetadataFactory();
            var factory = new TypeHandleMetadataFactory(typeMetadataFactoryMock.Object, graphMetadataFactoryMock.Object);

            //Act
            var graphBasedMetadata = factory.Create(s_GraphBasedHandle);

            //Assert
            Assert.That(graphBasedMetadata, Is.SameAs(k_GraphBasedMetadata));
            graphMetadataFactoryMock.Verify(f => f.Create(It.IsAny<TypeHandle>()), Times.Exactly(1));
        }


        [Test]
        public void Should_CreateEmptyMetadata_WhenEmptyTypeHandle()
        {
            //Arrange
            var typeMetadataFactoryMock = GetMockedTypeMetadataFactory();
            var graphMetadataFactoryMock = GetMockedGraphMetadataFactory();
            var factory = new TypeHandleMetadataFactory(typeMetadataFactoryMock.Object, graphMetadataFactoryMock.Object);

            //Act
            var emptyMetadata = factory.Create(default(TypeHandle));

            //Assert
            graphMetadataFactoryMock.Verify(f => f.Create(It.IsAny<TypeHandle>()), Times.Exactly(0));
            typeMetadataFactoryMock.Verify(f => f.Create(It.IsAny<TypeHandle>()), Times.Exactly(0));
            Assert.That(emptyMetadata, Is.SameAs(EmptyTypeMetadata.Instance));
        }
    }
}
