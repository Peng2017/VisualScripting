using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    class CacheTypeMetadataTest
    {
        const string k_Name = "Name";
        const string k_FriendlyName = "FriendlyName";
        static readonly TypeHandle k_Handle = new TypeHandle(null, "__TYPEHANDLE");
        static readonly List<TypeHandle> k_GenericArguments = new List<TypeHandle>{k_Handle,k_Handle,k_Handle};
        static readonly List<MemberInfoValue> k_PublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_Handle, k_Handle, "publicMember", MemberTypes.Field)
        };
        static readonly List<MemberInfoValue> k_NonPublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_Handle, k_Handle, "privateMember", MemberTypes.Field)
        };

        Mock<ITypeMetadata> m_MockMetadata;
        ITypeMetadata m_MockedMetadata;

        [SetUp]
        public void SetUp()
        {
            m_MockedMetadata = CreateMetadataMock().Object;
        }

        Mock<ITypeMetadata> CreateMetadataMock()
        {
            m_MockMetadata = new Mock<ITypeMetadata>();
            m_MockMetadata.SetupGet(meta => meta.TypeHandle).Returns(k_Handle);
            m_MockMetadata.SetupGet(meta => meta.Name).Returns(k_Name);
            m_MockMetadata.SetupGet(meta => meta.FriendlyName).Returns(k_FriendlyName);
            m_MockMetadata.SetupGet(meta => meta.GenericArguments).Returns(k_GenericArguments);
            m_MockMetadata.SetupGet(meta => meta.PublicMembers).Returns(k_PublicMembers);
            m_MockMetadata.SetupGet(meta => meta.NonPublicMembers).Returns(k_NonPublicMembers);

            m_MockMetadata.Setup(meta => meta.IsAssignableFrom(It.IsAny<ITypeMetadata>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsAssignableFrom(It.IsAny<Type>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsAssignableFrom(It.IsAny<IVSGraphModel>())).Returns(true);

            m_MockMetadata.Setup(meta => meta.IsAssignableTo(It.IsAny<ITypeMetadata>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsAssignableTo(It.IsAny<Type>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsAssignableTo(It.IsAny<IVSGraphModel>())).Returns(true);

            m_MockMetadata.Setup(meta => meta.IsSubclassOf(It.IsAny<ITypeMetadata>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsSubclassOf(It.IsAny<Type>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsSubclassOf(It.IsAny<IVSGraphModel>())).Returns(true);

            m_MockMetadata.Setup(meta => meta.IsSuperclassOf(It.IsAny<ITypeMetadata>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsSuperclassOf(It.IsAny<Type>())).Returns(true);
            m_MockMetadata.Setup(meta => meta.IsSuperclassOf(It.IsAny<IVSGraphModel>())).Returns(true);
            return m_MockMetadata;
        }

        [Test]
        public void Test_TypeHandle_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            TypeHandle handle = cacheMetadata.TypeHandle;
            handle = cacheMetadata.TypeHandle;

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.TypeHandle, Times.Once());
            Assert.That(handle, Is.EqualTo(k_Handle));

        }

        [Test]
        public void Test_Name_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            string typeName = cacheMetadata.Name;
            typeName = cacheMetadata.Name;

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.Name, Times.Once());
            Assert.That(typeName, Is.EqualTo(k_Name));
        }


        [Test]
        public void Test_FriendlyName_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            string friendlyName = cacheMetadata.FriendlyName;
            friendlyName = cacheMetadata.FriendlyName;

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.FriendlyName, Times.Once());
            Assert.That(friendlyName, Is.EqualTo(k_FriendlyName));
        }

        [Test]
        public void Test_GenericArguments_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            List<TypeHandle> genericArguments = cacheMetadata.GenericArguments.ToList();
            genericArguments = cacheMetadata.GenericArguments.ToList();

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.GenericArguments, Times.Once());
            CollectionAssert.AreEqual(genericArguments, k_GenericArguments);
        }

        [Test]
        public void Test_GetPublicMembers_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            List<MemberInfoValue> members = cacheMetadata.PublicMembers;
            members = cacheMetadata.PublicMembers;

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.PublicMembers, Times.Once());
            CollectionAssert.AreEquivalent(members, k_PublicMembers);
        }

        [Test]
        public void Test_GetNonPublicMembers_IsCached()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);

            //Act
            List<MemberInfoValue> members = cacheMetadata.NonPublicMembers;
            members = cacheMetadata.NonPublicMembers;

            //Assert
            m_MockMetadata.VerifyGet(metadata => metadata.NonPublicMembers, Times.Once());
            CollectionAssert.AreEquivalent(members, k_NonPublicMembers);
        }



        [Test]
        public void Test_IsAssignableFrom_NotCached_UsingMetadata()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyMetadata = EmptyTypeMetadata.Instance;

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyMetadata);
            isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyMetadata);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableFrom(It.IsAny<ITypeMetadata>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableFrom_NotCached_UsingType()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyType = typeof(int);

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyType);
            isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyType);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableFrom(It.IsAny<Type>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableFrom_NotCached_UsingGraph()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            IVSGraphModel dummyGraphModel = new Mock<IVSGraphModel>().Object;

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyGraphModel);
            isAssignableFrom = cacheMetadata.IsAssignableFrom(dummyGraphModel);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableFrom(It.IsAny<IVSGraphModel>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }



        [Test]
        public void Test_IsAssignableTo_NotCached_UsingMetadata()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyMetadata = EmptyTypeMetadata.Instance;

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableTo(dummyMetadata);
            isAssignableFrom = cacheMetadata.IsAssignableTo(dummyMetadata);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableTo(It.IsAny<ITypeMetadata>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_NotCached_UsingType()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyType = typeof(int);

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableTo(dummyType);
            isAssignableFrom = cacheMetadata.IsAssignableTo(dummyType);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableTo(It.IsAny<Type>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_NotCached_UsingGraph()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            IVSGraphModel dummyGraphModel = new Mock<IVSGraphModel>().Object;

            //Act
            bool isAssignableFrom = cacheMetadata.IsAssignableTo(dummyGraphModel);
            isAssignableFrom = cacheMetadata.IsAssignableTo(dummyGraphModel);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsAssignableTo(It.IsAny<IVSGraphModel>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }



        [Test]
        public void Test_IsSubclassOf_NotCached_UsingMetadata()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyMetadata = EmptyTypeMetadata.Instance;

            //Act
            bool isAssignableFrom = cacheMetadata.IsSubclassOf(dummyMetadata);
            isAssignableFrom = cacheMetadata.IsSubclassOf(dummyMetadata);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSubclassOf(It.IsAny<ITypeMetadata>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsSubclassOf_NotCached_UsingType()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyType = typeof(int);

            //Act
            bool isAssignableFrom = cacheMetadata.IsSubclassOf(dummyType);
            isAssignableFrom = cacheMetadata.IsSubclassOf(dummyType);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSubclassOf(It.IsAny<Type>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsSubclassOf_NotCached_UsingGraph()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            IVSGraphModel dummyGraphModel = new Mock<IVSGraphModel>().Object;

            //Act
            bool isAssignableFrom = cacheMetadata.IsSubclassOf(dummyGraphModel);
            isAssignableFrom = cacheMetadata.IsSubclassOf(dummyGraphModel);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSubclassOf(It.IsAny<IVSGraphModel>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }



        [Test]
        public void Test_IsSuperclassOf_NotCached_UsingMetadata()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyMetadata = EmptyTypeMetadata.Instance;

            //Act
            bool isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyMetadata);
            isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyMetadata);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSuperclassOf(It.IsAny<ITypeMetadata>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsSuperclassOf_NotCached_UsingType()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            var dummyType = typeof(int);

            //Act
            bool isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyType);
            isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyType);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSuperclassOf(It.IsAny<Type>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsSuperclassOf_NotCached_UsingGraph()
        {
            //Arrange
            var cacheMetadata = new CachedTypeMetadata(m_MockedMetadata);
            IVSGraphModel dummyGraphModel = new Mock<IVSGraphModel>().Object;

            //Act
            bool isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyGraphModel);
            isAssignableFrom = cacheMetadata.IsSuperclassOf(dummyGraphModel);

            //Assert
            m_MockMetadata.Verify(metadata => metadata.IsSuperclassOf(It.IsAny<IVSGraphModel>()), Times.Exactly(2));
            Assert.That(isAssignableFrom, Is.True);
        }

    }
}
