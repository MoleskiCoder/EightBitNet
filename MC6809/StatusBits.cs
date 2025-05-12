// <copyright file="StatusBits.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace MC6809
{
    using EightBit;

    [Flags]
    public enum StatusBits
    {
        None = 0,

        // Carry: set if there was a carry from the bit 7 during last add
        // operation, or if there was a borrow from last subtract operation,
        // or if bit 7 of the A register was set during last MUL operation.
        CF = Bits.Bit0,

        // Overflow: set if there was an overflow during last result calculation.
        // Logical, load and store operations clear this bit.
        VF = Bits.Bit1,

        // Zero: set if the result is zero. Like the N bit, this bit can be
        // set not only by arithmetic and logical operations, but also
        // by load / store operations.
        ZF = Bits.Bit2,

        // Negative: set if the most significant bit of the result is set.
        // This bit can be set not only by arithmetic and logical operations,
        // but also by load / store operations.
        NF = Bits.Bit3,

        // Interrupt mask: set if the IRQ interrupt is disabled.
        IF = Bits.Bit4,

        // Half carry: set if there was a carry from bit 3 to bit 4 of the result
        // during the last add operation.
        HF = Bits.Bit5,

        // Fast interrupt mask: set if the FIRQ interrupt is disabled.
        FF = Bits.Bit6,

        // Entire flag: set if the complete machine state was saved in the stack.
        // If this bit is not set then only program counter and condition code
        // registers were saved in the stack. This bit is used by interrupt
        // handling routines only.
        // The bit is cleared by fast interrupts, and set by all other interrupts.
        EF = Bits.Bit7,
    }
}
