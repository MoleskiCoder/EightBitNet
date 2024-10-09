// <copyright file="MemoryMapping.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class MemoryMapping(Memory memory, ushort begin, ushort mask, AccessLevel access)
    {
        public MemoryMapping(Memory memory, ushort begin, Mask mask, AccessLevel access)
        : this(memory, begin, (ushort)mask, access)
        {
        }

        public Memory Memory { get; set; } = memory;

        public ushort Begin { get; set; } = begin;

        public ushort Mask { get; set; } = mask;

        public AccessLevel Access { get; set; } = access;

        public int Offset(ushort absolute)
        {
            return (absolute - Begin) & Mask;
        }
    }
}
