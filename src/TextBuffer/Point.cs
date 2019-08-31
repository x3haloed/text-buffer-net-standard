using System;
using System.Collections.Generic;
using System.Text;

namespace TextBuffer
{
    /// <summary>
    /// Represents a point in a buffer in row/column coordinates.
    /// 
    /// Every public method that takes a point also accepts a *point-compatible*
    /// {Array}. This means a 2-element array containing {Number}s representing the
    /// row and column. So the following are equivalent:
    /// 
    /// ```coffee
    /// new Point(1, 2)
    /// [1, 2] # Point compatible Array
    /// ```
    /// </summary>
    public class Point : IComparable<Point>, IComparable, IEquatable<Point>
    {
        #region Construction

        public Point(double row, double column)
        {
            Row = row;
            Column = column;
        }

        /// <summary>
        /// Convert any point-compatible object to a <see cref="Point"/>.
        /// </summary>
        /// <param name="obj">
        /// This can be an object that's already a <see cref="Point"/>, in which case it's
        /// simply returned, or an array containing two {Number}s representing the
        /// row and column.
        /// </param>
        /// <param name="copy">
        /// An optional boolean indicating whether to force the copying of objects
        /// that are already points.
        /// </param>
        /// <returns>A <see cref="Point"/> based on the given object.</returns>
        public static Point FromObject(object obj, bool copy = false)
        {
            if (obj is Point objP)
            {
                if (copy)
                {
                    return objP.Copy();
                }
                else
                {
                    return objP;
                }
            }

            if (obj is double[] objA)
            {
                return new Point(objA[0], objA[1]);
            }

            var objType = obj.GetType();
            var rowField = objType.GetField("Row") ?? objType.GetField("row");
            var columnField = objType.GetField("Column") ?? objType.GetField("column");

            if (rowField != null && columnField != null)
            {
                if (rowField.FieldType == typeof(double) && columnField.FieldType == typeof(double))
                {
                    return new Point((double)rowField.GetValue(obj), (double)columnField.GetValue(obj));
                }

                throw new ArgumentException($"Object {nameof(obj)} has fields \"Column\" and \"Row\", but they are not of type {typeof(double).Name}.", nameof(obj));
            }
            else if (objType.GetFields().Length > 1)
            {
                rowField = objType.GetFields()[0];
                columnField = objType.GetFields()[1];

                if (rowField.FieldType == typeof(double) && columnField.FieldType == typeof(double))
                {
                    return new Point((double)rowField.GetValue(obj), (double)columnField.GetValue(obj));
                }

                throw new ArgumentException($"Object {nameof(obj)} has two fields, but they are not of type {typeof(double).Name}.", nameof(obj));
            }

            throw new ArgumentException($"Object {nameof(obj)} is not point-compatible", nameof(obj));
        }

        /// <summary>
        /// Returns a new {Point} with the same row and column.
        /// </summary>
        public Point Copy()
        {
            return new Point(Row, Column);
        }

        /// <summary>
        /// Returns a new {Point} with the row and column negated.
        /// </summary>
        public Point Negate()
        {
            return new Point(-Row, -Column);
        }

        #endregion Construction

        #region Variables

        private bool _frozen = false;
        private double _row;
        private double _column;

        #endregion Variables

        #region Properties
        /// <summary>
        /// A zero-indexed {Number} representing the row of the {Point}.
        /// </summary>
        public double Row
        {
            get => _row;
            set => _row = _frozen
                ? throw new InvalidOperationException("Cannot set value because object is frozen.")
                : value;
        }

        /// <summary>
        /// A zero-indexed {Number} representing the column of the {Point}.
        /// </summary>
        public double Column
        {
            get => _column;
            set => _column = _frozen
                ? throw new InvalidOperationException("Cannot set value because object is frozen.")
                : value;
        }

        #endregion Properties


        #region Comparison

        /// <returns>
        /// -1 if this point precedes the argument.
        /// 0 if this point is equivalent to the argument.
        /// 1 if this point follows the argument.
        /// </returns>
        public int CompareTo(Point other)
        {
            if (Row > other.Row)
            {
                return 1;
            }
            else if (Row < other.Row)
            {
                return -1;
            }
            else
            {
                if (Column > other.Column)
                {
                    return 1;
                }
                else if (Column < other.Column)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <param name="obj">A {Point} or point-compatible {Array}.</param>
        /// <returns>
        /// -1 if this point precedes the argument.
        /// 0 if this point is equivalent to the argument.
        /// 1 if this point follows the argument.
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj is Point objP)
            {
                return CompareTo(objP);
            }
            else
            {
                throw new ArgumentException($"Object {nameof(obj)} is not a {nameof(Point)}.", nameof(obj));
            }
        }

        public bool Equals(Point other)
        {
            if (other == null)
            {
                return false;
            }
            return Row == other.Row && Column == other.Column;
        }        

