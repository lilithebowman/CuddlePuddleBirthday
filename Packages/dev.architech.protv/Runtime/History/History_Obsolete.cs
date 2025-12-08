using System;

namespace ArchiTech.ProTV
{
    public partial class History
    {
        /// <summary>
        /// Use <see cref="Clear()"/> instead
        /// </summary>
        [Obsolete("Use Clear() instead")]
        public void _Clear() => Clear();

        /// <summary>
        /// Use <see cref="SelectEntry()"/> instead
        /// </summary>
        [Obsolete("Use SelectEntry() instead")]
        public void _SelectEntry() => SelectEntry();

        /// <summary>
        /// Use <see cref="SelectEntry(int)"/> instead
        /// </summary>
        [Obsolete("Use SelectEntry(int) instead")]
        public void _SelectEntry(int index) => SelectEntry(index);
    }
}