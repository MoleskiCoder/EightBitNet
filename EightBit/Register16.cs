// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public sealed class Register16
    {
        [FieldOffset(0)] private byte _low;
        [FieldOffset(1)] private byte _high;

        public Register16(byte low, byte high)
        {
            this.Low = low;
            this.High = high;
        }

        public Register16(ushort value)
        {
            this.Word = value;
        }

        public Register16()
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

        public unsafe ushort Word
        {
            get
            {
                fixed (byte* bytes = &this._low)
                {
                    return *(ushort*)bytes;
                }
            }
            set
            {
                fixed (byte* bytes = &this._low)
                {
                    *(ushort*)bytes = value;
                }
            }
        }

        public ref byte Low => ref this._low;

        public ref byte High => ref this._high;

        public static bool operator ==(Register16 left, Register16 right) => left.Equals(right);

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public override int GetHashCode() => this.Word;

        public override bool Equals(object? obj) => this.Equals(obj as Register16);

        public bool Equals(Register16? rhs) => ReferenceEquals(this, rhs) || (rhs is not null && rhs.Low == this.Low && rhs.High == this.High);

        public void Assign(byte low, byte high = 0)
        {
            this.Low = low;
            this.High = high;
        }

        public void Assign(Register16 from)
        {
            this.Assign(from.Low, from.High);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Register16 Increment()
        {
            ++this.Word;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Register16 Decrement()
        {
            --this.Word;
            return this;
        }
    }
}
