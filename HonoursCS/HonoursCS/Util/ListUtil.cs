using System;
using System.Collections.Generic;

namespace HonoursCS.Util
{
    public static class ListUtil
    {
        /// <summary>
        /// Creates a copy of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> CreateCopy<T>(IList<T> list)
        {
            List<T> other = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                other.Add(item);
            }
            return other;
        }

        /// <summary>
        /// Creates a copy of the list, filtering out as per the provided function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<T> CreateFilteredCopy<T>(IList<T> list, Func<T, bool> filter)
        {
            // We know it's going to be at most as big as the copy.
            List<T> other = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (filter(item))
                {
                    other.Add(item);
                }
            }
            return other;
        }

        /// <summary>
        /// Creates a list of unsigned integers in the range [start..end)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static List<uint> CreateListFromRange(uint start, uint end)
        {
            if (end < start) throw new InvalidOperationException("Tried to create a list from invalid range.");

            List<uint> outList = new List<uint>((int)(end - start));
            for (uint i = start; i < end; i++)
                outList.Add(i);
            return outList;
        }
    }
}