// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using EightBit;

    public sealed class Board : Bus
    {
        private readonly Ram ram = new(0x10000);  // 0000 - FFFF, 64K RAM
        private readonly MemoryMapping mapping;

        public Board()
        {
            this.CPU = new(this);
            this.mapping = new(this.ram, 0x0000, 0xffff, AccessLevel.ReadWrite);
            this.RaisedPOWER += this.Board_RaisedPOWER;
            this.LoweringPOWER += this.Board_LoweringPOWER;
        }

        public MC6809 CPU { get; }

        public override void Initialize()
        {
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void Board_RaisedPOWER(object? sender, EventArgs e)
        {
            this.CPU.RaisePOWER();

            this.CPU.LowerRESET();
            this.CPU.RaiseINT();

            this.CPU.RaiseNMI();
            this.CPU.RaiseFIRQ();
            this.CPU.RaiseHALT();

            this.RunPowerOnReset();
        }

        private void Board_LoweringPOWER(object? sender, EventArgs e)
        {
            this.CPU.LowerPOWER();
        }

        private void RunPowerOnReset()
        {
            this.CPU.RaiseRESET();
            this.CPU.LowerRESET();
            this.CPU.Step();
            this.CPU.RaiseRESET();
        }
    }
}
