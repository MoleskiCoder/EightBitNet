﻿// <copyright file="Ram.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Ram(int size) : Rom(size)
    {
        public Ram()
        : this(0)
        {
        }

        public sealed override ref byte Reference(ushort address) => ref this.Bytes()[address];

        public new void Poke(ushort address, byte value) => base.Poke(address, value);
    }
}
