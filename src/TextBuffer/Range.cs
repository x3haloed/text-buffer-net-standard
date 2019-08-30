using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TextBuffer
{
    /// <summary>
    /// Represents a region in a buffer in row/column coordinates.
    ///
    /// Every public method that takes a range also accepts a *range-compatible*
    /// {Array}. This means a 2-element array containing {Point}s or point-compatible
    /// arrays. So the following are equivalent:
    /// </summary>
    /// <example>new Range(new Point(0, 1), new Point(2, 3))</example>
    /// <example>new Range(new[] {0, 1}, new[] {2, 3})</example>
    /// <example>new[] {new[] {0, 1}, new[] {2, 3}} # Range compatible array</example>
    public class Range : IComparable<Range>, IComparable, IEquatable<Range>
    {
        private static readonly Regex _newlineRegex = Helpers.NewlineRegex;

        #region Construction

        public Range(Point pointA = null, Point pointB = null)
        {
            pointA = pointA ?? new Point(0, 0);
            pointB = pointB ?? new Point(0, 0);

            if (pointA.IsLessThanOrEqual(pointB))
            {
                Start = pointA;
                End = pointB;
            }
            else
            {
                Start = pointB;
                End = pointA;
            }
        }

        /// <summary>
        /// Convert any range-compatible object to a {Range}.
        /// </summary>
        /// <param name="obj">
        /// This can be an object that's already a {Range}, in which case it's
        /// simply returned, or an array containing two {Point}s or point-compatible
        /// arrays.
        /// </param>
        /// <param name="copy">
        /// An optional boolean indicating whether to force the copying of objects
        /// that are already ranges.
        /// </param>
        /// <returns>A {Range} based on the given object.</returns>
        public static Range FromObject(object obj, bool copy = false)
        {
            if (obj is object[] objA)
            {
                return new Range(Point.FromObject(objA[0]), Point.FromObject(objA[1]));
            }

            if (obj is Range objR)
            {
                if (copy)
                {
                    return objR.Copy();
                }

                return objR;
            }

            var objType = obj.GetType();
            var startField = objType.GetField("Start") ?? objType.GetField("start");
            var endField = objType.GetField("End") ?? objType.GetField("end");

            if (startField != null && endField != null)
            {
                return new Range(Point.FromObject(startField.GetValue(obj)), Point.FromObject(endField.GetValue(obj)));
            }
            else if (objType.GetFields().Length > 1)
            {
                startField = objType.GetFields()[0];
                endField = objType.GetFields()[1];

                return new Range(Point.FromObject(startField.GetValue(obj)), Point.FromObject(endField.GetValue(obj)));
            }

            throw new ArgumentException($"Object {nameof(obj)} is not range-compatible", nameof(obj));
        }

        /// <summary>
        /// Returns a range based on an optional starting point and the given text. If
        /// no starting point is given it will be assumed to be [0, 0].
        /// </summary>
        /// <param name="text">
        /// A {String} after which the range should end. The range will have as many
        /// rows as the text has lines have an end column based on the length of the
        /// last line.
        /// </param>
        /// <param name="startPoint">{Point} where the range should start.</param>
        /// <returns>A {Range}</returns>
        public static Range FromText(string text, Point startPoint = null)
        {
            startPoint = startPoint ?? new Point(0, 0);
            Point endPoint = startPoint.Copy();
            string[] lines = _newlineRegex.Split(text);

            int lastIndex;

            if (lines.Length > 1)
            {
                lastIndex = lines.Length - 1;
                endPoint.Row += lastIndex;
                endPoint.Column = lines[lastIndex].Length;
            }
            else
            {
                endPoint.Column += lines[0].Length;
            }

            return new Range(startPoint, endPoint);
        }

        /// <summary>
        /// Returns a {Range} that starts at the given point and ends at the
        /// start point plus the given row and column deltas.
        /// </summary>
        /// <param name="startPoint">A {Point} or point-compatible {Array}</param>
        /// <param name="rowDelta">
        /// A {Number} indicating how many rows to add to the start point
        /// to get the end point.
        /// </param>
        /// <param name="columnDelta">
        /// A {Number} indicating how many rows to columns to the start
        /// point to get the end point.
        /// </param>
        public static Range FromPointWithDelta(Point startPoint, double rowDelta, double columnDelta)
        {
            var endPoint = new Point(startPoint.Row + rowDelta, startPoint.Column + columnDelta);
            return new Range(startPoint, endPoint);
        }

        public static Range FromPointWithTraversalExtent(Point startPoint, Point extent)
            => new Range(startPoint, startPoint.Traverse(extent));

        /// <summary>
        /// Returns a new range with the same start and end positions.
        /// </summary>
        public Range Copy() => new Range(Start.Copy(), End.Copy());

        /// <summary>
        /// Returns a new range with the start and end positions negated.
        /// </summary>
        public Range Negate() => new Range(Start.Negate(), End.Negate());

        #endregion Construction

        #region Properties

        /// <summary>
        /// A {Point} representing the start of the {Range}.
        /// </summary>
        public virtual Point Start { get; set; }

        /// <summary>
        /// A {Point} representing the end of the {Range}.
        /// </summary>
        public virtual Point End { get; set; }

        #endregion Properties

        #region Serialization and Deserialization

        public double[][] Serialize() => new[] { Start.Serialize(), End.Serialize() };

        /// <summary>
        /// Call this with the result of <see cref="Serialize"/> to construct a new Range.
        /// </summary>
        /// <param name="array">{Array} of params to pass to the constructor</param>
        public static Range Deserialize(object[] array)
        {
            if (array != null && array.Length > 1)
            {
                return new Range(Point.FromObject(array[0]), Point.FromObject(array[1]));
            }

            return new Range();
        }

        #endregion Serialization and Deserialization

        #region Range Details

        /// <summary>
        /// Is the start position of this range equal to the end position?
        /// </summary>
        public bool IsEmpty() => Start.Equals(End);

        /// <summary>
        /// Returns a {Boolean} indicating whether this range starts and ends on
        /// the same row.
        /// </summary>
        /// <returns></returns>
        public bool IsSingleLine() => Start.Row == End.Row;

        /// <summary>
        /// Get the number of rows in this range.
        /// </summary>
        public double GetRowCount() => End.Row - Start.Row + 1;

        /// <summary>
        /// Returns an array of all rows in the range.
        /// </summary>
        public double[] GetRows() => GetRowsEnumerable().ToArray();

        /// <summary>
        /// Returns an interator of all rows in the range.
        /// </summary>
        public IEnumerable<double> GetRowsEnumerable()
        {
            for (double i = Start.Row; i <= End.Row; i += 1)
            {
                yield return i;
            }
        }

        #endregion Range Details

        #region Operations

        /// <summary>
        /// Freezes the range and its start and end point so it becomes
        /// immutable and returns itself.
        /// </summary>
        /// <returns>Returns an immutable version of this {Range}</returns>
        public RangeImmutable Freeze() => new RangeImmutable(Start.Freeze(), End.Freeze());

        /// <summary>
        /// Returns a new range that contains this range and the given range.
        /// </summary>
        /// <param name="otherRange">A {Range} or range-compatible {Array}</param>
        public Range Union(Range otherRange)
        {
            Point start = Start.IsLessThan(otherRange.Start) ? Start : otherRange.Start;
            Point end = End.IsGreaterThan(otherRange.End) ? End : otherRange.End;
            return new Range(start, end);
        }

        /// <summary>
        /// Build and return a new range by translating this range's start and
        /// end points by the given delta(s).
        /// </summary>
        /// <param name="startDelta">A {Point} by which to translate the start of this range.</param>
        /// <param name="endDelta">
        /// A {Point} to by which to translate the end of this
        /// range. If omitted, the `startDelta` will be used instead.
        /// </param>
        public Range Translate(Point startDelta, Point endDelta = null)
            => new Range(Start.Translate(startDelta), End.Translate(endDelta ?? startDelta));

        /// <summary>
        /// Build and return a new range by traversing this range's start and
        /// end points by the given delta.
        /// </summary>
        /// <param name="delta">
        /// A {Point} containing the rows and columns to traverse to derive
        /// the new range.
        /// </param>
        public Range Traverse(Point delta) => new Range(Start.Traverse(delta), End.Traverse(delta));

        #endregion Operations

        #region Comparison

        /// <summary>Compare two Ranges</summary>
        /// <param name="other">A {Range} or range-compatible {Array}.</param>
        /// <returns>
        /// Returns -1 if this range starts before the argument or contains it.
        /// Returns 0 if this range is equivalent to the argument.
        /// Returns 1 if this range starts after the argument or is contained by it.
        /// </returns>
        public int CompareTo(Range other)
        {
            int startCompare = Start.CompareTo(other.Start);

            if (startCompare != 0)
            {
                return startCompare;
            }

            return other.End.CompareTo(End);
        }

        /// <summary>Compare two Ranges</summary>
        /// <param name="other">A {Range} or range-compatible {Array}.</param>
        /// <returns>
        /// Returns -1 if this range starts before the argument or contains it.
        /// Returns 0 if this range is equivalent to the argument.
        /// Returns 1 if this range starts after the argument or is contained by it.
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj is Range objR)
            {
                return CompareTo(objR);
            }
            else
            {
                throw new ArgumentException($"Object {nameof(obj)} is not a {nameof(Range)}.", nameof(obj));
            }
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether this range has the same start
        /// and end points as the given {Range} or range-compatible {Array}.
        /// </summary>
        /// <param name="other">A {Range} or range-compatible {Array}.</param>
        public bool Equals(Range other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Start.Equals(Start) && other.End.Equals(End);
        }

        public override bool Equals(object obj)
        {
            if (obj is Range objR)
            {
                return Equals(objR);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode() ^ End.GetHashCode();
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether this range starts and ends on
        /// the same row as the argument.
        /// </summary>
        /// <param name="other">A {Range} or range-compatible {Array}.</param>
        public bool CoversSameRows(Range other) => Start.Row.Equals(other.Start.Row) && End.Row.Equals(other.End.Row);

        /// <summary>
        /// Determines whether this range intersects with the argument.
        /// </summary>
        /// <param name="otherRange">A {Range} or range-compatible {Array}</param>
        /// <param name="exclusive">
        /// {Boolean} indicating whether to exclude endpoints
        /// when testing for intersection. Defaults to `false`.
        /// </param>
        public bool IntersectsWith(Range otherRange, bool exclusive = false)
        {
            if (exclusive)
            {
                return !(End.IsLessThanOrEqual(otherRange.Start) || Start.IsGreaterThanOrEqual(otherRange.End));
            }

            return !(End.IsLessThan(otherRange.Start) || Start.IsGreaterThan(otherRange.End));
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether this range contains the given range.
        /// </summary>
        /// <param name="otherRange">A {Range} or range-compatible {Array}</param>
        /// <param name="exclusive">{Boolean} including that the containment should be exclusive of
        /// endpoints. Defaults to false.</param>
        /// <returns></returns>
        public bool ContainsRange(Range otherRange, bool exclusive = false)
            => ContainsPoint(otherRange.Start, exclusive) && ContainsPoint(otherRange.End, exclusive);

        /// <summary>
        /// Returns a {Boolean} indicating whether this range contains the given point.
        /// </summary>
        /// <param name="point">A {Point} or point-compatible {Array}</param>
        /// <param name="exclusive">
        /// {Boolean} including that the containment should be exclusive of
        /// endpoints. Defaults to false.
        /// </param>
        /// <returns></returns>
        public bool ContainsPoint(Point point, bool exclusive = false)
        {
            if (exclusive)
            {
                return point.IsGreaterThan(Start) && point.IsLessThan(End);
            }

            return point.IsGreaterThanOrEqual(Start) && point.IsLessThanOrEqual(End);
        }

        /// <summary>
        /// Returns a {Boolean} indicating whether this range intersects the
        /// given row {Number}.
        /// </summary>
        /// <param name="row">Row {Number}</param>
        public bool IntersectsRow(double row) => Start.Row <= row && row <= End.Row;

        /// <summary>
        /// Returns a {Boolean} indicating whether this range intersects the
        /// row range indicated by the given startRow and endRow {Number}s.
        /// </summary>
        /// <param name="rowA">{Number} start row</param>
        /// <param name="rowB">{Number} end row</param>
        /// <returns></returns>
        public bool IntersectsRowRange(double rowA, double rowB)
        {
            double startRow;
            double endRow;

            if (rowA <= rowB)
            {
                startRow = rowA;
                endRow = rowB;
            }
            else
            {
                startRow = rowB;
                endRow = rowA;
            }

            return End.Row >= startRow && endRow >= Start.Row;
        }

        public Point GetExtent() => End.TraversalFrom(Start);

        #endregion Comparison

        #region Conversion

        public Point ToDelta()
        {
            double rows = End.Row - Start.Row;
            double columns;

            if (rows is 0)
            {
                columns = End.Column - Start.Column;
            }
            else
            {
                columns = End.Column;
            }

            return new Point(rows, columns);
        }

        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        public override string ToString() => $"[{Start} - {End}]";

        #endregion Conversion
    }
}
