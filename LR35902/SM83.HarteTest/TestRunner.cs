namespace SM83.HarteTest
{
    using EightBit;

    internal sealed class TestRunner : LR35902.Bus
    {
        private readonly MemoryMapping _mapping;

        public Ram RAM { get; } = new(0x10000);

        public TestRunner()
        : base(false)
        {
            this._mapping = new(this.RAM, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
            this.RaisedPOWER += this.TestRunner_RaisedPOWER;
            this.LoweringPOWER += this.TestRunner_LoweringPOWER;
        }

        private void TestRunner_RaisedPOWER(object? sender, EventArgs e)
        {
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseHALT();
        }

        private void TestRunner_LoweringPOWER(object? sender, EventArgs e)
        {
            this.CPU.LowerPOWER();
        }

        public override MemoryMapping Mapping(ushort _) => this._mapping;

        public override void Initialize()
        {
        }
    }
}
