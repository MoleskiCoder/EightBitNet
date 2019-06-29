// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public abstract class Bus : IMapper
    {
        private byte data;

        public event EventHandler<EventArgs> WritingByte;

        public event EventHandler<EventArgs> WrittenByte;

        public event EventHandler<EventArgs> ReadingByte;

        public event EventHandler<EventArgs> ReadByte;

        public byte Data { get => this.data; set => this.data = value; }

        public Register16 Address { get; } = new Register16();

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek() => this.Reference();

        public byte Peek(ushort absolute) => this.Reference(absolute);

        public byte Peek(Register16 absolute) => this.Peek(absolute.Word);

        public byte Peek(byte low, byte high) => this.Reference(low, high);

        public void Poke(byte value) => this.Reference() = value;

        public void Poke(ushort absolute, byte value) => this.Reference(absolute) = value;

        public void Poke(Register16 absolute, byte value) => this.Poke(absolute.Word, value);

        public void Poke(byte low, byte high, byte value) => this.Reference(low, high) = value;

        public byte Read()
        {
            this.OnReadingByte();
            var returned = this.Data = this.Reference();
            this.OnReadByte();
            return returned;
        }

        public byte Read(ushort absolute)
        {
            this.Address.Word = absolute;
            return this.Read();
        }

        public byte Read(Register16 absolute) => this.Read(absolute.Word);

        public byte Read(byte low, byte high)
        {
            this.Address.Low = low;
            this.Address.High = high;
            return this.Read();
        }

        public void Write()
        {
            this.OnWritingByte();
            this.Reference() = this.Data;
            this.OnWrittenByte();
        }

        public void Write(byte value)
        {
            this.Data = value;
            this.Write();
        }

        public void Write(ushort absolute, byte value)
        {
            this.Address.Word = absolute;
            this.Write(value);
        }

        public void Write(Register16 absolute, byte value) => this.Write(absolute.Word, value);

        public void Write(byte low, byte high, byte value)
        {
            this.Address.Low = low;
            this.Address.High = high;
            this.Write(value);
        }

        public virtual void RaisePOWER()
        {
        }

        public virtual void LowerPOWER()
        {
        }

        public abstract void Initialize();

        protected virtual void OnWritingByte() => this.WritingByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnWrittenByte() => this.WrittenByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadingByte() => this.ReadingByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadByte() => this.ReadByte?.Invoke(this, EventArgs.Empty);

        protected ref byte Reference(ushort absolute)
        {
            var mapped = this.Mapping(absolute);
            var offset = (ushort)((absolute - mapped.Begin) & mapped.Mask);
            if (mapped.Access == AccessLevel.ReadOnly)
            {
                this.Data = mapped.Memory.Peek(offset);
                return ref this.data;
            }

            return ref mapped.Memory.Reference(offset);
        }

        protected ref byte Reference(Register16 absolute) => ref this.Reference(absolute.Word);

        protected ref byte Reference() => ref this.Reference(this.Address);

        protected ref byte Reference(byte low, byte high) => ref this.Reference(new Register16(low, high).Word);

        protected void LoadHexFile(string path)
        {
            using (var file = new IntelHexFile(path))
            {
                foreach (var chunk in file.Parse())
                {
                    var address = chunk.Item1;
                    var content = chunk.Item2;
                    var mapped = this.Mapping(address);
                    var offset = address - mapped.Begin;
                    mapped.Memory.Load(content.ToArray(), offset);
                }
            }
        }
    }
}
