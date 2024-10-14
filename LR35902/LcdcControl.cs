// <copyright file="LcdcControl.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    [Flags]
    public enum LcdcControls
    {
        None = 0,
        BG_EN = Bits.Bit0,
        OBJ_EN = Bits.Bit1,
        OBJ_SIZE = Bits.Bit2,
        BG_MAP = Bits.Bit3,
        TILE_SEL = Bits.Bit4,
        WIN_EN = Bits.Bit5,
        WIN_MAP = Bits.Bit6,
        LCD_EN = Bits.Bit7,
    }
}
