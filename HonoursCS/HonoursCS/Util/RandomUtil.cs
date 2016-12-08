using System;
using System.Collections.Generic;

namespace HonoursCS.Util
{
    public static class RandomUtil
    {
        /// <summary>
        /// Chooses a random element of the list, using the specified Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public static T Choose<T>(List<T> items, Random random)
        {
            // NOTE(zac): It appears that if I use items.Count as the number,
            // items.Count is a possible number, resulting in a null pointer expception.
            int index = random.Next(items.Count - 1);
            return items[index];
        }

        /// <summary>
        /// Choose and return an element of the list, removing it from the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public static T ChooseRemove<T>(List<T> items, Random random)
        {
            int index = random.Next(items.Count - 1);
            T item = items[index];
            items.RemoveAt(index);
            return item;
        }
    }
}