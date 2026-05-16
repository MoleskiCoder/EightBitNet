// <copyright file="Chip.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Runtime.CompilerServices;

    public class Chip : Device
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Bit(int which) => (byte)(1 << which);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Bit(byte which) => Bit((int)which);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBit(byte input, byte which) => (byte)(input | which);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBit(byte input, byte which, int condition) => SetBit(input, which, condition != 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBit(byte input, byte which, bool condition) => condition ? SetBit(input, which) : ClearBit(input, which);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClearBit(byte input, byte which) => (byte)(input & (byte)~which);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClearBit(byte input, byte which, int condition) => ClearBit(input, which, condition != 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClearBit(byte input, byte which, bool condition) => SetBit(input, which, !condition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte HighByte(int value) => (byte)(value >> 8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort HighShort(uint value) => (ushort)(value >> 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte HighByte(ushort value) => HighByte((int)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LowByte(int value) => (byte)(value & (int)Mask.Eight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort LowShort(uint value) => (ushort)(value & (int)Mask.Sixteen);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LowByte(ushort value) => LowByte((int)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort PromoteByte(byte value) => (ushort)(value << 8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint PromoteShort(ushort value) => (uint)(value << 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte DemoteByte(ushort value) => HighByte(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort HigherPart(ushort value) => (ushort)(value & 0xff00);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LowerPart(ushort value) => LowByte(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort MakeShort(byte low, byte high) => (ushort)(PromoteByte(high) | low);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MakeInteger(ushort low, ushort high) => (uint)(PromoteShort(high) | low);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HighNibble(byte value) => value >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LowNibble(byte value) => value & 0xf;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HigherNibble(byte value) => value & 0xf0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LowerNibble(byte value) => LowNibble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PromoteNibble(byte value) => LowByte(value << 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DemoteNibble(byte value) => HighNibble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountBits(uint value) => System.Numerics.BitOperations.PopCount(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EvenParity(uint value) => CountBits(value) % 2 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindFirstSet(uint value) => value == 0 ? 0 : System.Numerics.BitOperations.TrailingZeroCount(value) + 1;
    }
}
