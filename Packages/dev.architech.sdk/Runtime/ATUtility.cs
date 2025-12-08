using System;
using UnityEngine;

// ReSharper disable AssignNullToNotNullAttribute

namespace ArchiTech.SDK
{
    /// <summary>
    /// And Udon# compliant set of utility methods.
    /// </summary>
    public static class ATUtility
    {
        /// <summary>
        /// Simple generic helper method for making a shallow copy of any given array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the shallow copied data</returns>
        public static System.Array CopyArray(System.Array stale, Type type)
        {
            // copy null through
            if (stale == null) return null;
            System.Array fresh = System.Array.CreateInstance(type, stale.Length);
            if (stale.Length > 0) System.Array.Copy(stale, fresh, stale.Length);
            return fresh;
        }

        /// <summary>
        /// Generic helper method for ensuring an array is of a given length.
        /// If the <c>normalizedLength</c> parameter is 0, it will use the length of the <c>stale</c> array instead.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="normalizedLength">the expected length of the array</param>
        /// <param name="type">the type of the individual elements, defaults to the element type of the given array if not provided explicitly</param>
        /// <returns>the new array with the given length</returns>
        public static System.Array NormalizeArray(System.Array stale, int normalizedLength, System.Type type = null)
        {
            if (normalizedLength < 0) normalizedLength = 0;
            // disallow null by making an implicit array
            if (stale == null)
            {
                if (type == null) type = typeof(object);
                stale = System.Array.CreateInstance(type, normalizedLength);
            }
            else type = stale.GetType().GetElementType();

            int staleLength = stale.Length;
            // new array with expected length
            System.Array fresh = System.Array.CreateInstance(type, normalizedLength > 0 ? normalizedLength : staleLength);
            // fresh = System.Array.CreateInstance(type, stale.Length);
            // copy the smaller length size to the new array
            System.Array.Copy(stale, fresh, Math.Min(fresh.Length, staleLength));
            return fresh;
        }

        /// <summary>
        /// Generic helper method that removes the last item from the given array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the last item removed</returns>
        public static System.Array ArrayPop(System.Array stale, System.Type type) => RemoveArrayItem(stale, -1, type);

        /// <summary>
        /// Generic helper method that adds an item to the end of the given array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="insert">the data to insert</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the last item added</returns>
        public static System.Array ArrayPush(System.Array stale, object insert, System.Type type) => AddArrayItem(stale, -1, insert, type);

        /// <summary>
        /// Generic helper method that removes the first item from the given array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the first item removed</returns>
        public static System.Array ArrayShift(System.Array stale, System.Type type) => RemoveArrayItem(stale, 0, type);

        /// <summary>
        /// Generic helper method that adds an item to the start of the given array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="insert">the data to insert</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the first item added</returns>
        public static System.Array ArrayUnshift(System.Array stale, object insert, System.Type type) => AddArrayItem(stale, 0, insert, type);

        /// <summary>
        /// Generic helper method that adds an empty slot to the end of the array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the extra slot appended</returns>
        public static System.Array AddArrayItem(System.Array stale, System.Type type)
        {
            // assumes array was normalized already. Pass through null if given.
            if (stale == null) return null;
            System.Array fresh = System.Array.CreateInstance(type, stale.Length + 1);
            System.Array.Copy(stale, fresh, stale.Length);
            return fresh;
        }

        /// <summary>
        /// Generic helper method that inserts an item into a given slot for the array.
        /// If the index is negative, it will be treated as from the end of the array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="index">the slot in the array to put the data</param>
        /// <param name="insert">the data to insert</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the desired item added</returns>
        public static System.Array AddArrayItem(System.Array stale, int index, object insert, System.Type type)
        {
            // assumes array was normalized already. Pass through null if given.
            if (stale == null) return null;
            int oldLength = stale.Length;
            int newLength = oldLength + 1;
            if (index > oldLength) index = oldLength; // prevent out of bounds issue
            if (index < 0) index = newLength + index; // enable negative indexes
            System.Array fresh = System.Array.CreateInstance(type, newLength);
            // copy from start to the index
            System.Array.Copy(stale, 0, fresh, 0, index);
            // if element was anything but the last element, copy the remainder after the index
            if (index < oldLength) System.Array.Copy(stale, index + 1, fresh, index + 1, oldLength - index);
            if (insert != null) fresh.SetValue(insert, index);
            return fresh;
        }

