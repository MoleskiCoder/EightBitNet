namespace EightBit
{
    using System;

    [Flags]
    public enum Bits
    {
        Bit0 = 1,
        Bit1 = Bit0 << 1,
        Bit2 = Bit1 << 1,
        Bit3 = Bit2 << 1,
        Bit4 = Bit3 << 1,
        Bit5 = Bit4 << 1,
        Bit6 = Bit5 << 1,
        Bit7 = Bit6 << 1,
        Bit8 = Bit7 << 1,
        Bit9 = Bit8 << 1,
        Bit10 = Bit9 << 1,
        Bit11 = Bit10 << 1,
        Bit12 = Bit11 << 1,
        Bit13 = Bit12 << 1,
        Bit14 = Bit13 << 1,
        Bit15 = Bit14 << 1,
        Bit16 = Bit15 << 1
    }
}
