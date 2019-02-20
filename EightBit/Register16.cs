// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    [DebuggerDisplay("Word = {Word}")]
    public struct Register16
    {
        public byte Low;
        public byte High;

        public Register16(byte low, byte high)
        {
            this.Low = low;
            this.High = high;
        }

        public Register16(ushort value)
        {
            this.Low = Chip.LowByte(value);
            this.High = Chip.HighByte(value);
        }

        public Register16(int value)
        : this((ushort)value)
        {
        }

        public Register16(byte low)
        : this(low, 0)
        {
        }

        public Register16(Register16 rhs)
        {
            this.Low = rhs.Low;
            this.High = rhs.High;
        }

        public ushort Word
        {
            get
            {
                return (ushort)(this.Low | Chip.PromoteByte(this.High));
            }

            set
            {
                this.Low = Chip.LowByte(value);
                this.High = Chip.HighByte(value);
            }
        }

        public static Register16 operator ++(Register16 value) => Increment(value);

        public static Register16 operator --(Register16 value) => Decrement(value);

        public static bool operator ==(Register16 left, Register16 right) => left.Equals(right);

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public static Register16 Increment(Register16 value)
        {
            ++value.Word;
            return value;
        }

        public static Register16 Decrement(Register16 value)
        {
            --value.Word;
            return value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Register16))
            {
                return false;
            }

            var rhs = (Register16)obj;
            return rhs.Low == this.Low && rhs.High == this.High;
        }

        public override int GetHashCode() => this.Word;
    }
}
