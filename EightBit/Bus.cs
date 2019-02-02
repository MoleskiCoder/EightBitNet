namespace EightBit
{
    using System;

    abstract public class Bus : IMapper
    {
        private byte data;
        private Register16 address = new Register16();

        public event EventHandler<EventArgs> WritingByte;
        public event EventHandler<EventArgs> WrittenByte;

        public event EventHandler<EventArgs> ReadingByte;
        public event EventHandler<EventArgs> ReadByte;

        public byte Data
        {
            get { return data; }
            set { data = value; }
        }

        public Register16 Address
        {
            get { return address; }
        }

        public abstract MemoryMapping Mapping(Register16 absolute);

        public byte Peek() => Reference();
        public byte Peek(Register16 absolute) => Reference(absolute);
        public byte Peek(ushort absolute) => Peek(Chip.LowByte(absolute), Chip.HighByte(absolute));
        public byte Peek(byte low, byte high) => Reference(new Register16(low, high));

        public void Poke(byte value) => Reference() = value;
        public void Poke(Register16 absolute, byte value) => Reference(absolute) = value;
        public byte Poke(ushort absolute, byte value) => Poke(Chip.LowByte(absolute), Chip.HighByte(absolute), value);
        public byte Poke(byte low, byte high, byte value) => Reference(new Register16(low, high)) = value;

        public byte Read()
        {
            OnReadingByte();
            var returned = Data = Reference();
            OnReadByte();
            return returned;
        }

        public byte Read(Register16 absolute)
        {
            Address.Word = absolute.Word;
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

        public void Write(Register16 absolute, byte value)
        {
            Address.Word = absolute.Word;
            Write(value);
        }

        public virtual void RaisePOWER() {}
        public virtual void LowerPOWER() {}

        public abstract void Initialize();

        protected virtual void OnWritingByte() => WritingByte?.Invoke(this, EventArgs.Empty);
        protected virtual void OnWrittenByte() => WrittenByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadingByte() => ReadingByte?.Invoke(this, EventArgs.Empty);
        protected virtual void OnReadByte() => ReadByte?.Invoke(this, EventArgs.Empty);

        protected ref byte Reference(Register16 absolute)
        {
            var mapped = Mapping(absolute);
            var offset = (ushort)((absolute - mapped.Begin) & mapped.Mask);
            if (mapped.Access == AccessLevel.ReadOnly)
            {
                Data = mapped.Memory.Peek(offset);
                return ref data;
            }
            return ref mapped.Memory.Reference(offset);
        }

        protected ref byte Reference() => ref Reference(Address);

        protected ref byte Reference(ushort absolute) => ref Reference(Chip.LowByte(absolute), Chip.HighByte(absolute));

        protected ref byte Reference(byte low, byte high)
        {
            Address.Low = low;
            Address.High = high;
            return ref Reference();
        }

        //[[nodiscard]] static std::map<uint16_t, std::vector<uint8_t>> parseHexFile(std::string path);
        //void loadHexFile(std::string path);
    }
}
