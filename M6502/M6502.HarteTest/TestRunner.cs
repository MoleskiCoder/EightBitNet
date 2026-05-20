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
        }

        public override void Initialize()
        {
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override MemoryMapping Mapping(ushort _) => this._mapping;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
            this.CPU.RaiseSO();
            this.CPU.RaiseRDY();
        }
    }
}
