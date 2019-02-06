// <copyright file="MemoryMapping.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class MemoryMapping
    {
        public MemoryMapping(Memory memory, ushort begin, ushort mask, AccessLevel access)
        {
            this.Memory = memory;
            this.Begin = begin;
            this.Mask = mask;
            this.Access = access;
        }

        public Memory Memory { get; set; }

        public ushort Begin { get; set; }

        public ushort Mask { get; set; }

        public AccessLevel Access { get; set; }
    }
}
