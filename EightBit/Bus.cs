// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Bus : IMapper
    {
        private byte _data;

        public event EventHandler<EventArgs>? WritingByte;

        public event EventHandler<EventArgs>? WrittenByte;

        public event EventHandler<EventArgs>? ReadingByte;

        public event EventHandler<EventArgs>? ReadByte;

        public ref byte Data => ref _data;

        public Register16 Address { get; } = new();

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek() => Reference();

        public byte Peek(ushort absolute) => Reference(absolute);

        public byte Peek(Register16 absolute)
        {
            ArgumentNullException.ThrowIfNull(absolute);
            return Peek(absolute.Word);
        }

        public void Poke(byte value) => Reference() = value;

        public void Poke(ushort absolute, byte value) => Reference(absolute) = value;

        public void Poke(Register16 absolute, byte value)
        {
            ArgumentNullException.ThrowIfNull(absolute);
            Poke(absolute.Word, value);
        }

        public byte Read()
        {
            OnReadingByte();
            var returned = Data = Reference();
            OnReadByte();
            return returned;
        }

        public byte Read(ushort absolute)
        {
            Address.Word = absolute;
            return Read();
        }

        public byte Read(Register16 absolute)
        {
            ArgumentNullException.ThrowIfNull(absolute);
            return Read(absolute.Low, absolute.High);
        }

        public byte Read(byte low, byte high)
        {
            Address.Assign(low, high);
            return Read();
        }

        public void Write()
        {
            OnWritingByte();
            Reference() = Data;
            OnWrittenByte();
        }

        public void Write(byte value)
        {
            Data = value;
            Write();
        }

        public void Write(ushort absolute, byte value)
        {
            Address.Word = absolute;
            Write(value);
        }

        public void Write(Register16 absolute, byte value)
        {
            ArgumentNullException.ThrowIfNull(absolute);
            Write(absolute.Low, absolute.High, value);
        }

        public void Write(byte low, byte high, byte value)
        {
            Address.Assign(low, high);
            Write(value);
        }

        public virtual void RaisePOWER()
        {
        }

        public virtual void LowerPOWER()
        {
        }

        public abstract void Initialize();

        protected virtual void OnWritingByte() => WritingByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnWrittenByte() => WrittenByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadingByte() => ReadingByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadByte() => ReadByte?.Invoke(this, EventArgs.Empty);

        protected ref byte Reference(ushort absolute)
        {
            var mapped = Mapping(absolute);
            var offset = (ushort)mapped.Offset(absolute);
            if (mapped.Access == AccessLevel.ReadOnly)
            {
                Data = mapped.Memory.Peek(offset);
                return ref _data;
            }

            return ref mapped.Memory.Reference(offset);
        }

        protected ref byte Reference(Register16 absolute)
        {
            ArgumentNullException.ThrowIfNull(absolute);
            return ref Reference(absolute.Word);
        }

        protected ref byte Reference() => ref Reference(Address);

        protected void LoadHexFile(string path)
        {
            var file = new IntelHexFile(path);
            foreach (var (address, content) in file.Parse())
            {
                var mapped = Mapping(address);
                var offset = address - mapped.Begin;
                mapped.Memory.Load(content, offset);
            }
        }
    }
}
