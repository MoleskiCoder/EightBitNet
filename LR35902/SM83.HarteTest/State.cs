namespace SM83.HarteTest
{
    internal sealed class State
    {
        public ushort PC { get; set; }

        public ushort SP { get; set; }

        public byte A { get; set; }
        public byte F { get; set; }

        public byte B { get; set; }
        public byte C { get; set; }

        public byte D { get; set; }
        public byte E { get; set; }

        public byte H { get; set; }
        public byte L { get; set; }

        public byte IME { get; set; }
        public byte? IE { get; set; }
        public byte? EI { get; set; }

        public int[][]? RAM { get; set; }
    }
}
