// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Bus : IMapper
    {
        private byte data;
        private ushort address;

        public event EventHandler<EventArgs> WritingByte;

        public event EventHandler<EventArgs> WrittenByte;

        public event EventHandler<EventArgs> ReadingByte;

        public event EventHandler<EventArgs> ReadByte;

        public byte Data { get => this.data; set => this.data = value; }

        public ushort Address { get => this.address; set => this.address = value; }

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek() => this.Reference();

        public byte Peek(ushort absolute) => this.Reference(absolute);

        public byte Peek(byte low, byte high) => this.Reference(low, high);

        public void Poke(byte value) => this.Reference() = value;

        public byte Poke(ushort absolute, byte value) => this.Reference(absolute) = value;

        public byte Poke(byte low, byte high, byte value) => this.Reference(low, high) = value;

        public byte Read()
        {
            this.OnReadingByte();
            var returned = this.Data = this.Reference();
            this.OnReadByte();
            return returned;
        }

        public byte Read(ushort absolute)
        {
            this.Address = absolute;
            return this.Read();
        }

        public byte Read(byte low, byte high) => this.Read(Chip.MakeWord(low, high));

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
            this.Address = absolute;
            this.Write(value);
        }

        public void Write(byte low, byte high, byte value) => this.Write(Chip.MakeWord(low, high), value);

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

        protected ref byte Reference() => ref this.Reference(this.Address);

        protected ref byte Reference(byte low, byte high) => ref this.Reference(Chip.MakeWord(low, high));

        ////[[nodiscard]] static std::map<uint16_t, std::vector<uint8_t>> parseHexFile(std::string path);
        ////void loadHexFile(std::string path);
    }
}
