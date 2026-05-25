// <copyright file="Ricoh2A03.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502
{
    using EightBit;

    public class Ricoh2A03(Bus bus) : MOS6502(bus)
    {
        protected override void ARR() => this.BinaryARR();

        protected override byte DecimalSUB(byte operand, int borrow) => base.BinarySUB(operand, borrow);

        protected override byte DecimalADC(byte data) => base.BinaryADC(data);
    }
}
