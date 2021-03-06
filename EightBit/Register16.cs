﻿// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;

    [DebuggerDisplay("Word = {Word}")]
    public class Register16
    {
        private byte low;
        private byte high;

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

        public ref byte Low => ref this.low;

        public ref byte High => ref this.high;

        public static bool operator ==(Register16 left, Register16 right) => left.Equals(right);

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public override bool Equals(object obj)
        {
            var rhs = obj as Register16;
            return rhs == null ? false : rhs.Low == this.Low && rhs.High == this.High;
        }

        public override int GetHashCode() => this.Word;
    }
}
