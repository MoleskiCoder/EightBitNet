namespace EightBit
{
    public abstract class BigEndianProcessor : Processor
    {
        protected BigEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override ushort PeekWord(ushort address)
        {
            var high = Bus.Peek(address);
            var low = Bus.Peek(++address);
            return MakeWord(low, high);
        }

        public override void PokeWord(ushort address, ushort value)
        {
            Bus.Poke(address, LowByte(value));
            Bus.Poke(++address, HighByte(value));
        }

        protected override ushort FetchWord()
        {
            var high = FetchByte();
            var low = FetchByte();
            return MakeWord(low, high);
        }

        protected override ushort GetWord()
        {
            var high = BusRead();
            ++Bus.Address;
            var low = BusRead();
            return MakeWord(low, high);
        }

        protected override ushort GetWordPaged(byte page, byte offset)
        {
            var high = BusRead(offset, page);
            var low = BusRead((byte)(offset + 1), page);
            return MakeWord(low, high);
        }

        protected override ushort PopWord()
        {
            var high = Pop();
            var low = Pop();
            return MakeWord(low, high);
        }

        protected override void PushWord(ushort value)
        {
            Push(LowByte(value));
            Push(HighByte(value));
        }

        protected override void SetWord(ushort value)
        {
            BusWrite(HighByte(value));
            ++Bus.Address;
            BusWrite(LowByte(value));
        }

        protected override void SetWordPaged(byte page, byte offset, ushort value)
        {
            BusWrite(offset, page, HighByte(value));
            BusWrite((byte)(offset + 1), page, LowByte(value));
        }
    }
}
