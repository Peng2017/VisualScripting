using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Misc
{
    [TestFixture]
    class TypeExtensionsTests
    {
        [TestCase(typeof(string),      "String")]
        [TestCase(typeof(int[]),       "Integer[]")]
        [TestCase(typeof(int[][]),     "Integer[][]")]
        [TestCase(typeof(float),       "Float")]
        [TestCase(typeof(Vector3),     "Vector 3")]
        [TestCase(typeof(Quaternion),  "Quaternion")]
        [TestCase(typeof(GameObject),  "GameObject")]
        [TestCase(typeof(VSArray<>),   "Array of T")]
        [TestCase(typeof(VSArray<Vector2>),        "Array of Vector 2")]
        [TestCase(typeof(Tuple<int, float>),       "Tuple of Integer and Float")]
        [TestCase(typeof(VSArray<VSArray<float>>), "Array of Array of Float")]
        [TestCase(typeof(VSArray<double[]>),       "Array of Double[]")]
        public void FriendlyNameTest(Type type, string expected)
        {
            Assert.That(type.FriendlyName(), Is.EqualTo(expected));
        }

        [TestCase("Asd Qwe_Asd-rr", "Asd_Qwe_Asd_rr")]
        [TestCase("asd%-$yy", "asd___yy")]
        [TestCase("uu%yy", "uu_yy")]
        [TestCase("asd--qwe_", "asd__qwe_")]
        public void CodifyNameTest(string actual, string expected)
        {
            Assert.That(TypeSystem.CodifyString(actual), Is.EqualTo(expected));
        }

        [TestCase(typeof(byte),    true)]
        [TestCase(typeof(sbyte),   true)]
        [TestCase(typeof(ushort),  true)]
        [TestCase(typeof(uint),    true)]
        [TestCase(typeof(ulong),   true)]
        [TestCase(typeof(short),   true)]
        [TestCase(typeof(int),     true)]
        [TestCase(typeof(long),    true)]
        [TestCase(typeof(decimal), true)]
        [TestCase(typeof(double),  true)]
        [TestCase(typeof(float),   true)]
        [TestCase(typeof(bool),    false)]
        [TestCase(typeof(string),  false)]
        [TestCase(typeof(object),  false)]
        public void IsNumericTest(Type type, bool result)
        {
            Assert.That(type.IsNumeric, Is.EqualTo(result));
        }

        [TestCase(typeof(int), typeof(float),   true)]
        [TestCase(typeof(int), typeof(string),  false)]
        [TestCase(typeof(int), typeof(bool),    false)]
        [TestCase(typeof(int), typeof(Vector2), false)]
        public void HasNumericConversionTest(Type a, Type b, bool result)
        {
            Assert.That(a.HasNumericConversionTo(b), Is.EqualTo(result));
        }
    }
}
