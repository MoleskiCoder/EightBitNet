// <copyright file="Register16.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EightBit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Register16
    {
        // _low and _high must remain adjacent and in this order
        private byte _low;
        private byte _high;

        // Whole overlays low/high as a single little-endian ushort.
        private ref ushort Whole
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.As<byte, ushort>(ref this._low);
        }

        public Register16(byte low, byte high)
        {
            this.Low = low;
            this.High = high;
        }

        public Register16(ushort value) => this.Joined = value;

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
            Debug.Assert(rhs is not null, "rhs cannot be null");
            this.Low = rhs.Low;
            this.High = rhs.High;
        }

        public ushort Joined
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (BitConverter.IsLittleEndian)
                {
                    return this.Whole;
                }
                else
                {
                    return Chip.MakeShort(this.Low, this.High);
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (BitConverter.IsLittleEndian)
                {
                    this.Whole = value;
                }
                else
                {
                    this.Low = Chip.LowByte(value);
                    this.High = Chip.HighByte(value);
                }
            }
        }

        public ref byte Low => ref this._low;

        public ref byte High => ref this._high;

        public static bool operator ==(Register16 left, Register16 right)
        {
            Debug.Assert(left is not null, "left cannot be null");
            return left.Equals(right);
        }

        public bool Zero => this.Joined == 0;

        public bool NonZero => !this.Zero;

        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public override int GetHashCode() => this.Joined;

        public override bool Equals(object? obj) => this.Equals(obj as Register16);

        public bool Equals(Register16? rhs)
        {
            Debug.Assert(rhs is not null, "rhs cannot be null");
            return this.Joined == rhs.Joined;
        }

        public void Assign(byte low, byte high = 0)
        {
            this.Low = low;
            this.High = high;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assign(Register16 from)
        {
            Debug.Assert(from is not null, "from cannot be null");
            if (BitConverter.IsLittleEndian)
            {
                this.Whole = from.Whole;
            }
            else
            {
                this.Low = from.Low;
                this.High = from.High;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Increment() => ++this.Joined;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decrement() => --this.Joined;
    }
}
