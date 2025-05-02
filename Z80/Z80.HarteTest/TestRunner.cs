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
        }

        public override MemoryMapping Mapping(ushort _) => this._mapping;

        public override void Initialize()
        {
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }


        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseHALT();
            this.CPU.RaiseNMI();
        }
    }
}
