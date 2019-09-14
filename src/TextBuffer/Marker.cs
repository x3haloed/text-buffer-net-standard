using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    public class Marker : IComparable<Marker>, IComparable, IEquatable<Marker>
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
            IDictionary<string, object> properties = null,
            bool exclusivitySet = false)
        {
            Id = id;
            Layer = layer;
            Tailed = tailed;
            Reversed = reversed;
            Valid = valid;
            Invalidate = invalidate;
            Exclusive = exclusive;
            Properties = properties == null
                ? new ReadOnlyDictionary<string, object>(new Dictionary<string, object>())
                : new ReadOnlyDictionary<string, object>(properties);
            HasChangeObservers = false;
            if (!exclusivitySet)
            {
                Layer.SetMarkerIsExclusive(id, IsExclusive());
            }
            PreviousEventState = GetSnapshot(GetRange());
        }

        #endregion Construction

        #region Static Members

        public static readonly HashSet<string> OptionKeys = new HashSet<string> { "reversed", "tailed", "invalidate", "exclusive" };

        #endregion Static Members

        #region Properties

        public bool TrackDestruction { get; set; }
        public string DestroyStackTrace { get; set; }

        private object Id { get; }
        private MarkerLayer Layer { get; }
        private bool Tailed { get; set; }
        private bool Reversed { get; set; }
        private bool Valid { get; set; }
        private string Invalidate { get; set; }
        private bool? Exclusive { get; set; }
        private ReadOnlyDictionary<string, object> Properties { get; set; }
        private MarkerSnapshot PreviousEventState { get; set; }
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
            return Update(GetRange(), reversed: reversed, tailed: true, range: range, exclusive: exclusive);
        }

        /// <summary>
        /// Returns a {Point} representing the marker's current head position.
        /// </summary>
        public Point GetHeadPosition() => Reversed ? GetStartPosition() : GetEndPosition();

        /// <summary>
        /// Sets the head position of the marker.
        /// </summary>
        /// <param name="position">
        /// A {Point} or point-compatible {Array}. The position will be
        /// clipped before it is assigned.
        /// </param>
        public bool SetHeadPosition(Point position)
        {
            Range oldRange = GetRange();
            bool? reversed = null;
            Range newRange;

            if (HasTail())
            {
                if (IsReversed())
                {
                    if (position.IsLessThan(oldRange.End))
                    {
                        newRange = new Range(position, oldRange.End);
                    }
                    else
                    {
                        reversed = false;
                        newRange = new Range(oldRange.End, position);
                    }
                }
                else
                {
                    if (position.IsLessThan(oldRange.Start))
                    {
                        reversed = true;
                        newRange = new Range(position, oldRange.Start);
                    }
                    else
                    {
                        newRange = new Range(oldRange.Start, position);
                    }
                }
            }
            else
            {
                newRange = new Range(position, position);
            }

            return Update(oldRange, reversed: reversed, range: newRange);
        }

        /// <summary>
        /// Returns a {Point} representing the marker's current tail position.
        /// If the marker has no tail, the head position will be returned instead.
        /// </summary>
        public Point GetTailPosition() => Reversed ? GetEndPosition() : GetStartPosition();

        /// <summary>
        /// Sets the tail position of the marker. If the marker doesn't have a
        /// tail, it will after calling this method.
        /// </summary>
        /// <param name="position">A {Point} or point-compatible {Array}. The position will be
        /// clipped before it is assigned.</param>
        public bool SetTailPosition(Point position)
        {
            Range oldRange = GetRange();
            bool? reversed = null;
            bool tailed = true;
            Range newRange;

            if (Reversed)
            {
                if (position.IsLessThan(oldRange.Start))
                {
                    reversed = false;
                    newRange = new Range(position, oldRange.Start);
                }
                else
                {
                    newRange = new Range(oldRange.Start, position);
                }
            }
            else
            {
                if (position.IsLessThan(oldRange.End))
                {
                    newRange = new Range(position, oldRange.End);
                }
                else
                {
                    reversed = true;
                    newRange = new Range(oldRange.End, position);
                }
            }

            return Update(oldRange, reversed: reversed, tailed: tailed, range: newRange);
        }

        /// <summary>
        /// Returns a {Point} representing the start position of the marker,
        /// which could be the head or tail position, depending on its orientation.
        /// </summary>
        public Point GetStartPosition() => Layer.GetMarkerStartPosition(Id);

        /// <summary>
        /// Returns a {Point} representing the end position of the marker,
        /// which could be the head or tail position, depending on its orientation.
        /// </summary>
        public Point GetEndPosition() => Layer.GetMarkerEndPosition(Id);

        /// <summary>
        /// Removes the marker's tail. After calling the marker's head position
        /// will be reported as its current tail position until the tail is planted
        /// again.
        /// </summary>
        public bool ClearTail()
        {
            Point headPosition = GetHeadPosition();
            return Update(GetRange(), tailed: false, reversed: false, range: new Range(headPosition, headPosition));
        }

        /// <summary>
        /// Plants the marker's tail at the current head position. After calling
        /// the marker's tail position will be its head position at the time of the
        /// call, regardless of where the marker's head is moved.
        /// </summary>
        public bool PlantTail()
        {
            if (!HasTail())
            {
                Point headPosition = GetHeadPosition();
                return Update(GetRange(), tailed: true, range: new Range(headPosition, headPosition));
            }

            return false;
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether the head precedes the tail.
        /// </summary>
        public bool IsReversed() => Tailed && Reversed;

        /// <summary>
        /// Returns a {Boolean} indicating whether the marker has a tail.
        /// </summary>
        public bool HasTail() => Tailed;

        /// <summary>
        /// Is the marker valid?
        /// </summary>
        public bool IsValid() => !IsDestroyed() && Valid;

        /// <summary>
        /// Is the marker destroyed?
        /// </summary>
        public bool IsDestroyed() => !Layer.HasMarker(Id);

        /// <summary>
        /// Returns a {Boolean} indicating whether changes that occur exactly at
        /// the marker's head or tail cause it to move.
        /// </summary>
        public bool IsExclusive()
        {
            if (Exclusive.HasValue && Exclusive.Value)
            {
                return Exclusive.Value; 
            }
            return GetInvalidationStrategy() == "inside" || !HasTail();
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether this marker is equivalent to
        /// another marker, meaning they have the same range and options.
        /// </summary>
        /// <param name="other">{Marker} other marker</param>
        public bool Equals(Marker other) =>
            Invalidate == other.Invalidate &&
                Tailed == other.Tailed &&
                Reversed == other.Reversed &&
                Exclusive == other.Exclusive &&
                Properties.Equals(other.Properties) &&
                GetRange().Equals(other.GetRange());

        /// <summary>
        /// Get the invalidation strategy for this marker.
        /// Valid values include: `never`, `surround`, `overlap`, `inside`, and `touch`.
        /// </summary>
        /// <returns></returns>
        public string GetInvalidationStrategy() => Invalidate;

        /// <summary>
        /// Returns an {Object} containing any custom properties associated with
        /// the marker.
        /// </summary>
        public ReadOnlyDictionary<string, object> GetProperties() => Properties;

        /// <summary>
        /// Merges an {Object} containing new properties into the marker's
        /// existing properties.
        /// </summary>
        /// <param name="properties">
        /// an {Object} containing any custom properties to associate with
        /// the marker.
        /// </param>
        public bool SetProperties(IDictionary<string, object> properties) =>
            Update(GetRange(), properties: Properties.ToImmutableDictionary().AddRange(properties));

        /// <summary>
        /// Creates and returns a new {Marker} with the same properties as this
        /// marker.
        /// </summary>
        public Marker Copy(MarkerOptions options)
        {
            MarkerSnapshot snapshot = GetSnapshot(null);
            return Layer.CreateMarker(GetRange(), snapshot.MergeOptions(options));
        }

        /// <summary>
        ///  Destroys the marker, causing it to emit the 'destroyed' event.
        /// </summary>
        public void Destroy(bool suppressMarkerLayerUpdateEvents)
        {
            if (IsDestroyed())
            {
                return;
            }

            if (TrackDestruction)
            {
                DestroyStackTrace = new Exception().StackTrace;
            }

            Layer.DestroyMarker(this, suppressMarkerLayerUpdateEvents);
            OnDidDestroy.Invoke(this, null);
        }

        /// <summary>
        /// Compares this marker to another based on their ranges.
        /// </summary>
        public int CompareTo(object obj)
        {
            if (obj is Marker objM)
            {
                return CompareTo(objM);
            }
            else
            {
                throw new ArgumentException($"Object {nameof(obj)} is not a {nameof(Marker)}.", nameof(obj));
            }
        }

        /// <summary>
        /// Compares this marker to another based on their ranges.
        /// </summary>
        public int CompareTo(Marker other) => Layer.CompareMarkers(Id, other.Id);

        /// <summary>
        /// Returns whether this marker matches the given parameters. The parameters
        /// are the same as {MarkerLayer.FindMarkers}.
        /// </summary>
        public bool MatchesParams()
        {

        }

        /// <summary>
        /// Returns whether this marker matches the given parameter name and value.
        /// The parameters are the same as {MarkerLayer::findMarkers}.
        /// </summary>
        public bool MatchesParam(string key, object value)
        {
            switch (key)
            {
                case "startPosition":
                    return GetStartPosition().Equals(value);
                case "endPosition":
                    return GetEndPosition().Equals(value);
                case "containsPoint":
                case "containsPosition":
                    return ContainsPoint(value);
                case "containsRange":
                    return ContainsRange(value);
                case "startRow":
                    return GetStartPosition().Row.Equals(value);
                case "endRow":
                    return GetEndPosition().Row.Equals(value);
                case "intersectsRow":
                    return IntersectsRow(value);
                case "invalidate":
                    return Invalidate == (string)value;
                case "reversed":
                    return Reversed == (bool)value;
                case "tailed":
                    return Tailed == (bool)value;
                case "valid":
                    return IsValid() == (bool)value;
                default:
                    return Equals(Properties[key], value);
            }
        }

        public bool Update(
            Range oldRange,
            Range range = null,
            bool? reversed = null,
            bool? tailed = null,
            bool? valid = null,
            bool? exclusive = null,
            IDictionary<string, object> properties = null,
            bool textChanged = false,
            bool suppressMarkerLayerUpdateEvents = false)
        {
            if (IsDestroyed())
            {
                return false;
            }

            bool wasExclusive = IsExclusive();
            bool updated = false, propertiesChanged = false;

            if (range != null && !range.Equals(oldRange))
            {
                Layer.SetMarkerRange(Id, range);
                updated = true;
            }

            if (tailed.HasValue && tailed.Value != Tailed)
            {
                Tailed = tailed.Value;
                updated = true;
            }

            if (valid.HasValue && valid.Value != Valid)
            {
                Valid = valid.Value;
                updated = true;
            }

            if (reversed.HasValue && reversed.Value != Reversed)
            {
                Reversed = reversed.Value;
                updated = true;
            }

            if (wasExclusive != IsExclusive())
            {
                Layer.SetMarkerIsExclusive(Id, IsExclusive());
                updated = true;
            }

            if (properties != null && !properties.Equals(Properties))
            {
                Properties = new ReadOnlyDictionary<string, object>(properties);
                propertiesChanged = true;
                updated = true;
            }

            EmitChangeEvent(range ?? oldRange, textChanged, propertiesChanged);
            if (updated && !suppressMarkerLayerUpdateEvents)
            {
                Layer.MarkerUpdated();
            }

            return updated;
        }

        public MarkerSnapshot GetSnapshot(Range range, bool includeMarker = true) =>
            new MarkerSnapshot(range, Properties, Reversed, Tailed, Valid, Invalidate, Exclusive, includeMarker ? this : null);

        public override string ToString() => $"[Marker {Id}, {GetRange()}]";

        #region Private

        private string Inspect() => ToString();

        private bool EmitChangeEvent(Range currentRange, bool textChanged, bool propertiesChanged)
        {
            MarkerSnapshot oldState = PreviousEventState;

            currentRange = currentRange ?? GetRange();

            if (!propertiesChanged &&
                oldState.Valid == Valid &&
                oldState.Tailed == Tailed &&
                oldState.Reversed == Reversed &&
                oldState.Range.CompareTo(currentRange) == 0)
            {
                return false;
            }

            MarkerSnapshot newState = PreviousEventState = GetSnapshot(currentRange);

            Point oldHeadPosition, oldTailPosition, newHeadPosition, newTailPosition;

            if (oldState.Reversed)
            {
                oldHeadPosition = oldState.Range.Start;
                oldTailPosition = oldState.Range.End;
            }
            else
            {
                oldHeadPosition = oldState.Range.End;
                oldTailPosition = oldState.Range.Start;
            }

            if (newState.Reversed)
            {
                newHeadPosition = newState.Range.Start;
                newTailPosition = newState.Range.End;
            }
            else
            {
                newHeadPosition = newState.Range.End;
                newTailPosition = newState.Range.Start;
            }

            OnDidChange.Invoke(
                new OnDidChangeEventArgs(
                    oldState.Valid, newState.Valid, oldState.Tailed, newState.Tailed,
                    oldState.Properties, newState.Properties,
                    oldHeadPosition, newHeadPosition,
                    oldTailPosition, newTailPosition,
                    textChanged));
            return true;
        }

        #endregion Private
    }
}
