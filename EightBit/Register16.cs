// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;

    [DebuggerDisplay("Word = {Word}")]
    public sealed class Register16
    {
        private byte _low;
        private byte _high;

        public Register16(byte low, byte high)
        {
            Low = low;
            High = high;
        }

        public Register16(ushort value)
        {
            Low = Chip.LowByte(value);
            High = Chip.HighByte(value);
        }

        public Register16()
        : this((ushort)0)
        {
        }

        public Register16(int value)
        : this((ushort)value)
        {
        }

        public Register16(uint value)
        : this((ushort)value)
        {
        }

        public Register16(byte low)
        : this(low, 0)
        {
        }

        public Register16(Register16 rhs)
        {
            ArgumentNullException.ThrowIfNull(rhs);
            Low = rhs.Low;
            High = rhs.High;
        }

        public ushort Word
        {
            get => Chip.MakeWord(Low, High);

            set
            {
                Low = Chip.LowByte(value);
                High = Chip.HighByte(value);
            }
        }

        public ref byte Low => ref _low;

        public ref byte High => ref _high;

        public static bool operator ==(Register16 left, Register16 right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.Equals(right);
        }

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public override int GetHashCode() => Word;

        public override bool Equals(object? obj)
        {
            return Equals(obj as Register16);
        }

        public bool Equals(Register16? rhs)
        {
            if (ReferenceEquals(this, rhs))
            {
                return true;
            }

            if (rhs is null)
            {
                return false;
            }

            return rhs.Low == Low && rhs.High == High;
        }

        public void Assign(byte low, byte high)
        {
            _low = low;
            _high = high;
        }

        public void Assign(Register16 from)
        {
            ArgumentNullException.ThrowIfNull(from);
            Assign(from._low, from._high);
        }
    }
}
