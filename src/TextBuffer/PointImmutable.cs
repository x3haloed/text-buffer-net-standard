using System;

namespace TextBuffer
{
    public class PointImmutable : Point
    {
        public PointImmutable(double row, double column) : base(row, column) { }

        public override double Row { get => base.Row; set => throw new Exception("Row is not settable"); }
        public override double Column { get => base.Column; set => throw new Exception("Column is not settable"); }
    }
}
