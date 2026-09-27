namespace M6502.HarteTest
{
    using EightBit;

    internal sealed class TestRunner : Bus
    {
        public Ram RAM { get; } = new(0x10000);

        public Core CPU { get; }

        private readonly MemoryMapping _mapping;

        public TestRunner(Func<TestRunner, Core> cpuFactory)
        {
            this.CPU = cpuFactory(this);
            this._mapping = new(this.RAM, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
            this.RaisedPOWER += this.TestRunner_RaisedPOWER;
            this.LoweringPOWER += this.TestRunner_LoweringPOWER;
        }

        private void TestRunner_RaisedPOWER(object? sender, EventArgs e)
        {
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
            this.CPU.RaiseSO();
            this.CPU.RaiseRDY();
        }

        private void TestRunner_LoweringPOWER(object? sender, EventArgs e)
        {
            this.CPU.LowerPOWER();
        }

        public override void Initialize()
        {
        }

        public override MemoryMapping Mapping(ushort _) => this._mapping;
    }
}
