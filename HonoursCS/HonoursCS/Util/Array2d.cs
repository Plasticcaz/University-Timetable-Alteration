using System;

namespace HonoursCS.Util
{
    /// <summary>
    /// A wrapper around a single-array acting like a 2d array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Array2D<T>
    {
        /// <summary>
        /// The actual array. Conceptually a 2d array,
        /// but actually just a 1d array.
        /// </summary>
        private readonly T[] m_data;

        /// <summary>
        /// The Width of the 2d array.
        /// </summary>
        public uint Width { get; private set; }

        /// <summary>
        /// The Height of the 2d array.
        /// </summary>
        public uint Height { get; private set; }

        /// <summary>
        /// Construct a 2D array with the specified width and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Array2D(uint width, uint height)
        {
            Width = width;
            Height = height;
            m_data = new T[Width * Height];
        }

        /// <summary>
        /// Access the element at index (x, y).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public T At(uint x, uint y)
        {
#if DEBUG
            if (x > Width && y > Height)
            {
                throw new InvalidOperationException("Invalid x or y specified.");
            }
#endif
            return m_data[y * Width + x];
        }

        /// <summary>
        /// Put an element at index (x, y).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="item"></param>
        public void SetAt(uint x, uint y, T item)
        {
#if DEBUG
            if (x > Width && y > Height)
            {
                throw new InvalidOperationException("Invalid x or y specified.");
            }
#endif
            m_data[y * Width + x] = item;
        }

        /// <summary>
        /// Gets the whole array as a 1d array.
        /// </summary>
        /// <returns></returns>
        public T[] GetInternalData()
        {
            return m_data;
        }
    }
}