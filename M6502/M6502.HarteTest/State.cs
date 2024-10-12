namespace M6502.HarteTest
{
    internal sealed class State
    {
        public ushort PC { get; set; }

        public byte S { get; set; }

        public byte A { get; set; }

        public byte X { get; set; }

        public byte Y { get; set; }

        public byte P { get; set; }

        public int[][]? RAM { get; set; }
    }
}
