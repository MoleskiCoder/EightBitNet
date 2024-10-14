// <copyright file="ObjectAttribute.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    public class ObjectAttribute
    {
        public ObjectAttribute()
        {
        }

        public ObjectAttribute(Ram ram, ushort address)
        {
            this.PositionY = ram.Peek(address);
            this.PositionX = ram.Peek(++address);
            this.Pattern = ram.Peek(++address);
            this.Flags = ram.Peek(++address);
        }

        public byte PositionY { get; }

        public byte PositionX { get; }

        public byte Pattern { get; }

        public byte Flags { get; }

        public int Priority => this.Flags & (byte)Bits.Bit7;

        public bool HighPriority => this.Priority != 0;

        public bool LowPriority => this.Priority == 0;

        public bool FlipY => (this.Flags & (byte)Bits.Bit6) != 0;

        public bool FlipX => (this.Flags & (byte)Bits.Bit5) != 0;

        public int Palette => (this.Flags & (byte)Bits.Bit4) >> 4;
    }
}