        public override bool Equals(object obj)
        {
            if (obj is Point objP)
            {
                return Equals(objP);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Row.GetHashCode() ^ Column.GetHashCode();
        }

        public static Point Zero { get; } = new Point(0, 0).Freeze();
        public static Point Infinity { get; } = new Point(double.PositiveInfinity, double.PositiveInfinity).Freeze();

        /// <summary>
        /// Returns a {Boolean} indicating whether this point precedes the given
        /// {Point} or point-compatible {Array}.
        /// </summary>
        /// <param name="other">A {Point} or point-compatible {Array}.</param>
        public bool IsLessThan(Point other) => CompareTo(other) < 0;

        /// <summary>
        /// Returns a {Boolean} indicating whether this point precedes or is
        /// equal to the given {Point} or point-compatible {Array}.
        /// </summary>
        /// <param name="other">A {Point} or point-compatible {Array}.</param>
        public bool IsLessThanOrEqual(Point other) => CompareTo(other) <= 0;

        /// <summary>
        /// Returns a {Boolean} indicating whether this point follows the given
        /// {Point} or point-compatible {Array}.
        /// </summary>
        /// <param name="other">A {Point} or point-compatible {Array}.</param>
        public bool IsGreaterThan(Point other) => CompareTo(other) > 0;

        /// <summary>
        /// Returns a {Boolean} indicating whether this point follows or is
        /// equal to the given {Point} or point-compatible {Array}.
        /// </summary>
        /// <param name="other">A {Point} or point-compatible {Array}.</param>
        public bool IsGreaterThanOrEqual(Point other) => CompareTo(other) >= 0;

        public bool IsZero() => Row is 0 && Column is 0;

        public bool IsPositive()
        {
            if (Row > 0)
	        {
                return true;
	        }
            else if (Row < 0)
            {
                return false;
            }

            return Column > 0;
        }

        public bool IsNegative()
        {
            if (Row < 0)
            {
                return true;
            }
            else if (Row > 0)
            {
                return false;
            }

            return Column < 0;
        }

        #endregion Comparison

        #region Operations

        /// <summary>
        /// Makes this point immutable and returns itself.
        /// </summary>
        /// <returns>
        /// an immutable version of this {Point}
        /// </returns>
        public Point Freeze()
        {
            _frozen = true;
            return this;
        }

        /// <summary>
        /// Build and return a new point by adding the rows and columns of
        /// the given point.
        /// </summary>
        /// <param name="other">
        /// A {Point} whose row and column will be added to this point's row
        /// and column to build the returned point.
        /// </param>
        /// <returns>a {Point}.</returns>
        public Point Translate(Point other) => new Point(Row + other.Row, Column + other.Column);

        /// <summary>
        /// Build and return a new {Point} by traversing the rows and columns
        /// specified by the given point.
        /// 
        /// This method differs from the direct, vector-style addition offered by
        /// <see cref="Translate(Point)"/>. Rather than adding the rows and columns directly, it derives
        /// the new point from traversing in "typewriter space". At the end of every row
        /// traversed, a carriage return occurs that returns the columns to 0 before
        /// continuing the traversal.
        /// </summary>
        /// <example>
        /// Traversing 0 rows, 2 columns:
        /// new Point(10, 5).Traverse(new Point(0, 2)) # => [10, 7]`
        /// </example>
        /// <example>
        /// Traversing 2 rows, 2 columns. Note the columns reset from 0 before adding:
        /// `new Point(10, 5).Traverse(new Point(2, 2)) # => [12, 2]`
        /// </example>
        /// <param name="other">A {Point} providing the rows and columns to traverse by.</param>
        /// <returns>a {Point}</returns>
        public Point Traverse(Point other)
        {
            double row = Row + other.Row;
            double column;

            if (other.Row is 0)
            {
                column = Column + other.Column;
            }
            else
            {
                column = other.Column;
            }

            return new Point(row, column);
        }

        public Point TraversalFrom(Point other)
        {
            if (Row == other.Row)
            {
                if (double.IsInfinity(Column) && double.IsInfinity(other.Column))
                {
                    return new Point(0, 0);
                }

                return new Point(0, Column - other.Column);
            }

            return new Point(Row - other.Row, Column);
        }

        public (Point, Point) SplitAt(double column)
        {
            double rightColumn = Row is 0 ? Column - column : Column;
            return (new Point(0, column), new Point(Row, rightColumn));
        }

        #endregion Operations

        #region Conversion

        /// <summary>
        /// Returns an array of this point's row and column.
        /// </summary>
        public double[] ToArray() => new[] { Row, Column };

        /// <summary>
        /// Returns an array of this point's row and column.
        /// </summary>
        public double[] Serialize() => ToArray();

        /// <summary>
        /// Returns a string representation of the point.
        /// </summary>
        public override string ToString() => $"({Row}, {Column})";

        #endregion Conversion
    }
}
