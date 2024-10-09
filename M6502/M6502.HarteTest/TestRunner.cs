namespace M6502.HarteTest
{
    using EightBit;

    internal sealed class TestRunner : Bus
    {
        public Ram RAM { get; } = new(0x10000);

        public MOS6502 CPU { get; }
        //public WDC65C02 CPU { get; }

        private readonly MemoryMapping _mapping;

        public TestRunner()
        {
            CPU = new(this);
            _mapping = new(RAM, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
        }

        public override void Initialize()
        {
        }

        public override void LowerPOWER()
        {
            CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override MemoryMapping Mapping(ushort _) => _mapping;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            CPU.RaisePOWER();
            CPU.RaiseRESET();
            CPU.RaiseINT();
            CPU.RaiseNMI();
            CPU.RaiseSO();
            CPU.RaiseRDY();
        }
    }
}
