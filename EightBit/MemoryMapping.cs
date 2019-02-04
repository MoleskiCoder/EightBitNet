// <copyright file="MemoryMapping.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class MemoryMapping
    {
        private Memory memory;
        private ushort begin;
        private ushort mask;
        private AccessLevel access;

        public MemoryMapping(Memory memory, ushort begin, ushort mask, AccessLevel access)
        {
            this.memory = memory;
            this.begin = begin;
            this.mask = mask;
            this.access = access;
        }

        public Memory Memory { get => this.memory; set => this.memory = value; }

        public ushort Begin { get => this.begin; set => this.begin = value; }

        public ushort Mask { get => this.mask; set => this.mask = value; }

        public AccessLevel Access { get => this.access; set => this.access = value; }
    }
}
