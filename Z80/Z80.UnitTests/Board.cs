// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using EightBit;

    public sealed class Board : Bus
    {
        private readonly Ram ram = new(0x10000);
        private readonly InputOutput ports = new();
        private readonly MemoryMapping mapping;

        public Board()
        {
            this.CPU = new(this, this.ports);
            this.mapping = new(this.ram, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
            this.RaisedPOWER += this.Board_RaisedPOWER;
            this.LoweringPOWER += this.Board_LoweringPOWER;
        }

        public Z80 CPU { get; }

        public override void Initialize()
        {
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void Board_RaisedPOWER(object? sender, EventArgs e)
        {
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
        }

        private void Board_LoweringPOWER(object? sender, EventArgs e)
        {
            this.CPU.LowerPOWER();
        }
    }
}
