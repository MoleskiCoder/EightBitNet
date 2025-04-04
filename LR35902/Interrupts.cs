﻿// <copyright file="Interrupts.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    // IF and IE flags
    [Flags]
    public enum Interrupts
    {
        None = 0,
        VerticalBlank = Bits.Bit0,         // VBLANK
        DisplayControlStatus = Bits.Bit1,  // LCDC Status
        TimerOverflow = Bits.Bit2,         // Timer Overflow
        SerialTransfer = Bits.Bit3,        // Serial Transfer
        KeypadPressed = Bits.Bit4,         // Hi-Lo transition of P10-P13
    }
}
