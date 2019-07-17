using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    [System.ComponentModel.Category("Arrays")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class VisualScriptingArrayTests
    {
        [TestCase(new[] {0, 1, 2, 3, 4}, 0, 0)]
        [TestCase(new[] {0, 1, 2, 3, 4}, 2, 2)]
        public void GetElementTest(int[] source, int index, int expected)
        {
            var list1 = source.CopyToVsArray();

            var element = list1.GetElement(index);
            Assert.That(element, Is.EqualTo(expected));
        }

        [TestCase(typeof(Transform), false)]
        [TestCase(typeof(GameObject), false)]
        [TestCase(typeof(List<Transform>), true)]
        [TestCase(typeof(int[]), true)]
        public void IsVSArrayCompatibleTest(Type type, bool isVSArrayCompatible)
        {
            Assert.That(type.IsVsArrayCompatible(), Is.EqualTo(isVSArrayCompatible));
        }

        [Test]
        public void GetElementThrowTest()
        {
            var list = new VSArray<int>(new[] {1, 2, 3});
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.GetElement(-1));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.GetElement(8));
        }

        [Test]
        public void SetElementTest()
        {
            var list = new VSArray<int>(new[] {1, 2, 3, 4, 5, 6});

            list.SetElement(0, 10);
            Assert.That(list.GetElement(0), Is.EqualTo(10));

            list.SetElement(3, 10);
            Assert.That(list.GetElement(3), Is.EqualTo(10));
        }

        [Test]
        public void SetElementThrowTest()
        {
            var list = new VSArray<int>(new[] {1, 2, 3});
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.SetElement(-1, 0));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.SetElement(8, 0));
        }

        [Test]
        public void GetElementCountTest()
        {
            var list = new VSArray<int>(new[] { 1, 2, 3, 4, 5, 6 });
            Assert.That(list.GetElementCount(), Is.EqualTo(6));
        }

        [Test]
        public void AddElementTest()
        {
            var list = new VSArray<char>(new[] { 'a', 'b', 'c', 'd', 'e' });
            Assert.That(list.GetElementCount(), Is.EqualTo(5));

            list.AddElement('f');
            Assert.That(list.GetElementCount(), Is.EqualTo(6));
            Assert.That(list.GetElement(5), Is.EqualTo('f'));
        }

        [Test]
        public void RemoveElementTest()
        {
            var list = new VSArray<int>(new[] { 0, 1, 2, 3, 0, 0, 4, 5 });
            Assert.That(list.GetElementCount(), Is.EqualTo(8));

            list.RemoveElement(0);
            Assert.That(list.GetElementCount(), Is.EqualTo(7));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, 0, new[] {1, 2, 3, 4})]
        [TestCase(new[] {0, 1, 2, 3, 4}, 2, new[] {0, 1, 3, 4})]
        public void RemoveElementAtTest(int[] source, int index, int[] expected)
        {
            var list = new VSArray<int>(source);
            Assert.That(list.GetElementCount(), Is.EqualTo(source.Length));

            list.RemoveElementAt(index);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [Test]
        public void RemoveElementsTest()
        {
            var list = new VSArray<int>(new[] { 0, 1, 2, 3, 0, 0, 4, 5 });
            Assert.That(list.GetElementCount(), Is.EqualTo(8));

            list.RemoveElements(new VSArray<int>(new[] {1, 3}));
            Assert.That(list.GetElementCount(), Is.EqualTo(6));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, 0, 1, new[] {1, 0, 2, 3, 4})]
        [TestCase(new[] {0, 1, 2, 3, 4}, 2, 0, new[] {2, 1, 0, 3, 4})]
        [TestCase(new[] {0, 1, 2, 3, 4}, 3, 4, new[] {0, 1, 2, 4, 3})]
        public void SwapElementsTest(int[] source, int index0, int index1, int[] expected)
        {
            var list = new VSArray<int>(source);
            list.SwapElements(index0, index1);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [TestCase(new[] { 0, 1, 2, 3, 4 }, new[] { true, true, false, false, true }, new[] { 2, 3 })]
        [TestCase(new[] { 0, 1, 2, 3, 4 }, new[] { true, false }, new[] { 1, 3 })]
        public void CullElements(int[] source, bool[] cull, int[] expected)
        {
            var list = new VSArray<int>(source);
            list.CullElements(cull.ToVSArray());
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [Test]
        public void GetCopyTest()
        {
            var list = new VSArray<int>(new[] { 0, 1, 2, 3, 0, 0, 4, 5 });
            var list2 = list.GetCopy();
            Assert.That(list, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 0, 1, 0, 1, 2, 2, 3, 2, 3, 1, 0}, new[] {0, 1, 2, 3})]
        [TestCase(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3})]
        public void RemoveDuplicateElementsTest(int[] source, int[] expected)
        {
            var list1 = new VSArray<int>(source);

            // Create new list, don't modify original
            var list2 = list1.GetRemoveDuplicateElements();
            Assert.That(list1, Is.EqualTo(source.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            list1.RemoveDuplicateElements();
            Assert.That(list1, Is.EqualTo(expected.ToList()));
            Assert.That(list1, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, new[] {4, 3, 2, 1, 0})]
        [TestCase(new[] {0}, new[] {0})]
        public void ReverseElementsTest(int[] source, int[] expected)
        {
            var list1 = new VSArray<int>(source);

            // Create new list, don't modify original
            var list2 = list1.GetReverseElements();
            Assert.That(list1, Is.EqualTo(source.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            list1.ReverseElements();
            Assert.That(list1, Is.EqualTo(expected.ToList()));
            Assert.That(list1, Is.EqualTo(list2));
        }

        [TestCase(new[] { 4, 2, 0, 1, 3 }, new[] { 0, 1, 2, 3, 4 })]
        [TestCase(new[] {0}, new[] {0})]
        public void SortElementsTest(int[] source, int[] expected)
        {
            var list1 = new VSArray<int>(source);

            // Create new list, don't modify original
            var list2 = list1.GetSortElements();
            Assert.That(list1, Is.EqualTo(source.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            list1.SortElements();
            Assert.That(list1, Is.EqualTo(expected.ToList()));
            Assert.That(list1, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3, 4, 5, 6, 7}, 3412, new[] {1, 2, 0, 4, 7, 3, 5, 6})]
        [TestCase(new[] {0, 1, 2, 3, 4, 5, 6, 7}, 9876, new[] {2, 5, 3, 7, 0, 1, 6, 4})]
        public void ShuffleTest(int[] source, int seed, int[] expected)
        {
            var list1 = new VSArray<int>(source);

            // Create new list, don't modify original
            var list2 = list1.GetShuffleElements(seed);
            Assert.That(list1, Is.EqualTo(source.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            list1.ShuffleElements(seed);
            Assert.That(list1, Is.EqualTo(expected.ToList()));
            Assert.That(list1, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, -1, new[] {1, 2, 3, 4, 0})]
        [TestCase(new[] {0, 1, 2, 3, 4},  1, new[] {4, 0, 1, 2, 3})]
        [TestCase(new[] {0, 1, 2, 3, 4}, -2, new[] {2, 3, 4, 0, 1})]
        [TestCase(new[] {0, 1, 2, 3, 4},  2, new[] {3, 4, 0, 1, 2})]
        [TestCase(new[] {0}, 2, new[] {0})]
        [TestCase(new[] {0, 1}, 1, new[] {1, 0})]
        [TestCase(new[] {0, 1}, 2, new[] {0, 1})]
        [TestCase(new[] {0, 1}, 3, new[] {1, 0})]
        [TestCase(new[] {0, 1}, -3, new[] {1, 0})]
        public void ShiftElementsTest(int[] source, int amount, int[] expected)
        {
            var list1 = new VSArray<int>(source);

            // Create new list, don't modify original
            var list2 = list1.GetShiftElements(amount);
            Assert.That(list1, Is.EqualTo(source.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            list1.ShiftElements(amount);
            Assert.That(list1, Is.EqualTo(expected.ToList()));
            Assert.That(list1, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3}, new[] {4, 5, 6, 7}, new[] {false, true, false, true}, new[] {0, 5, 2, 7})]
        [TestCase(new[] {0, 1, 2}, new[] {4, 5, 6, 7, 8, 9}, new[] {false, true, true}, new[] {0, 5, 6, 0, 8, 9})]
        public void MixTest(int[] a, int[] b, bool[] c, int[] expected)
        {
            var listA = new VSArray<int>(a);
            var listB = new VSArray<int>(b);
            var indices = new VSArray<bool>(c);

            // Create new list, don't modify original
            var list2 = listA.GetMix(listB, indices);
            Assert.That(listA, Is.EqualTo(a.ToVSArray()));
            Assert.That(listB, Is.EqualTo(b.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            listA.Mix(listB, indices);
            Assert.That(listA, Is.EqualTo(expected.ToVSArray()));
            Assert.That(listA, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3, 4, 5, 6, 7}, 3412, 2, new[] {1, 4})]
        [TestCase(new[] {0, 1, 2, 3, 4}, 4567, 10, new[] {1, 4, 0, 3, 0, 1, 3, 3, 1, 2})]
        public void RandomPickTest(int[] source, int seed, int count, int[] expected)
        {
            var list = new VSArray<int>(source).GetRandomPick(count, seed);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [TestCase(new[] {0, 1, 2, 3, 4, 5, 6, 7}, 2, new[] {0, 2, 4, 6})]
        [TestCase(new[] {0, 1, 2, 3, 4, 5, 6, 7}, 3, new[] {0, 3, 6})]
        public void TakeEveryTest(int[] source, int skip, int[] expected)
        {
            var list = new VSArray<int>(source).GetTakeEvery(skip);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [Test]
        public void TakeEveryThrowTest()
        {
            var list = new VSArray<int>();
            Assert.Throws(typeof(ArgumentException), () => list.GetTakeEvery(0));
            Assert.Throws(typeof(ArgumentException), () => list.GetTakeEvery(-1));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, new[] {1, 2, 3, 4})]
        [TestCase(new[] {0}, new int[] {})]
        public void ExcludeFirstElementTest(int[] source, int[] expected)
        {
            var list = new VSArray<int>(source).GetExcludeFirstElement();
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3})]
        [TestCase(new[] {0}, new int[] {})]
        public void ExcludeLastElementTest(int[] source, int[] expected)
        {
            var list = new VSArray<int>(source).GetExcludeLastElement();
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [Test]
        public void IsEmptyTest()
        {
            var listA = new VSArray<int>(new[] {0, 1, 2, 3, 4});
            Assert.That(listA.IsEmpty(), Is.EqualTo(false));

            var listB = new VSArray<int>();
            Assert.That(listB.IsEmpty(), Is.EqualTo(true));
        }

        [Test]
        public void FirstElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4});
            Assert.That(list.FirstElement(), Is.EqualTo(0));
        }

        [Test]
        public void FirstElementTestThrow()
        {
            var list = new VSArray<int>();
            Assert.Throws(typeof(InvalidOperationException), () => list.FirstElement());
        }

        [Test]
        public void SecondElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4});
            Assert.That(list.SecondElement(), Is.EqualTo(1));
        }

        [Test]
        public void SecondElementTestThrow()
        {
            var list = new VSArray<int>();
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.SecondElement());
            list.AddElement(2);
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.SecondElement());
        }

        [Test]
        public void LastElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4});
            Assert.That(list.LastElement(), Is.EqualTo(4));
        }

        [Test]
        public void LastElementTestThrow()
        {
            var list = new VSArray<int>();
            Assert.Throws(typeof(InvalidOperationException), () => list.LastElement());
        }

        [Test]
        public void MedianElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4, 5, 6});
            Assert.That(list.MedianElement(), Is.EqualTo(3));
        }

        [Test]
        public void MedianElementTestThrow()
        {
            var list = new VSArray<int>();
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => list.MedianElement());
        }

        [Test]
        public void ContainElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4, 5, 6});
            Assert.That(list.ContainsElement(3), Is.EqualTo(true));
            Assert.That(list.ContainsElement(7), Is.EqualTo(false));
        }

        [Test]
        public void ClearTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4, 5, 6});
            Assert.That(list.GetElementCount(), Is.EqualTo(7));
            list.Clear();
            Assert.That(list.GetElementCount(), Is.EqualTo(0));
        }

        [Test]
        public void GetIndexOfElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4, 3, 6});
            Assert.That(list.GetIndexOfElement(3), Is.EqualTo(3));
            Assert.That(list.GetIndexOfElement(4), Is.EqualTo(4));
        }

        [Test]
        public void GetLastIndexOfElementTest()
        {
            var list = new VSArray<int>(new[] {0, 1, 2, 3, 4, 3, 6});
            Assert.That(list.GetLastIndexOfElement(3), Is.EqualTo(5));
            Assert.That(list.GetLastIndexOfElement(4), Is.EqualTo(4));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 5}, new[] {0, 1 , 2, 3, 4, 5})]
        [TestCase(new[] {0, 1, 2}, new int[] {}, new[] {0, 1, 2})]
        public void UnionTest(int[] a, int[] b, int[] expected)
        {
            var listA = new VSArray<int>(a);
            var listB = new VSArray<int>(b);

            // Create new list, don't modify original
            var list2 = listA.Union(listB);

            Assert.That(listA, Is.EqualTo(a.ToVSArray()));
            Assert.That(listB, Is.EqualTo(b.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            listA.UnionWith(listB);
            Assert.That(listA, Is.EqualTo(expected.ToVSArray()));
            Assert.That(listA, Is.EqualTo(list2));
        }

        [TestCase(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 5}, new[] {0, 1 , 2, 3})]
        [TestCase(new[] {0, 1, 2}, new int[] {}, new int[] {})]
        public void IntersectionTest(int[] a, int[] b, int[] expected)
        {
            var listA = new VSArray<int>(a);
            var listB = new VSArray<int>(b);

            // Create new list, don't modify original
            var list2 = listA.Intersect(listB);

            Assert.That(listA, Is.EqualTo(a.ToVSArray()));
            Assert.That(listB, Is.EqualTo(b.ToVSArray()));
            Assert.That(list2, Is.EqualTo(expected.ToVSArray()));

            // Modify original (in-place)
            listA.IntersectWith(listB);
            Assert.That(listA, Is.EqualTo(expected.ToVSArray()));
            Assert.That(listA, Is.EqualTo(list2));
        }

        [Test]
        public void SumTestF()
        {
            var list = new VSArray<float>(new[] {1.0f, 0.0f, 4.0f, 3.0f, 2.0f});
            var value = list.Sum();
            Assert.That(value, Is.EqualTo(10.0f));
        }

        [Test]
        public void SumTestV()
        {
            var list = new VSArray<Vector3>();
            list.Add(new Vector3(0, 0, 0));
            list.Add(new Vector3(1, 2, 3));
            list.Add(new Vector3(2, 4, 6));
            list.Add(new Vector3(3, 6, 9));
            list.Add(new Vector3(4, 8, 12));
            var value = list.Sum();
            Assert.That(value, Is.EqualTo(new Vector3(10, 20, 30)));
        }

        [Test]
        public void AverageTest()
        {
            var list = new VSArray<float>(new[] {1.0f, 0.0f, 4.0f, 3.0f, 2.0f});
            var value = list.Average();
            Assert.That(value, Is.EqualTo(2.0f));
        }

        [Test]
        public void MinimumTestI()
        {
            var list = new VSArray<int>(new[] {1, 0, 4, 3, 2});
            var value = list.Minimum();
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void MinimumTestF()
        {
            var list = new VSArray<float>(new[] {1.0f, 0.0f, 4.0f, 3.0f, 2.0f});
            var value = list.Minimum();
            Assert.That(value, Is.EqualTo(0.0f));
        }

        [Test]
        public void MaximumTestI()
        {
            var list = new VSArray<int>(new[] {1, 0, 4, 3, 2});
            var value = list.Maximum();
            Assert.That(value, Is.EqualTo(4));
        }

        [Test]
        public void MaximumTestF()
        {
            var list = new VSArray<float>(new[] {1.0f, 0.0f, 4.0f, 3.0f, 2.0f});
            var value = list.Maximum();
            Assert.That(value, Is.EqualTo(4.0f));
        }

        [Test]
        public void VarianceTest()
        {
            var list = new VSArray<float>(new float[] {2, 4, 4, 4, 5, 5, 7, 9});
            var value = list.Variance();
            Assert.That(value, Is.EqualTo(4.0f));
        }

        [Test]
        public void StandardDeviationTest()
        {
            var list = new VSArray<float>(new float[] {2, 4, 4, 4, 5, 5, 7, 9});
            var value = list.StandardDeviation();
            Assert.That(value, Is.EqualTo(2.0f));
        }

        [TestCase(new[] {true, true, false}, true)]
        [TestCase(new[] {true, true, true}, true)]
        [TestCase(new[] {false, false, false}, false)]
        public void AnyTrueTest(bool[] source, bool expected)
        {
            var list = new VSArray<bool>(source);
            var value = list.AnyTrue();
            Assert.That(value, Is.EqualTo(expected));
        }

        [TestCase(new[] {true, true, false}, false)]
        [TestCase(new[] {true, true, true}, true)]
        [TestCase(new[] {false, false, false}, false)]
        public void AllTrueTest(bool[] source, bool expected)
        {
            var list = new VSArray<bool>(source);
            var value = list.AllTrue();
            Assert.That(value, Is.EqualTo(expected));
        }

        [TestCase(4, new float[] {1, 1, 2, 3})]
        [TestCase(10, new float[] {1, 1, 2, 3, 5, 8, 13, 21, 34, 55})]
        public void MakeFibonacciTest(int count, float[] expected)
        {
            var list = VSArrayExtensions.MakeFibonacci(count);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [TestCase(4, 0, 1)]
        [TestCase(8, -5, 5)]
        public void MakeRandomIntegerTest(int count, int min, int max)
        {
            var list = VSArrayExtensions.MakeRandomInteger(count, min, max);
            Assert.That(list.Count, Is.EqualTo(count));
            Assert.That(list.Minimum(), Is.GreaterThanOrEqualTo(min));
            Assert.That(list.Maximum(), Is.LessThanOrEqualTo(max));
            // Stability test
            Assert.That(list, Is.EqualTo(VSArrayExtensions.MakeRandomInteger(count, min, max)));
        }

        [TestCase(4, 0.0f, 1.0f)]
        [TestCase(8, -5.0f, 5.0f)]
        public void MakeRandomFloatTest(int count, float min, float max)
        {
            var list = VSArrayExtensions.MakeRandomFloat(count, min, max);
            Assert.That(list.Count, Is.EqualTo(count));
            Assert.That(list.Minimum(), Is.GreaterThanOrEqualTo(min));
            Assert.That(list.Maximum(), Is.LessThanOrEqualTo(max));
            // Stability test
            Assert.That(list, Is.EqualTo(VSArrayExtensions.MakeRandomFloat(count, min, max)));
        }

        [TestCase(50, 0.0f)]
        [TestCase(50, 1.0f)]
        [TestCase(100, 0.5f)]
        [TestCase(100, 0.25f)]
        [TestCase(100, 0.75f)]
        public void MakeRandomBooleanTest(int count, float probability)
        {
            var list = VSArrayExtensions.MakeRandomBoolean(count, probability);
            Assert.That(list.Count, Is.EqualTo(count));

            float sum = list.Sum(x => x ? 1 : 0);

            Assert.That(sum / count, Is.EqualTo(probability).Within(0.1f));

            // Stability test
            Assert.That(list, Is.EqualTo(VSArrayExtensions.MakeRandomBoolean(count, probability)));
        }

        [TestCase(4, -1, -1, -1, 1, 1, 1)]
        [TestCase(8, 2, 4, 8, 10, 12, 14)]
        public void MakeRandomVectorTest(int count, float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            var min = new Vector3(minX, minY, minZ);
            var max = new Vector3(maxX, maxY, maxZ);

            var list = VSArrayExtensions.MakeRandomVector(count, min, max);
            var allX = list.elements.Select(v => v.x).ToList();
            var allY = list.elements.Select(v => v.y).ToList();
            var allZ = list.elements.Select(v => v.z).ToList();

            Assert.That(list.Count, Is.EqualTo(count));
            Assert.That(allX.Min, Is.GreaterThanOrEqualTo(min.x));
            Assert.That(allX.Max, Is.LessThanOrEqualTo(max.x));
            Assert.That(allY.Min, Is.GreaterThanOrEqualTo(min.y));
            Assert.That(allY.Max, Is.LessThanOrEqualTo(max.y));
            Assert.That(allZ.Min, Is.GreaterThanOrEqualTo(min.z));
            Assert.That(allZ.Max, Is.LessThanOrEqualTo(max.z));
            // Stability test
            Assert.That(list, Is.EqualTo(VSArrayExtensions.MakeRandomVector(count, min, max)));
        }

        [Test]
        public void MakeRandomQuaternionTest()
        {
            var list = VSArrayExtensions.MakeRandomQuaternion(100);
            Assert.That(list.Count, Is.EqualTo(100));

            float sumX = list.Sum(x => x.x);
            float sumY = list.Sum(x => x.y);
            float sumZ = list.Sum(x => x.z);

            Assert.That(sumX / 100, Is.EqualTo(0).Within(0.1f));
            Assert.That(sumY / 100, Is.EqualTo(0).Within(0.1f));
            Assert.That(sumZ / 100, Is.EqualTo(0).Within(0.1f));

            // Stability test
            Assert.That(list, Is.EqualTo(VSArrayExtensions.MakeRandomQuaternion(100)));
        }

        [TestCase(4, 0, 1, new[] {0, 1, 2, 3})]
        [TestCase(5, 10, 3, new[] {10, 13, 16, 19, 22})]
        public void MakeStepTestI(int count, int init, int step, int[] expected)
        {
            var list = VSArrayExtensions.MakeStep(count, init, step);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [TestCase(4, 0, 1, new float[] {0, 1, 2, 3})]
        [TestCase(5, 10, 3, new float[] {10, 13, 16, 19, 22})]
        public void MakeStepTestF(int count, float init, float step, float[] expected)
        {
            var list = VSArrayExtensions.MakeStep(count, init, step);
            Assert.That(list, Is.EqualTo(expected.ToVSArray()));
        }

        [Test]
        public void MakeNormalizedTest()
        {
            var list = VSArrayExtensions.MakeNormalized(VSArrayExtensions.MakeRandomVector(10, -Vector3.one * 100, Vector3.one * 100));
            Assert.That(list.Count, Is.EqualTo(10));
            foreach (var element in list.elements)
            {
                Assert.That(element.magnitude, Is.EqualTo(1.0f).Within(0.001f));
            }
        }

        static void DebugLog<T>(VSArray<T> list)
        {
            for (int i = 0; i < list.GetElementCount(); i++)
                Debug.Log("[" + i + "]: " + list[i]);
        }

        static void DebugLog<T>(IList<T> list, IReadOnlyList<T> expected)
        {
            for (int i = 0; i < list.Count; i++)
                Debug.Log("[" + i + "]: " + expected[i] + " => " + list[i]);
        }
    }
}
