// <copyright file="StatusBits.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    [Flags]
    public enum StatusBits
    {
        // Negative
        SF = Bits.Bit7,

        // Zero
        ZF = Bits.Bit6,

        // Undocumented Y flag
        YF = Bits.Bit5,

        // Half carry
        HC = Bits.Bit4,

        // Undocumented X flag
        XF = Bits.Bit3,

        // Parity
        PF = Bits.Bit2,

        // Zero
        VF = Bits.Bit2,

        // Negative?
        NF = Bits.Bit1,

        // Carry
        CF = Bits.Bit0,
    }
}
