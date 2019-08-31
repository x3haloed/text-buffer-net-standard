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
    }
}
