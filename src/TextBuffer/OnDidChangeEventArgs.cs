using System;
using System.Collections.Immutable;

namespace TextBuffer
{
    public class OnDidChangeEventArgs : EventArgs
    {
        /// <summary>
        /// The former head position
        /// </summary>
        public Point OldHeadPosition { get; }

        /// <summary>
        /// The new head position
        /// </summary>
        public Point NewHeadPosition { get; }

        /// <summary>
        /// The former tail position
        /// </summary>
        public Point OldTailPosition { get; }

        /// <summary>
        /// The new tail position
        /// </summary>
        public Point NewTailPosition { get; }

        /// <summary>
        /// whether the marker was valid before the change
        /// </summary>
        public bool WasValid { get; }

        /// <summary>
        /// whether the marker is now valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// whether the marker had a tail before the change
        /// </summary>
        public bool HadTail { get; }

        /// <summary>
        /// whether the marker now has a tail
        /// </summary>
        public bool HasTail { get; }

        /// <summary>
        /// the marker's custom properties before the change.
        /// </summary>
        public ImmutableDictionary<string, object> OldProperties { get; }

        /// <summary>
        /// the marker's custom properties after the change.
        /// </summary>
        public ImmutableDictionary<string, object> NewProperties { get; }

        /// <summary>
        /// whether this change was caused by a textual change to the buffer or whether the marker was
        /// manipulated directly via its public API.
        /// </summary>
        public bool TextChanged { get; }
    }
}