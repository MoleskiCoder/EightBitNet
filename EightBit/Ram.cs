// <copyright file="Ram.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Ram : Rom
    {
        public Ram(int size = 0)
        : base(size)
        {
        }

        public override sealed ref byte Reference(ushort address) => ref this.Bytes()[address];

        public new void Poke(ushort address, byte value) => base.Poke(address, value);
    }
}
