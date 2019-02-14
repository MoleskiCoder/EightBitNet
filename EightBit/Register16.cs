// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Runtime.InteropServices;

    // This'll only work for little endian host processors...
    [StructLayout(LayoutKind.Explicit)]
    public struct Register16
    {
        [FieldOffset(0)]
        public byte Low;

        [FieldOffset(1)]
        public byte High;

        [FieldOffset(0)]
        public ushort Word;

        public Register16(byte low, byte high)
        {
            this.Word = 0;
            this.Low = low;
            this.High = high;
        }

        public Register16(ushort value)
        : this(Chip.LowByte(value), Chip.HighByte(value))
        {
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
            this.Low = 0;
            this.High = 0;
            this.Word = rhs.Word;
        }

        public static Register16 operator ++(Register16 value)
        {
            return Increment(value);
        }

        public static Register16 operator --(Register16 value)
        {
            return Decrement(value);
        }

        public static bool operator ==(Register16 left, Register16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Register16 left, Register16 right)
        {
            return !(left == right);
        }

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

            Register16 rhs = (Register16)obj;
            return rhs.Word == this.Word;
        }

        public override int GetHashCode()
        {
            return this.Word;
        }
    }
}
