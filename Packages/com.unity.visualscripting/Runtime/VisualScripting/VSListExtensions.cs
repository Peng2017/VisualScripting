using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEngine.VisualScripting
{
    [PublicAPI]
    public static class VSListExtensions
    {
        const int k_DefaultSeed = 48537227;

        public static T GetElement<T>(this List<T> list, int index)
        {
            return list[index];
        }

        public static void SetElement<T>(this List<T> list, int index, T element)
        {
            list[index] = element;
        }

        public static int GetElementCount<T>(this List<T> list)
        {
            return list.Count;
        }

        public static void AddElement<T>(this List<T> list, T element)
        {
            list.Add(element);
        }

        public static void InsertElement<T>(this List<T> list, int index, T element)
        {
            list.Insert(index, element);
        }

        public static void RemoveElement<T>(this List<T> list, T element)
        {
            list.Remove(element);
        }

        public static void RemoveElementAt<T>(this List<T> list, int index)
        {
            list.RemoveAt(index);
        }

        public static void RemoveElements<T>(this List<T> list, List<T> elements)
        {
            foreach (var element in elements)
            {
                list.Remove(element);
            }
        }

        public static void SwapElements<T>(this List<T> list, int firstIndex, int secondIndex)
        {
            var temp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = temp;
        }

        public static void CullElements<T>(this List<T> list, List<bool> cull)
        {
            var writeIndex = 0;
            for (var readIndex = 0; readIndex < list.Count; readIndex++)
            {
                if (!cull[readIndex % cull.GetElementCount()])
                    list[writeIndex++] = list[readIndex];
            }

            list.RemoveRange(writeIndex,list.Count-writeIndex);
        }

        public static List<T> GetCopy<T>(this List<T> list)
        {
            return list.ToList();
        }

        public static void RemoveDuplicateElements<T>(this List<T> list)
        {
            if (list.Count < 2)
                return;

            list.Sort();
            int writeIndex = 0;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int readIndex = 1; readIndex < list.Count; readIndex++)
            {
                if (comparer.Equals(list[readIndex], list[writeIndex]))
                    continue;

                writeIndex++;
                list[writeIndex] = list[readIndex];
            }
            writeIndex++;
            list.RemoveRange(writeIndex,list.Count-writeIndex);
        }

        public static List<T> GetRemoveDuplicateElements<T>(this List<T> list)
        {
            var newArray = GetCopy(list);
            newArray.RemoveDuplicateElements();
            return newArray;
        }

        public static void ReverseElements<T>(this List<T> list)
        {
            list.Reverse();
        }

        public static List<T> GetReverseElements<T>(this List<T> list)
        {
            var newList = list.ToList();
            newList.Reverse();
            return newList;
        }

        public static void SortElements<T>(this List<T> list)
        {
            list.Sort();
        }

        public static List<T> GetSortElements<T>(this List<T> list)
        {
            var newArray = list.ToList();
            newArray.SortElements();
            return newArray;
        }

        public static void ShuffleElements<T>(this List<T> list, int seed = k_DefaultSeed)
        {
            Random.InitState(seed);

            // Fisher-Yates shuffle.
            for (var i = 0; i < list.Count; i++)
            {
                SwapElements(list, i, Random.Range(i, list.Count));
            }
        }

        public static List<T> GetShuffleElements<T>(this List<T> list, int seed = k_DefaultSeed)
        {
            var newArray = list.ToList();
            newArray.ShuffleElements(seed);
            return newArray;
        }

        public static void ShiftElements<T>(this List<T> list, int amount = 1)
        {
            if (list.Count < 1)
                return;

            if (amount < 0)
            {
                for (var j = 0; j < -amount; j++)
                {
                    for (var i = 0; i < list.Count - 1; i++)
                    {
                        SwapElements(list, i, (i + 1) % list.Count);
                    }
                }
            }
            else if (amount > 0)
            {
                for (var j = 0; j < amount; j++)
                {
                    for (var i = list.Count - 1; i > 0; i--)
                    {
                        SwapElements(list, i, (i - 1) % list.Count);
                    }
                }
            }
        }

        public static List<T> GetShiftElements<T>(this List<T> list, int amount = 1)
        {
            List<T> newArray = list.ToList();
            newArray.ShiftElements(amount);
            return newArray;
        }

        public static void Mix<T>(this List<T> list, List<T> other, List<bool> takeOther)
        {
            List<T> newList = GetMix(list, other, takeOther);

            list.Clear();
            for (int i = 0; i < newList.Count; i++)
                list.Add(newList[i]);
        }

        public static List<T> GetMix<T>(this List<T> list, List<T> other, List<bool> takeOther)
        {
            var newList = new List<T>();

            int newListCount = Math.Max(list.Count, other.Count);

            for (var i = 0; i < newListCount; i++)
            {
                if (takeOther[i % takeOther.GetElementCount()])
                    newList.Add(other[i % other.GetElementCount()]);
                else
                    newList.Add(list[i % list.Count]);
            }
            return newList;
        }

        public static List<T> GetRandomPick<T>(this List<T> list, int count = 1, int seed = k_DefaultSeed)
        {
            var newList = new List<T>();

            Random.InitState(seed);

            for (var i = 0; i < count; i++)
            {
                var index = Random.Range(0, list.Count);
                newList.Add(list[index]);
            }

            return newList;
        }

        public static List<T> GetTakeEvery<T>(this List<T> list, int skip)
        {
            if (skip < 1)
                throw new ArgumentException("Skip value must greater than zero", nameof(skip));

            return list.Where((item, index) => index % skip == 0).ToList();
        }

        public static List<T> GetExcludeFirstElement<T>(this List<T> list)
        {
            return list.Skip(1).ToList();
        }

        public static List<T> GetExcludeLastElement<T>(this List<T> list)
        {
            return list.Take(list.Count - 1).ToList();
        }

        public static bool IsEmpty<T>(this List<T> list)
        {
            return list.Count==0;
        }

        public static T FirstElement<T>(this List<T> list)
        {
            return list.First();
        }

        public static T SecondElement<T>(this List<T> list)
        {
            return list[1];
        }

        public static T LastElement<T>(this List<T> list)
        {
            return list.Last();
        }

        public static T MedianElement<T>(this List<T> list)
        {
            return list[list.Count / 2];
        }

        public static bool ContainsElement<T>(this List<T> list, T element)
        {
            return list.Contains(element);
        }

        public static void Clear<T>(this List<T> list)
        {
            list.Clear();
        }

        public static int GetIndexOfElement<T>(this List<T> list, T element)
        {
            return list.IndexOf(element);
        }

        public static int GetLastIndexOfElement<T>(this List<T> list, T element)
        {
            return list.LastIndexOf(element);
        }

        public static void UnionWith<T>(this List<T> list, List<T> other)
        {
            foreach (var i in other)
            {
                //TODO Complexity of O(n*m) - While not urgent this is slow
                if(!list.Contains(i))
                    list.Add(i);
            }
        }

        public static void IntersectWith<T>(this List<T> list, List<T> other)
        {
            var writeIndex = 0;
            for (var readIndex = 0; readIndex < list.Count; readIndex++)
            {
                var entry = list[readIndex];
                //TODO Complexity of O(n*m) - While not urgent this is slow
                if (other.Contains(entry))
                    list[writeIndex++] = entry;
            }

            list.RemoveRange(writeIndex,list.Count-writeIndex);
        }

        public static int Sum(this List<int> array)
        {
            return Enumerable.Sum(array);
        }

        public static float Sum(this List<float> array)
        {
            return Enumerable.Sum(array);
        }

        public static Vector3 Sum(this List<Vector3> array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Vector3 sum = Vector3.zero;

            foreach (var value in array)
            {
                sum += value;
            }

            return sum;
        }

        public static float Average(this List<int> array)
        {
            return (float)Enumerable.Average(array);
        }

        public static float Average(this List<float> array)
        {
            return Enumerable.Average(array);
        }

        public static int Minimum(this List<int> array)
        {
            return array.Min();
        }

        public static float Minimum(this List<float> array)
        {
            return array.Min();
        }

        public static int Maximum(this List<int> array)
        {
            return array.Max();
        }

        public static float Maximum(this List<float> array)
        {
            return array.Max();
        }

        public static float Variance(this List<float> array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var average = array.Average();

            var variance = 0.0f;
            foreach (var value in array)
            {
                variance += Mathf.Pow(value - average, 2.0f);
            }

            return variance / array.Count;
        }

        public static float StandardDeviation(this List<float> array)
        {
            return Mathf.Sqrt(array.Variance());
        }

        public static bool AnyTrue(this List<bool> array)
        {
            return array.Any(x => x);
        }

        public static bool AllTrue(this List<bool> array)
        {
            return array.All(x => x);
        }

        public static List<float> MakeFibonacci(int elementCount)
        {
            if (elementCount < 2)
                throw new ArgumentException("Element Count must be greater or equal to 2.", nameof(elementCount));

            var fibonacci = new List<float>(elementCount);
            fibonacci.Add(1);
            fibonacci.Add(1);
            for (var i = 2; i < elementCount; i++)
            {
                fibonacci.Add(fibonacci[i - 1] + fibonacci[i - 2]);
            }
            return fibonacci;
        }

        public static List<float> MakeRandomFloat(int elementCount, float minRange = 0, float maxRange = 1, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new List<float>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(minRange, maxRange));
            }
            return array;
        }

        public static List<int> MakeRandomInteger(int elementCount, int minRange = 0, int maxRange = 1, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new List<int>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(minRange, maxRange));
            }
            return array;
        }

        public static List<Vector3> MakeRandomVector(int elementCount, Vector3 minRange, Vector3 maxRange, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new List<Vector3>(elementCount);
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

        public static List<Quaternion> MakeRandomQuaternion(int elementCount, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            Random.InitState(seed);

            var array = new List<Quaternion>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.rotationUniform);
            }
            return array;
        }

        public static List<bool> MakeRandomBoolean(int elementCount, float probability = 0.5f, int seed = k_DefaultSeed)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be in the range [0,1].", nameof(probability));

            Random.InitState(seed);

            var array = new List<bool>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(Random.Range(0.0f, 1.0f) < probability);
            }
            return array;
        }

        public static List<float> MakeStep(int elementCount, float minValue = 0, float step = 1)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            var array = new List<float>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(minValue);
                minValue += step;
            }
            return array;
        }

        public static List<int> MakeStep(int elementCount, int minValue = 0, int step = 1)
        {
            if (elementCount < 1)
                throw new ArgumentException("Element Count must be greater or equal to 1.", nameof(elementCount));

            var array = new List<int>(elementCount);
            for (var i = 0; i < elementCount; i++)
            {
                array.Add(minValue);
                minValue += step;
            }
            return array;
        }

        public static List<Vector3> MakeNormalized(VSArray<Vector3> array)
        {
            var newArray = new List<Vector3>(array.Count);
            foreach (var element in array)
            {
                newArray.Add(element.normalized);
            }

            return newArray;
        }
    }
}
