// <copyright file="RefreshRegister.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public struct RefreshRegister(byte value) : System.IEquatable<RefreshRegister>
    {
        private readonly byte high = (byte)(value & (byte)Bits.Bit7);
        private byte variable = (byte)(value & (byte)Mask.Seven);

        public static implicit operator byte(RefreshRegister input) => ToByte(input);

        public static implicit operator RefreshRegister(byte input) => FromByte(input);

        public static RefreshRegister operator ++(RefreshRegister value) => Increment(value);

        public static bool operator ==(RefreshRegister left, RefreshRegister right) => left.Equals(right);

        public static bool operator !=(RefreshRegister left, RefreshRegister right) => !(left == right);

        public static byte ToByte(RefreshRegister input) => (byte)(input.high | (input.variable & (byte)Mask.Seven));

        public static RefreshRegister Increment(RefreshRegister value)
        {
            ++value.variable;
            return value;
        }

        public static RefreshRegister FromByte(byte input) => new(input);

        public readonly byte ToByte() => ToByte(this);

        public override readonly bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return this.Equals((RefreshRegister)obj);
        }
        public readonly bool Equals(RefreshRegister other) => other.high == this.high && other.variable == this.variable;

        public override readonly int GetHashCode() => this.high + this.variable;

    }
}
