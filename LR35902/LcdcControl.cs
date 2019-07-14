// <copyright file="LcdcControl.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    namespace GameBoy
    {
        [System.Flags]
        public enum LcdcControl
        {
            None = 0,
            DisplayBackground = Bits.Bit0,
            ObjectEnable = Bits.Bit1,
            ObjectBlockCompositionSelection = Bits.Bit2,
            BackgroundCodeAreaSelection = Bits.Bit3,
            BackgroundCharacterDataSelection = Bits.Bit4,
            WindowEnable = Bits.Bit5,
            WindowCodeAreaSelection = Bits.Bit6,
            LcdEnable = Bits.Bit7
        }
    }
}
