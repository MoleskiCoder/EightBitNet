namespace Z80.HarteTest
{
    internal sealed class State
    {
        public ushort PC { get; set; }

        public ushort SP { get; set; }

        // A, B, C, etc. are stored as 8-bit values

        public byte A { get; set; }
        public byte F { get; set; }

        public byte B { get; set; }
        public byte C { get; set; }

        public byte D { get; set; }
        public byte E { get; set; }

        public byte H { get; set; }
        public byte L { get; set; }

        // af_ etc. are the "shadow registers"

        public ushort AF_ { get; set; }
        public ushort BC_ { get; set; }
        public ushort DE_ { get; set; }
        public ushort HL_ { get; set; }

        public byte I { get; set; }
        public byte R { get; set; }

        public byte IM { get; set; }

        // EI refers to if Enable Interrupt was the last-emulated instruction. You can probably ignore this.
        public byte EI { get; set; }

        // Used to track specific behavior during interrupt depending on if CMOS or not and previously-executed instructions. You can probably ignore this.
        public int P { get; set; }

        // Used to track if the last-modified opcode modified flag registers (with a few exceptions). This is important because CCF will behave differently depending on this
        public byte Q { get; set; }

        public byte IFF1 { get; set; }
        public byte IFF2 { get; set; }

        public ushort WZ { get; set; }

        public ushort IX { get; set; }
        public ushort IY { get; set; }

        // Address, value pairs to initialize RAM
        public int[][]? RAM { get; set; }
    }
}
