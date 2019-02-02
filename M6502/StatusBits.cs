namespace EightBit
{
    using System;

    [Flags]
    public enum StatusBits : byte
    {
        // Negative
        NF = Bits.Bit7,

        // Overflow
        VF = Bits.Bit6,

        // reserved
        RF = Bits.Bit5,

        // Brk
        BF = Bits.Bit4,

        // D (use BCD for arithmetic)
        DF = Bits.Bit3,

        // I (IRQ disable)
        IF = Bits.Bit2,

        // Zero
        ZF = Bits.Bit1,

        // Carry
        CF = Bits.Bit0,
   }
}
