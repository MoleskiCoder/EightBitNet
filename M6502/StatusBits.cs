// <copyright file="StatusBits.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    [Flags]
    public enum StatusBits : byte
    {
        None = 0,

        // Carry
        CF = Bits.Bit0,

        // Zero
        ZF = Bits.Bit1,

        // I (IRQ disable)
        IF = Bits.Bit2,

        // D (use BCD for arithmetic)
        DF = Bits.Bit3,

        // Brk
        BF = Bits.Bit4,

        // reserved
        RF = Bits.Bit5,

        // Overflow
        VF = Bits.Bit6,

        // Negative
        NF = Bits.Bit7,
    }
}
