using System;

namespace TextBuffer
{
    public class RangeImmutable : Range
    {
        public RangeImmutable(Point pointA = null, Point pointB = null) : base(pointA, pointB) { }

        public override Point Start { get => base.Start; set => throw new Exception("Start is not settable"); }
        public override Point End { get => base.End; set => throw new Exception("End is not settable"); }
    }
}
