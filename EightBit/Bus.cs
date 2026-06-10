// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EightBit
{
    public abstract class Bus : IMapper
    {
        private bool _writing;
        private byte _data;

        public ref byte Data => ref this._data;

        public Register16 Address { get; } = new();

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek(ushort absolute) => this.Reference(absolute);

        public byte Peek(Register16 absolute)
        {
            Debug.Assert(absolute is not null, "absolute cannot be null");
            return this.Peek(absolute.Joined);
        }

        public void Poke(byte value) => this.Reference() = value;

        public void Poke(ushort absolute, byte value) => this.Reference(absolute) = value;

        public void Poke(Register16 absolute, byte value)
        {
            Debug.Assert(absolute is not null, "absolute cannot be null");
            this.Poke(absolute.Joined, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read()
        {
            Debug.Assert(!this._writing, "Writing flag is in an invalid state");
            this.Data = this.Reference();
            Debug.Assert(!this._writing, "Writing flag is in an invalid state");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write()
        {
            Debug.Assert(!this._writing, "Writing flag is in an invalid state");
            this._writing = true;
            this.Reference() = this.Data;
            this._writing = false;
            Debug.Assert(!this._writing, "Writing flag is in an invalid state");
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaisePOWER()
        {
        }

        public virtual void LowerPOWER()
        {
        }

        public abstract void Initialize();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref byte Reference(ushort absolute)
        {
            var mapped = this.Mapping(absolute);
            var offset = mapped.Offset(absolute);
            if (mapped.Access != AccessLevel.ReadOnly)
            {
                return ref mapped.Memory.Reference(offset);
            }
            if (!this._writing)
            {
                this.Data = mapped.Memory.Peek(offset);
            }
            return ref this._data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref byte Reference() => ref this.Reference(this.Address.Joined);

        protected void LoadHexFile(string path)
        {
            var file = new IntelHexFile(path);
            foreach (var (address, content) in file.Parse())
            {
                var mapped = this.Mapping(address);
                var offset = address - mapped.Begin;
                mapped.Memory.Load(content, offset);
            }
        }
    }
}
