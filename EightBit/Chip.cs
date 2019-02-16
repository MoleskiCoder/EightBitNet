// <copyright file="Chip.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Chip : Device
    {
        protected Chip()
        {
        }

        public static byte SetFlag(byte input, byte flag) => (byte)(input | flag);

        public static byte SetFlag(byte input, byte flag, int condition) => SetFlag(input, flag, condition != 0);

        public static byte SetFlag(byte input, byte flag, bool condition) => condition ? SetFlag(input, flag) : ClearFlag(input, flag);

        public static byte ClearFlag(byte input, byte flag) => (byte)(input & (byte)~flag);

        public static byte ClearFlag(byte input, byte flag, int condition) => ClearFlag(input, flag, condition != 0);

        public static byte ClearFlag(byte input, byte flag, bool condition) => SetFlag(input, flag, !condition);

        public static byte HighByte(int value) => (byte)(value >> 8);

        public static byte HighByte(ushort value) => HighByte((int)value);

        public static byte LowByte(int value) => (byte)(value & (int)Mask.Mask8);

        public static byte LowByte(ushort value) => LowByte((int)value);

        public static ushort PromoteByte(byte value) => (ushort)(value << 8);

        public static byte DemoteByte(ushort value) => HighByte(value);

        public static ushort HigherPart(ushort value) => (ushort)(value & 0xff00);

        public static byte LowerPart(ushort value) => LowByte(value);

        public static ushort MakeWord(byte low, byte high) => (ushort)(PromoteByte(high) | low);

        public static int HighNibble(byte value) => value >> 4;

        public static int LowNibble(byte value) => value & 0xf;

        public static int HigherNibble(byte value) => value & 0xf0;

        public static int LowerNibble(byte value) => LowNibble(value);

        public static int PromoteNibble(byte value) => LowByte(value << 4);

        public static int DemoteNibble(byte value) => HighNibble(value);

        public static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                ++count;
                value &= value - 1;
            }

            return count;
        }

        public static int CountBits(byte value) => CountBits((int)value);

        public static bool EvenParity(int value) => CountBits(value) % 2 == 0;

        public static bool EvenParity(byte value) => EvenParity((int)value);
    }
}
