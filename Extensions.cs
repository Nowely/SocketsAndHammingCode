using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAHC
{
    public static class Extensions
    {
        /// <summary>
        /// Splits an array into several smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<List<T>> Split<T>(this T[] array, int size)
        {
            for (var i = 0; i < (float) array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size).ToList();
            }
        }

        /// <summary>
        /// Splits an <see cref="BitArray"/> into several smaller bool arrays.
        /// </summary>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static List<bool>[] Split(this BitArray array, int size)
        {
            var temp = new bool[array.Length];
            array.CopyTo(temp, 0);
            return temp.Split(size).ToArray();
        }
    }
}