using System;
using TextBuffer;
using Xunit;

namespace TextBufferTest
{
    public class PointTest
    {
        [Theory]
        [InlineData("(0, 0)", 0, 0)]
        [InlineData("(-1, -2)", 1, 2)]
        [InlineData("(1, 2)", -1, -2)]
        [InlineData("(1, -2)", -1, 2)]
        public void NegateShouldNegateTheRowAndColumn(string expected, double row, double column)
        {
            Assert.Equal(expected, new Point(row, column).Negate().ToString());
        }

        [Fact]
        public void FromObjectReturnsANewPointIfObjectIsPointCompatibleArray()
        {
            Assert.Equal(new Point(1, 3), Point.FromObject(new double[] { 1, 3 }));
            Assert.Equal(new Point(double.PositiveInfinity, double.PositiveInfinity), Point.Infinity);
        }

        [Fact]
        public void FromObjectReturnsTheCopyOfObjectIfItIsAnInstanceOfPoint()
        {
            var origin = new Point(0, 0);
            Assert.Same(origin, Point.FromObject(origin, false));
            Assert.NotSame(origin, Point.FromObject(origin, true));
        }

        [Fact]
        public void CopyReturnsACopyOfTheObject()
        {
            Assert.Equal(new Point(3, 4), new Point(3, 4).Copy());
            Assert.Equal(new Point(0, 0), Point.Zero.Copy());
        }

        [Fact]
        public void NegateReturnsANewPointWithRowAndColumnNegated()
        {
            Assert.Equal(new Point(-3, -4), new Point(3, 4).Negate());
            Assert.Equal(new Point(0, 0), Point.Zero.Negate());
        }

        [Fact]
        public void FreezeMakesThePointObjectImmutable()
        {
            Assert.Throws<InvalidOperationException>(() => new Point(3, 4).Freeze().Row = 5);
            Assert.Throws<InvalidOperationException>(() => new Point(3, 4).Freeze().Column = 6);
            Assert.Throws<InvalidOperationException>(() => Point.Zero.Freeze().Row = 1);
            Assert.Throws<InvalidOperationException>(() => Point.Zero.Freeze().Column = 2);
        }

        [Theory]
        [InlineData(-1, 2, 3, 2, 6)]
        [InlineData(-1, 2, 3, 3, 4)]
        [InlineData(0, 1, 1, 1, 1)]
        [InlineData(1, 2, 3, 2, 0)]
        [InlineData(1, 2, 3, 1, 3)]
        public void CompareToReturnsNegOneForLessThanZeroForEqualToOneForGreaterThanComparisons(
            int expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).CompareTo(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(true, 2, 3, 2, 5)]
        [InlineData(true, 2, 3, 3, 4)]
        [InlineData(false, 2, 3, 2, 3)]
        [InlineData(false, 2, 3, 2, 1)]
        [InlineData(false, 2, 3, 1, 2)]
        public void IsLessThanReturnsABooleanIndicatingWhetherAPointPrecedesTheGivenPoint(
            bool expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).IsLessThan(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(true, 2, 3, 2, 5)]
        [InlineData(true, 2, 3, 3, 4)]
        [InlineData(true, 2, 3, 2, 3)]
        [InlineData(false, 2, 3, 2, 1)]
        [InlineData(false, 2, 3, 1, 2)]
        public void IsLessThanOrEqualReturnsABooleanIndicatingWhetherAPointPrecedesOrEqualTheGivenPoint(
            bool expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).IsLessThanOrEqual(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(false, 2, 3, 2, 5)]
        [InlineData(false, 2, 3, 3, 4)]
        [InlineData(false, 2, 3, 2, 3)]
        [InlineData(true, 2, 3, 2, 1)]
        [InlineData(true, 2, 3, 1, 2)]
        public void IsGreaterThanReturnsABooleanIndicatingWhetherAPointFollowsTheGivenPoint(
            bool expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).IsGreaterThan(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(false, 2, 3, 2, 5)]
        [InlineData(false, 2, 3, 3, 4)]
        [InlineData(true, 2, 3, 2, 3)]
        [InlineData(true, 2, 3, 2, 1)]
        [InlineData(true, 2, 3, 1, 2)]
        public void IsGreaterThanOrEqualReturnsABooleanIndicatingWhetherAPointFollowsOrEqualTheGivenPoint(
            bool expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).IsGreaterThanOrEqual(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(true, 1, 1, 1, 1)]
        [InlineData(true, 1, 2, 1, 2)]
        [InlineData(false, 1, 2, 3, 3)]
        [InlineData(false, 1, 2, 1, 3)]
        [InlineData(false, 1, 2, 3, 2)]
        public void EqualsReturnsIfWhetherTwoPointsAreEqual(
            bool expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(expected, new Point(rowA, columnA).Equals(new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(false, -1, -1)]
        [InlineData(false, -1, 0)]
        [InlineData(false, -1, double.PositiveInfinity)]
        [InlineData(false, 0, 0)]
        [InlineData(true, 0, 1)]
        [InlineData(true, 5, 0)]
        [InlineData(true, 5, -1)]
        public void IsPositiveReturnsTrueIfThePointRepresentsAForwardTraversal(
            bool expected, double row, double column)
        {
            Assert.Equal(expected, new Point(row, column).IsPositive());
        }

        [Theory]
        [InlineData(false, 1, 1)]
        [InlineData(false, 0, 1)]
        [InlineData(false, 1, 0)]
        [InlineData(true, 0, 0)]
        public void IsZeroReturnsTrueIfThePointIsZero(
            bool expected, double row, double column)
        {
            Assert.Equal(expected, new Point(row, column).IsZero());
        }

        [Theory]
        [InlineData(1, 1, 3, 4, 1, 1)]
        [InlineData(1, 2, 1, 2, 5, 6)]
        public void MinReturnsTheMinimumOfTwoPoints(
            double expectedRow, double expectedColumn, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(new Point(expectedRow, expectedColumn), Point.Min(new Point(rowA, columnA), new Point(rowB, columnB)));
        }

        [Theory]
        [InlineData(3, 4, 3, 4, 1, 1)]
        [InlineData(5, 6, 1, 2, 5, 6)]
        public void MaxReturnsTheMaximumOfTwoPoints(
            double expectedRow, double expectedColumn, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(new Point(expectedRow, expectedColumn), Point.Max(new Point(rowA, columnA), new Point(rowB, columnB)));
        }
    }
}
