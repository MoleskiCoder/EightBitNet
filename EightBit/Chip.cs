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

        public static byte Bit(int which) => (byte)(1 << which);

        public static byte Bit(byte which) => Bit((int)which);

        public static byte SetBit(byte input, byte which) => (byte)(input | which);

        public static byte SetBit(byte input, byte which, int condition) => SetBit(input, which, condition != 0);

        public static byte SetBit(byte input, byte which, bool condition) => condition ? SetBit(input, which) : ClearBit(input, which);

        public static byte ClearBit(byte input, byte which) => (byte)(input & (byte)~which);

        public static byte ClearBit(byte input, byte which, int condition) => ClearBit(input, which, condition != 0);

        public static byte ClearBit(byte input, byte which, bool condition) => SetBit(input, which, !condition);

        public static byte HighByte(int value) => (byte)(value >> 8);

        public static byte HighByte(ushort value) => HighByte((int)value);

        public static byte LowByte(int value) => (byte)(value & (int)Mask.Eight);

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

        public static int CountBits(uint value)
        {
            return System.Numerics.BitOperations.PopCount(value);
        }

        public static bool EvenParity(uint value)
        {
            return CountBits(value) % 2 == 0;
        }

        public static int FindFirstSet(uint value)
        {
            if (value == 0)
            {
                return 0;
            }

            return System.Numerics.BitOperations.TrailingZeroCount(value) + 1;
        }
    }
}
