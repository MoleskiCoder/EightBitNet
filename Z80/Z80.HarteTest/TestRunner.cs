namespace Z80.HarteTest
{
    using EightBit;

    internal sealed class TestRunner : Bus
    {
        private readonly MemoryMapping _mapping;

        private readonly InputOutput ports = new();

        public Ram RAM { get; } = new(0x10000);
        public Z80 CPU { get; }

        public TestRunner()
        {
            this.CPU = new(this, this.ports);
            this._mapping = new(this.RAM, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
            this.RaisedPOWER += this.TestRunner_RaisedPOWER;
            this.LoweringPOWER += this.TestRunner_LoweringPOWER;
        }

        public override MemoryMapping Mapping(ushort _) => this._mapping;

        public override void Initialize()
        {
        }

        private void TestRunner_LoweringPOWER(object? sender, EventArgs e)
        {
            this.CPU.LowerPOWER();
        }

        private void TestRunner_RaisedPOWER(object? sender, EventArgs e)
        {
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseHALT();
            this.CPU.RaiseNMI();
        }
    }
}
