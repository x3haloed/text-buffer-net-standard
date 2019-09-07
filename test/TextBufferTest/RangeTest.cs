using System;
using System.Collections.Generic;
using System.Text;
using TextBuffer;
using Xunit;

namespace TextBufferTest
{
    public class RangeTest
    {
        [Theory]
        [InlineData(false, 1, 2, 3, 4, 1, 0, 1, 0)]
        [InlineData(true, 1, 2, 3, 4, 1, 1, 1, 2)]
        [InlineData(true, 1, 2, 3, 4, 1, 1, 1, 3)]
        [InlineData(true, 1, 2, 3, 4, 3, 4, 4, 5)]
        [InlineData(true, 1, 2, 3, 4, 3, 3, 4, 5)]
        [InlineData(true, 1, 2, 3, 4, 1, 5, 2, 2)]
        [InlineData(false, 1, 2, 3, 4, 3, 5, 4, 4)]
        public void IntersectsWithWhenTheExclusiveArgumentIsFalseByDefaultReturnsTrueIfTheRangesIntersectExclusiveOfTheirEndpoints(
            bool expected, double rangeARowA, double rangeAColumnA, double rangeARowB, double rangeAColumnB,
            double rangeBRowA, double rangeBColumnA, double rangeBRowB, double rangeBColumnB)
        {
            Assert.Equal(
                expected,
                new Range(new Point(rangeARowA, rangeAColumnA), new Point(rangeARowB, rangeAColumnB))
                    .IntersectsWith(new Range(new Point(rangeBRowA, rangeBColumnA), new Point(rangeBRowB, rangeBColumnB))));
        }

        [Theory]
        [InlineData(false, 1, 2, 3, 4, 1, 2, 1, 2)]
        [InlineData(false, 1, 2, 3, 4, 3, 4, 3, 4)]
        public void IntersectsWithWhenTheExclusiveArgumentIsFalseReturnsTrueIfTheRangesIntersectExclusiveOfTheirEndpoints(
            bool expected, double rangeARowA, double rangeAColumnA, double rangeARowB, double rangeAColumnB,
            double rangeBRowA, double rangeBColumnA, double rangeBRowB, double rangeBColumnB)
        {
            Assert.Equal(
                expected,
                new Range(new Point(rangeARowA, rangeAColumnA), new Point(rangeARowB, rangeAColumnB))
                    .IntersectsWith(
                        new Range(new Point(rangeBRowA, rangeBColumnA), new Point(rangeBRowB, rangeBColumnB)),
                        true));
        }

        [Theory]
        [InlineData(false, 1, 2, 3, 4, 1, 0, 1, 1)]
        [InlineData(false, 1, 2, 3, 4, 1, 1, 1, 2)]
        [InlineData(true, 1, 2, 3, 4, 1, 1, 1, 3)]
        [InlineData(false, 1, 2, 3, 4, 3, 4, 4, 5)]
        [InlineData(true, 1, 2, 3, 4, 3, 3, 4, 5)]
        [InlineData(true, 1, 2, 3, 4, 1, 5, 2, 2)]
        [InlineData(false, 1, 2, 3, 4, 3, 5, 4, 4)]
        [InlineData(false, 1, 2, 3, 4, 1, 2, 1, 2)]
        [InlineData(false, 1, 2, 3, 4, 3, 4, 3, 4)]
        public void IntersectsWithWhenTheExclusiveArgumentIsTrueReturnsTrueIfTheRangesIntersectExclusiveOfTheirEndpoints(
            bool expected, double rangeARowA, double rangeAColumnA, double rangeARowB, double rangeAColumnB,
            double rangeBRowA, double rangeBColumnA, double rangeBRowB, double rangeBColumnB)
        {
            Assert.Equal(
                expected,
                new Range(new Point(rangeARowA, rangeAColumnA), new Point(rangeARowB, rangeAColumnB))
                    .IntersectsWith(
                        new Range(new Point(rangeBRowA, rangeBColumnA), new Point(rangeBRowB, rangeBColumnB)),
                        true));
        }

        [Theory]
        [InlineData("[(0, 0) - (0, 0)]", 0, 0, 0, 0)]
        [InlineData("[(-3, -4) - (-1, -2)]", 1, 2, 3, 4)]
        [InlineData("[(1, 2) - (3, 4)]", -1, -2, -3, -4)]
        [InlineData("[(-3, 4) - (1, -2)]", -1, 2, 3, -4)]
        public void NegateShouldNegateTheStartAndEndPoints(
            string expected, double rowA, double columnA, double rowB, double columnB)
        {
            Assert.Equal(
                expected,
                new Range(new Point(rowA, columnA), new Point(rowB, columnB)).Negate().ToString());
        }
    }
}
