namespace EightBit
{
    using System;

    abstract public class Bus : IMapper
    {
        private byte data;
        private ushort address;

        public event EventHandler<EventArgs> WritingByte;
        public event EventHandler<EventArgs> WrittenByte;

        public event EventHandler<EventArgs> ReadingByte;
        public event EventHandler<EventArgs> ReadByte;

        public byte Data
        {
            get { return data; }
            set { data = value; }
        }

        public ushort Address
        {
            get { return address; }
            set { address = value; }
        }

        public abstract MemoryMapping Mapping(ushort absolute);

        public byte Peek() => Reference();
        public byte Peek(ushort absolute) => Reference(absolute);
        public byte Peek(byte low, byte high) => Reference(low, high);

        public void Poke(byte value) => Reference() = value;
        public byte Poke(ushort absolute, byte value) => Reference(absolute) = value;
        public byte Poke(byte low, byte high, byte value) => Reference(low, high) = value;

        public byte Read()
        {
            OnReadingByte();
            var returned = Data = Reference();
            OnReadByte();
            return returned;
        }

        public byte Read(ushort absolute)
        {
            Address = absolute;
            return Read();
        }

        public byte Read(byte low, byte high)
        {
            return Read(Chip.MakeWord(low, high));
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
            Address = absolute;
            Write(value);
        }

        public void Write(byte low, byte high, byte value)
        {
            Write(Chip.MakeWord(low, high), value);
        }

        public virtual void RaisePOWER() {}
        public virtual void LowerPOWER() {}

        public abstract void Initialize();

        protected virtual void OnWritingByte() => WritingByte?.Invoke(this, EventArgs.Empty);
        protected virtual void OnWrittenByte() => WrittenByte?.Invoke(this, EventArgs.Empty);

        protected virtual void OnReadingByte() => ReadingByte?.Invoke(this, EventArgs.Empty);
        protected virtual void OnReadByte() => ReadByte?.Invoke(this, EventArgs.Empty);

        protected ref byte Reference(ushort absolute)
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

        protected ref byte Reference(byte low, byte high)
        {
            return ref Reference(Chip.MakeWord(low, high));
        }

        //[[nodiscard]] static std::map<uint16_t, std::vector<uint8_t>> parseHexFile(std::string path);
        //void loadHexFile(std::string path);
    }
}
