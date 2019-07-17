using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.VisualScripting
{
    public static class EnumerableExtensions
    {
        public static VSArray<T> ToVSArray<T>(this IEnumerable<T> list)
        {
            var enumerable = list.ToList();
            return new VSArray<T>(enumerable.Count) { elements = enumerable.ToList() };
        }

        public static VSArray<T> CopyToVsArray<T>(this IEnumerable<T> list)
            where T : struct
        {
            var enumerable = list as T[] ?? list.ToArray();
            var newArray = new VSArray<T>(enumerable.Length);
            foreach (var element in enumerable)
            {
                newArray.AddElement(element);
            }
            return newArray;
        }
    }
}
