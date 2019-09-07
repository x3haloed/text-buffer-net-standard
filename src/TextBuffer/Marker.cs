using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace TextBuffer
{
    /// <summary>
    /// Represents a buffer annotation that remains logically stationary
    /// even as the buffer changes. This is used to represent cursors, folds, snippet
    /// targets, misspelled words, and anything else that needs to track a logical
    /// location in the buffer over time.
    /// 
    /// Head and Tail:
    /// Markers always have a *head* and sometimes have a *tail*. If you think of a
    /// marker as an editor selection, the tail is the part that's stationary and the
    /// head is the part that moves when the mouse is moved. A marker without a tail
    /// always reports an empty range at the head position. A marker with a head position
    /// greater than the tail is in a "normal" orientation. If the head precedes the
    /// tail the marker is in a "reversed" orientation.
    /// 
    /// Validity:
    /// Markers are considered *valid* when they are first created. Depending on the
    /// invalidation strategy you choose, certain changes to the buffer can cause a
    /// marker to become invalid, for example if the text surrounding the marker is
    /// deleted. See {TextBuffer::markRange} for invalidation strategies.
    /// </summary>
    public class Marker
    {
        #region Construction

        public Marker(
            object id,
            MarkerLayer layer,
            Range range,
            bool tailed = true,
            bool reversed = false,
            bool valid = true,
            string invalidate = "overlap",
            bool? exclusive = null,
            Dictionary<string, object> properties = null,
            bool exclusivitySet = false)
        {
            Id = id;
            Layer = layer;
            Tailed = tailed;
            Reversed = reversed;
            Valid = valid;
            Invalidate = invalidate;
            Exclusive = exclusive;
            Properties = properties?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
            HasChangeObservers = false;
            if (!exclusivitySet)
            {
                Layer.SetMarkerIsExclusive(id, IsExclusive());
            }
        }

        #endregion Construction

        #region Static Members

        private static readonly HashSet<string> OptionKeys = new HashSet<string> { "reversed", "tailed", "invalidate", "exclusive" };

        #endregion Static Members

        #region Properties

        private object Id { get; }
        private MarkerLayer Layer { get; }
        private bool Tailed { get; }
        private bool Reversed { get; }
        private bool Valid { get; }
        private string Invalidate { get; }
        private bool? Exclusive { get; }
        private ImmutableDictionary<string, object> Properties { get; }
        private bool HasChangeObservers { get; }

        #endregion Properties

        #region Event Subscription

        /// <summary>
        /// The marker was destroyed.
        /// </summary>
        public event EventHandler OnDidDestroy;

        /// <summary>
        /// The state of the marker has changed.
        /// </summary>
        public event OnDidChangeDelegate OnDidChange;
        public delegate void OnDidChangeDelegate(OnDidChangeEventArgs e);

        #endregion Event Subscription

        /// <summary>
        /// Returns the current {Range} of the marker. The range is immutable.
        /// </summary>
        public Range GetRange() => Layer.GetMarkerRange(Id);

        /// <summary>
        /// Sets the range of the marker.
        /// </summary>
        /// <param name="range">The range will be clipped before it is assigned.</param>
        /// <param name="reversed">indicates the marker will to be in a reversed orientation.</param>
        /// <param name="exclusive">
        /// indicates that changes occurring at either end of
        /// the marker will be considered *outside* the marker rather than inside.
        /// This defaults to `false` unless the marker's invalidation strategy is
        /// `inside` or the marker has no tail, in which case it defaults to `true`.
        /// </param>
        /// <returns></returns>
        public bool SetRange(Range range, bool? reversed = null, bool? exclusive = null)
        {
            return Update(GetRange(), reversed, true, range, exclusive);
        }
    }
}
