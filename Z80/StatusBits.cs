// <copyright file="StatusBits.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80
{
    using EightBit;

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

#pragma warning disable CA1069 // Enums values should not be duplicated

        // Parity
        PF = Bits.Bit2,

        // Overflow
        VF = Bits.Bit2,

#pragma warning restore CA1069 // Enums values should not be duplicated

        // Negative?
        NF = Bits.Bit1,

        // Carry
        CF = Bits.Bit0,
    }
}
