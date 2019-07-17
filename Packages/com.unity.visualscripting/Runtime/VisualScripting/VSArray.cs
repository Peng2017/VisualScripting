using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEngine.VisualScripting
{
    [Node, VisualScriptingFriendlyName("Array")]
    [Serializable]
    [PublicAPI]
    public class VSArray<T> : IList<T>, IVSArray
    {
        [Hidden]
        public List<T> elements;

        [Hidden]
        const int k_DefaultSeed = 48537227;

        [VisualScriptingFriendlyName("GetElement")]
        public T this[int index]
        {
            get => elements[index];
            set => elements[index] = value;
        }

        [Hidden]
        public VSArray(int capacity = 0)
        {
            elements = new List<T>(capacity);
        }

        [Hidden]
        public VSArray(IEnumerable<T> collection)
        {
            elements = new List<T>(collection);
        }

        [Hidden]
        public static implicit operator List<T>(VSArray<T> list)
        {
            return list.elements;
        }

        [Hidden]
        public static implicit operator VSArray<T>(List<T> list)
        {
            return new VSArray<T> { elements = list };
        }

        [Hidden]
        public static implicit operator T[](VSArray<T> list)
        {
            return list.elements.ToArray();
        }

        [Hidden]
        public static implicit operator VSArray<T>(T[] array)
        {
            return new VSArray<T> { elements = array.ToList() };
        }

        [Hidden]
        public IEnumerator<T> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        [Hidden]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        //<HACK>
        // TODO: Temporary until array initialization is implemented.
        [VisualScriptingFriendlyName("Initialize Array")]
        public void Initialize(int capacity)
        {
            elements = new List<T>(capacity);
        }

        //</HACK>

        [Hidden]
        public T GetElement(int index)
        {
            return elements[index];
        }

        public void SetElement(int index, T element)
        {
            elements[index] = element;
        }

        public int GetElementCount()
        {
            return elements.Count;
        }

        public void AddElement(T element)
        {
            elements.Add(element);
        }

        public void InsertElement(int index, T element)
        {
            elements.Insert(index, element);
        }

        public void RemoveElement(T element)
        {
            elements.Remove(element);
        }

        public void RemoveElementAt(int index)
        {
            elements.RemoveAt(index);
        }

        public void RemoveElements(VSArray<T> toRemove)
        {
            foreach (var element in toRemove)
            {
                RemoveElement(element);
            }
        }

        public void SwapElements(int firstIndex, int secondIndex)
        {
            var temp = elements[firstIndex];
            elements[firstIndex] = elements[secondIndex];
            elements[secondIndex] = temp;
        }

        public void CullElements(VSArray<bool> cull)
        {
            var newList = new List<T>();

            for (var i = 0; i < elements.Count; i++)
            {
                if (!cull[i % cull.GetElementCount()])
                {
                    newList.Add(elements[i]);
                }
            }

            elements = newList;
        }

        [VisualScriptingFriendlyName("Copy")]
        public VSArray<T> GetCopy()
        {
            var newArray = new VSArray<T> { elements = { Capacity = GetElementCount() } };
            foreach (var element in elements)
            {
                newArray.AddElement(element);
            }

            return newArray;
        }

        public void RemoveDuplicateElements()
        {
            elements = elements.Distinct().ToList();
        }

        [VisualScriptingFriendlyName("RemoveDuplicateElements")]
        public VSArray<T> GetRemoveDuplicateElements()
        {
            var newArray = GetCopy();
            newArray.RemoveDuplicateElements();
            return newArray;
        }

        public void ReverseElements()
        {
            elements.Reverse();
        }

        [VisualScriptingFriendlyName("ReverseElements")]
        public VSArray<T> GetReverseElements()
        {
            var newArray = GetCopy();
            newArray.ReverseElements();
            return newArray;
        }

        public void SortElements()
        {
            elements.Sort();
        }

        [VisualScriptingFriendlyName("SortElements")]
        public VSArray<T> GetSortElements()
        {
            var newArray = GetCopy();
            newArray.SortElements();
            return newArray;
        }

        public void ShuffleElements(int seed = k_DefaultSeed)
        {
            Random.InitState(seed);

            // Fisher-Yates shuffle.
            for (var i = 0; i < elements.Count; i++)
            {
                SwapElements(i, Random.Range(i, elements.Count));
            }
        }

        [VisualScriptingFriendlyName("ShuffleElements")]
        public VSArray<T> GetShuffleElements(int seed = k_DefaultSeed)
        {
            var newArray = GetCopy();
            newArray.ShuffleElements(seed);
            return newArray;
        }

        public void ShiftElements(int amount = 1)
        {
            if (elements.Count < 1)
                return;

            if (amount < 0)
            {
                for (var j = 0; j < -amount; j++)
                {
                    for (var i = 0; i < elements.Count - 1; i++)
                    {
                        SwapElements(i, (i + 1) % elements.Count);
                    }
                }
            }
            else if (amount > 0)
            {
                for (var j = 0; j < amount; j++)
                {
                    for (var i = elements.Count - 1; i > 0; i--)
                    {
                        SwapElements(i, (i - 1) % elements.Count);
                    }
                }
            }
        }

        [VisualScriptingFriendlyName("ShiftElements")]
        public VSArray<T> GetShiftElements(int amount = 1)
        {
            var newArray = GetCopy();
            newArray.ShiftElements(amount);
            return newArray;
        }

        public void Mix(VSArray<T> other, VSArray<bool> takeOther)
        {
            elements = GetMix(other, takeOther);
        }

        [VisualScriptingFriendlyName("Mix")]
        public VSArray<T> GetMix(VSArray<T> other, VSArray<bool> takeOther)
        {
            var newList = new List<T>();

            var newListCount = Math.Max(elements.Count, other.Count);

            for (var i = 0; i < newListCount; i++)
            {
                newList.Add(takeOther[i % takeOther.GetElementCount()] ? other[i % other.GetElementCount()] : elements[i % elements.Count]);
            }

            return newList;
        }

        [VisualScriptingFriendlyName("RandomPick")]
        public VSArray<T> GetRandomPick(int count = 1, int seed = k_DefaultSeed)
        {
            var newList = new List<T>();

            Random.InitState(seed);

            for (var i = 0; i < count; i++)
            {
                var index = Random.Range(0, elements.Count);
                newList.Add(elements[index]);
            }

            return newList;
        }

        [VisualScriptingFriendlyName("TakeEvery")]
        public VSArray<T> GetTakeEvery(int skip)
        {
            if (skip < 1)
                throw new ArgumentException("Skip value must greater than zero", nameof(skip));

            return elements.Where((item, index) => index % skip == 0).ToList();
        }

        [VisualScriptingFriendlyName("ExcludeFirstElement")]
        public VSArray<T> GetExcludeFirstElement()
        {
            return elements.Skip(1).ToList();
        }

        [VisualScriptingFriendlyName("ExcludeLastElement")]
        public VSArray<T> GetExcludeLastElement()
        {
            return elements.Take(elements.Count - 1).ToList();
        }

        public bool IsEmpty()
        {
            return !elements.Any();
        }

        public T FirstElement()
        {
            return elements.First();
        }

        public T SecondElement()
        {
            return elements[1];
        }

        public T LastElement()
        {
            return elements.Last();
        }

        public T MedianElement()
        {
            return elements[elements.Count / 2];
        }

        public bool ContainsElement(T element)
        {
            return elements.Contains(element);
        }

        public void Clear()
        {
            elements.Clear();
        }

        public int GetIndexOfElement(T element)
        {
            return elements.IndexOf(element);
        }

        public int GetLastIndexOfElement(T element)
        {
            return elements.LastIndexOf(element);
        }

        public void UnionWith(VSArray<T> other)
        {
            elements = elements.Union(other).ToList();
        }

        [VisualScriptingFriendlyName("Union")]
        public VSArray<T> Union(VSArray<T> other)
        {
            return elements.Union(other).ToList();
        }

        public void IntersectWith(VSArray<T> other)
        {
            elements = elements.Intersect(other).ToList();
        }

        [VisualScriptingFriendlyName("Intersection")]
        public VSArray<T> Intersect(VSArray<T> other)
        {
            return elements.Intersect(other).ToList();
        }

        [Hidden]
        public void Add(T item)
        {
            elements.Add(item);
        }

        [Hidden]
        public bool Contains(T item)
        {
            return elements.Contains(item);
        }

        [Hidden]
        public void CopyTo(T[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }

        [Hidden]
        public bool Remove(T item)
        {
            return elements.Remove(item);
        }

        [Hidden]
        public int IndexOf(T item)
        {
            return elements.IndexOf(item);
        }

        [Hidden]
        public void Insert(int index, T item)
        {
            elements.Insert(index, item);
        }

        [Hidden]
        public void RemoveAt(int index)
        {
            elements.RemoveAt(index);
        }

        [Hidden]
        public int Count => elements.Count;

        [Hidden]
        public bool IsReadOnly => false;
    }

    public interface IVSArray
    {
    }

    [Node, VisualScriptingFriendlyName("Array")]
    [PublicAPI]
    public static class VSArrayExtensions
    {
        const int k_DefaultSeed = 48537227;

        public static int Sum(this VSArray<int> array)
        {
            return array.elements.Sum();
        }

        public static float Sum(this VSArray<float> array)
        {
            return array.elements.Sum();
        }

        public static Vector3 Sum(this VSArray<Vector3> array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Vector3 sum = Vector3.zero;

            foreach (var value in array.elements)
            {
                sum += value;
            }

            return sum;
        }

        public static float Average(this VSArray<int> array)
        {
            return array.elements.Average();
        }

        public static float Average(this VSArray<float> array)
        {
            return array.elements.Average();
        }

        public static int Minimum(this VSArray<int> array)
        {
            return array.elements.Min();
        }

        public static float Minimum(this VSArray<float> array)
        {
            return array.elements.Min();
        }

        public static int Maximum(this VSArray<int> array)
        {
            return array.elements.Max();
        }

        public static float Maximum(this VSArray<float> array)
        {
            return array.elements.Max();
        }

        public static float Variance(this VSArray<float> array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var average = array.elements.Average();

            var variance = 0.0f;
            foreach (var value in array.elements)
            {
                variance += Mathf.Pow(value - average, 2.0f);
            }

            return variance / array.GetElementCount();
        }

        public static float StandardDeviation(this VSArray<float> array)
        {
            return Mathf.Sqrt(array.Variance());
        }

        public static bool AnyTrue(this VSArray<bool> array)
        {
            return array.Any(x => x);
        }

        public static bool AllTrue(this VSArray<bool> array)
        {
            return array.All(x => x);
        }

        public static VSArray<float> MakeFibonacci(int elementCount)
        {
            if (elementCount < 2)
                throw new ArgumentException("Element Count must be greater or equal to 2.", nameof(elementCount));

            var fibonacci = new VSArray<float>(elementCount);
            fibonacci.Add(1);
            fibonacci.Add(1);
            for (var i = 2; i < elementCount; i++)
            {
                fibonacci.Add(fibonacci[i - 1] + fibonacci[i - 2]);
            }
            return fibonacci;
        }

        public static VSArray<float> MakeRandomFloat(int elementCount, float minRange = 0, float maxRange = 1, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new VSArray<float>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(minRange, maxRange));
            }
            return array;
        }

        public static VSArray<int> MakeRandomInteger(int elementCount, int minRange = 0, int maxRange = 1, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new VSArray<int>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(minRange, maxRange));
            }
            return array;
        }

        public static VSArray<Vector3> MakeRandomVector(int elementCount, Vector3 minRange, Vector3 maxRange, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new VSArray<Vector3>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(
                    new Vector3(
                        Random.Range(minRange.x, maxRange.x),
                        Random.Range(minRange.y, maxRange.y),
                        Random.Range(minRange.z, maxRange.z)));
            }
            return array;
        }

        public static VSArray<Quaternion> MakeRandomQuaternion(int elementCount, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new VSArray<Quaternion>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.rotationUniform);
            }
            return array;
        }

        public static VSArray<bool> MakeRandomBoolean(int elementCount, float probability = 0.5f, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be in the range [0,1].", nameof(probability));

            Random.InitState(seed);

            var array = new VSArray<bool>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(0.0f, 1.0f) < probability);
            }
            return array;
        }

        public static VSArray<float> MakeStep(int elementCount, float minValue = 0, float step = 1)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            var array = new VSArray<float>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(minValue);
                minValue += step;
            }
            return array;
        }

        public static VSArray<int> MakeStep(int elementCount, int minValue = 0, int step = 1)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            var array = new VSArray<int>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(minValue);
                minValue += step;
            }
            return array;
        }

        public static VSArray<Vector3> MakeNormalized(VSArray<Vector3> array)
        {
            var newArray = new VSArray<Vector3>(array.Count);
            foreach (var element in array)
            {
                newArray.Add(element.normalized);
            }

            return newArray;
        }
    }
}
