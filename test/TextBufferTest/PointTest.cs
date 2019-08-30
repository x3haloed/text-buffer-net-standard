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
    }
}
