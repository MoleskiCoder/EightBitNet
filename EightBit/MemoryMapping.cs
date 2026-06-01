// <copyright file="MemoryMapping.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class MemoryMapping
    {
        public MemoryMapping(Memory memory, ushort begin, ushort mask, AccessLevel access)
        {
            this._memory = memory;
            this._begin = begin;
            this._mask = mask;
            this._access = access;
            for (int i = ushort.MinValue; i < ushort.MaxValue + 1; ++i)
            {
                System.Diagnostics.Debug.Assert(i >= ushort.MinValue);
                System.Diagnostics.Debug.Assert(i <= ushort.MaxValue);
                this._offsets[i] = this.CalculateOffset((ushort)i);
            }
        }

        public MemoryMapping(Memory memory, ushort begin, Mask mask, AccessLevel access)
        : this(memory, begin, (ushort)mask, access)
        {
        }

        private readonly ushort[] _offsets = new ushort[ushort.MaxValue + 1];
        private readonly Memory _memory;
        private readonly ushort _begin;
        private readonly ushort _mask;
        private readonly AccessLevel _access;

        public Memory Memory => this._memory;

        public ushort Begin => this._begin;

        public ushort Mask => this._mask;

        public AccessLevel Access => this._access;

        private ushort CalculateOffset(ushort absolute) => (ushort)((absolute - this.Begin) & this.Mask);

        public ushort Offset(ushort absolute) => this._offsets[absolute];
    }
}
