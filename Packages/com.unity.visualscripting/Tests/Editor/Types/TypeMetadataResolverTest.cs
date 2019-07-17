using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    class TypeMetadataResolverTest
    {
        static readonly TypeHandle k_IntHandle = new TypeHandle(null,"__INT");
        static readonly TypeHandle k_FloatHandle = new TypeHandle(null,"__FLOAT");
        static readonly TypeHandle k_DoubleHandle = new TypeHandle(null,"__DOUBLE");

        static readonly ITypeMetadata k_IntMetadata = new Mock<ITypeMetadata>().Object;
        static readonly ITypeMetadata k_FloatMetadata = new Mock<ITypeMetadata>().Object;
        static readonly ITypeMetadata k_DoubleMetadata = new Mock<ITypeMetadata>().Object;

        static Mock<ITypeMetadataFactory> GetMockedMetadataFactory()
        {
            Mock<ITypeMetadataFactory> mock = new Mock<ITypeMetadataFactory>();
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th == k_IntHandle))).Returns(k_IntMetadata);
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th == k_FloatHandle))).Returns(k_FloatMetadata);
            mock.Setup(factory => factory.Create(It.Is<TypeHandle>(th => th == k_DoubleHandle))).Returns(k_DoubleMetadata);
            return mock;
        }

        [Test]
        public void Should_CreateNewMetadata_OnEveryDifferentTypeHandle()
        {
            //Arrange
            var factoryMock = GetMockedMetadataFactory();
            var resolver = new TypeMetadataResolver(factoryMock.Object);

            //Act
            var intMetadata = resolver.Resolve(k_IntHandle);
            var floatMetadata = resolver.Resolve(k_FloatHandle);
            var doubleMetadata = resolver.Resolve(k_DoubleHandle);

            //Assert
            factoryMock.Verify(factory => factory.Create(It.IsAny<TypeHandle>()), Times.Exactly(3));
            Assert.That(intMetadata, Is.SameAs(k_IntMetadata));
            Assert.That(floatMetadata, Is.SameAs(k_FloatMetadata));
            Assert.That(doubleMetadata, Is.SameAs(k_DoubleMetadata));
        }

        [Test]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void Test_CachingIsUsed_WhenSendingSameTypeHandle()
        {
            //Arrange
            var factoryMock = GetMockedMetadataFactory();
            var resolver = new TypeMetadataResolver(factoryMock.Object);

            //Act
            var intMetadata = resolver.Resolve(k_IntHandle);
                intMetadata = resolver.Resolve(k_IntHandle);
            var floatMetadata = resolver.Resolve(k_FloatHandle);
                floatMetadata = resolver.Resolve(k_FloatHandle);
            var doubleMetadata = resolver.Resolve(k_DoubleHandle);
                doubleMetadata = resolver.Resolve(k_DoubleHandle);

            //Assert
            factoryMock.Verify(factory => factory.Create(It.IsAny<TypeHandle>()), Times.Exactly(3));
            Assert.That(intMetadata, Is.SameAs(k_IntMetadata));
            Assert.That(floatMetadata, Is.SameAs(k_FloatMetadata));
            Assert.That(doubleMetadata, Is.SameAs(k_DoubleMetadata));
        }
    }
}
