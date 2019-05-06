// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class Board : EightBit.Bus
    {
        private readonly Ram ram = new Ram(0x10000);  // 0000 - FFFF, 64K RAM
        private readonly MemoryMapping mapping;

        public Board()
        {
            this.CPU = new MC6809(this);
            this.mapping = new MemoryMapping(this.ram, 0x0000, 0xffff, EightBit.AccessLevel.ReadWrite);
        }

        public MC6809 CPU { get; }

        public override void Initialize()
        {
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            this.CPU.RaisePOWER();

            this.CPU.LowerRESET();
            this.CPU.RaiseINT();

            this.CPU.RaiseNMI();
            this.CPU.RaiseFIRQ();
            this.CPU.RaiseHALT();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }
    }
}