        /// <summary>
        /// Generic helper method that will move an item from a given index to another.
        /// Method is non-alloc and will return the same array instance as was given originally.
        /// If the indexes are negative, they will be treated as from the end of the array.
        /// </summary>
        /// <param name="arr">the original array</param>
        /// <param name="from">the index of the item to move</param>
        /// <param name="to">the index that the item should be moved to</param>
        /// <returns>the original array after the entries have been moved</returns>
        public static System.Array MoveArrayItem(System.Array arr, int from, int to)
        {
            // assumes array was normalized already. Pass through null if given.
            if (arr == null) return null;
            int len = arr.Length;
            object moving = arr.GetValue(from);
            // enable negative indexes
            if (from < 0) from = len + from;
            if (to < 0) to = len + to;
            // shift element leftward by shifting affected elements to the right
            if (to < from) System.Array.Copy(arr, to, arr, to + 1, from - to);
            // shift element rightward by shifting affected elements to the left
            else System.Array.Copy(arr, from + 1, arr, from, to - from);
            arr.SetValue(moving, to);
            return arr;
        }

        /// <summary>
        /// Generic helper method that will deletes an item from the given slot in the array.
        /// If the index is negative, it will be treated as from the end of the array.
        /// </summary>
        /// <param name="stale">the original array</param>
        /// <param name="index">the slot in the array to remove</param>
        /// <param name="type">the explicit element type of the new array</param>
        /// <returns>a new array with the desired index removed</returns>
        public static System.Array RemoveArrayItem(System.Array stale, int index, System.Type type)
        {
            // assumes array was normalized already. Pass through null if given.
            if (stale == null) return null;
            int oldLength = stale.Length;
            if (oldLength == 0) return stale; // nothing to remove
            int newLength = oldLength - 1;
            System.Array fresh = System.Array.CreateInstance(type, newLength);
            if (newLength == 0) return fresh; // new array has nothing to copy
            if (index > newLength) index = newLength; // prevent out of bounds issue
            if (index < 0) index = oldLength + index; // enable negative indexes
            // copy from start to the index
            System.Array.Copy(stale, 0, fresh, 0, index);
            // if element was anything but the last element, copy the remainder after the index
            // this method retains the original ordering of the array
            if (index < newLength) System.Array.Copy(stale, index + 1, fresh, index, newLength - index);
            return fresh;
        }

        public static System.Array ResizeArrayAndFill(System.Array stale, int newSize, object fill, System.Type type)
        {
            if (stale == null) return null;
            var oldSize = stale.Length;
            System.Array fresh = System.Array.CreateInstance(type, newSize);
            if (newSize == 0) return fresh;
            System.Array.Copy(stale, fresh, Math.Min(oldSize, newSize));
            for (int i = oldSize; i < newSize; i++) fresh.SetValue(fill, i);
            return fresh;
        }

        /// <summary>
        /// Will hunt through all parents of the given transform (optionally including itself) to find a component of the given type.
        /// As soon as it finds an instance of the requested type, it will immediately return that component.
        /// </summary>
        /// <param name="type">the component type that should be looked for</param>
        /// <param name="t">transform reference to begin searching from</param>
        /// <param name="includeSelf">optionally include the given transform object</param>
        /// <returns></returns>
        public static UnityEngine.Component GetComponentInNearestParent(System.Type type, Transform t, bool includeSelf = true)
        {
            if (t == null) return null;
            Transform parent = t;
            Component component = includeSelf ? t.GetComponent(type) : null;
            while (component == null)
            {
                parent = parent.parent;
                if (parent == null) break;
                component = parent.GetComponent(type);
            }

            return component;
        }

        /// <summary>
        /// Will hunt through all parents of the given transform (optionally including itself) to find a component of the given type.
        /// Continues all the way up the chain, retaining the most recently found component, or null if the type was never encountered.
        /// </summary>
        /// <param name="type">the component type that should be looked for</param>
        /// <param name="t">transform reference to begin searching from</param>
        /// <param name="includeSelf">optionally include the given transform object</param>
        /// <returns></returns>
        public static UnityEngine.Component GetComponentInFurthestParent(System.Type type, Transform t, bool includeSelf = true)
        {
            if (t == null) return null;
            Transform parent = t;
            Component component = includeSelf ? t.GetComponent(type) : null;
            while (parent != null)
            {
                parent = parent.parent;
                if (parent == null) break;
                var c = parent.GetComponent(type);
                if (c != null) component = c;
            }

            return component;
        }
    }
}