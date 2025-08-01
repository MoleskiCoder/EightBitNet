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
        }

    }
}
