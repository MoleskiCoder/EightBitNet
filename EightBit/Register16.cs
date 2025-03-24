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
            this.Low = low;
            this.High = high;
        }

        public Register16(ushort value)
        {
            this.Low = Chip.LowByte(value);
            this.High = Chip.HighByte(value);
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
            this.Low = rhs.Low;
            this.High = rhs.High;
        }

        public ushort Word
        {
            get => Chip.MakeWord(this.Low, this.High);

            set
            {
                this.Low = Chip.LowByte(value);
                this.High = Chip.HighByte(value);
            }
        }

        public ref byte Low => ref this._low;

        public ref byte High => ref this._high;

        public static bool operator ==(Register16 left, Register16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public override int GetHashCode() => this.Word;

        public override bool Equals(object? obj) => this.Equals(obj as Register16);

        public bool Equals(Register16? rhs) => ReferenceEquals(this, rhs) || (rhs is not null && rhs.Low == this.Low && rhs.High == this.High);

        public void Assign(byte low, byte high)
        {
            this._low = low;
            this._high = high;
        }

        public void Assign(Register16 from)
        {
            this.Assign(from._low, from._high);
        }
    }
}
