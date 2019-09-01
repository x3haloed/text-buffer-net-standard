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
    }
}
