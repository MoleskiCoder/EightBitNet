// <copyright file="StatusBits.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    namespace GameBoy
    {
        [System.Flags]
        public enum StatusBits
        {
            None = 0,
            CF = Bits.Bit4,
            HC = Bits.Bit5,
            NF = Bits.Bit6,
            ZF = Bits.Bit7,
        }
    }
}
