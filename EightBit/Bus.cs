// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class Bus : IMapper
    {
        private byte _data;

        public event EventHandler<EventArgs>? WritingByte;

        public event EventHandler<EventArgs>? WrittenByte;

        public event EventHandler<EventArgs>? ReadingByte;

        public event EventHandler<EventArgs>? ReadByte;

        public ref byte Data => ref this._data;

        public Register16 Address { get; } = new();

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek() => this.Reference();

        public byte Peek(ushort absolute) => this.Reference(absolute);

        public byte Peek(Register16 absolute)
        {
            return this.Peek(absolute.Word);
        }

        public void Poke(byte value) => this.Reference() = value;

        public void Poke(ushort absolute, byte value) => this.Reference(absolute) = value;

        public void Poke(Register16 absolute, byte value)
        {
            this.Poke(absolute.Word, value);
        }

        public byte Read()
        {
            this.ReadingByte?.Invoke(this, EventArgs.Empty);
            this.Data = this.Reference();
            ReadByte?.Invoke(this, EventArgs.Empty);
            return this.Data;
        }

        public byte Read(ushort absolute)
        {
            this.Address.Word = absolute;
            return this.Read();
        }

        public byte Read(Register16 absolute)
        {
            this.Address.Assign(absolute);
            return this.Read();
        }

        public byte Read(byte low, byte high)
        {
            this.Address.Assign(low, high);
            return this.Read();
        }

        public void Write()
        {
            this.WritingByte?.Invoke(this, EventArgs.Empty);
            this.Reference() = this.Data;
            this.WrittenByte?.Invoke(this, EventArgs.Empty);
        }

        public void Write(byte value)
        {
            this.Data = value;
            this.Write();
        }

        public void Write(ushort absolute, byte value)
        {
            this.Address.Word = absolute;
            this.Data = value;
            this.Write();
        }

        public void Write(Register16 absolute, byte value)
        {
            this.Address.Assign(absolute);
            this.Data = value;
            this.Write();
        }

        public void Write(byte low, byte high, byte value)
        {
            this.Address.Assign(low, high);
            this.Data = value;
            this.Write();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaisePOWER()
        {
        }

        public virtual void LowerPOWER()
        {
        }

        public abstract void Initialize();

        protected ref byte Reference(ushort absolute)
        {
            var mapped = this.Mapping(absolute);
            var offset = (ushort)mapped.Offset(absolute);
            if (mapped.Access == AccessLevel.ReadOnly)
            {
                this.Data = mapped.Memory.Peek(offset);
                return ref this._data;
            }

            return ref mapped.Memory.Reference(offset);
        }

        protected ref byte Reference() => ref this.Reference(this.Address.Word);

        protected void LoadHexFile(string path)
        {
            var file = new IntelHexFile(path);
            foreach (var (address, content) in file.Parse())
            {
                var mapped = this.Mapping(address);
                var offset = address - mapped.Begin;
                _ = mapped.Memory.Load(content, offset);
            }
        }
    }
}
