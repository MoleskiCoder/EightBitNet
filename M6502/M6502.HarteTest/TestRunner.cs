using EightBit;

namespace M6502.HarteTest
{
    internal class TestRunner : EightBit.Bus
    {
        public EightBit.Ram RAM { get; } = new(0x10000);

        public EightBit.M6502 CPU { get; }

        private readonly MemoryMapping mapping;

        public TestRunner()
        {
            this.CPU = new(this);
            this.mapping = new(this.RAM, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
        }

        public override void Initialize()
        {
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

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
