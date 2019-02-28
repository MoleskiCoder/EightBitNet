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

        // Half carry
        AC = Bits.Bit4,

        // Parity
        PF = Bits.Bit2,

        // Carry
        CF = Bits.Bit0,
    }
}
