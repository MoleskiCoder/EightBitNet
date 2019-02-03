namespace EightBit
{
    public abstract class LittleEndianProcessor : Processor
    {
        protected LittleEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override ushort PeekWord(ushort address)
        {
            var low = Bus.Peek(address);
            var high = Bus.Peek(++address);
            return MakeWord(low, high);
        }

        public override void PokeWord(ushort address, ushort value)
        {
            Bus.Poke(address, LowByte(value));
            Bus.Poke(++address, HighByte(value));
        }

        protected override ushort FetchWord()
        {
            var low = FetchByte();
            var high = FetchByte();
            return MakeWord(low, high);
        }

        protected override ushort GetWord()
        {
            var low = BusRead();
            ++Bus.Address;
            var high = BusRead();
            return MakeWord(low, high);
        }

        protected override ushort GetWordPaged(byte page, byte offset)
        {
            var low = GetBytePaged(page, offset);
            var high = GetBytePaged(page, (byte)(offset + 1));
            return MakeWord(low, high);
        }

        protected override ushort PopWord()
        {
            var low = Pop();
            var high = Pop();
            return MakeWord(low, high);
        }

        protected override void PushWord(ushort value)
        {
            Push(HighByte(value));
            Push(LowByte(value));
        }

        protected override void SetWord(ushort value)
        {
            BusWrite(LowByte(value));
            ++Bus.Address;
            BusWrite(HighByte(value));
        }

        protected override void SetWordPaged(byte page, byte offset, ushort value)
        {
            SetBytePaged(page, offset, LowByte(value));
            SetBytePaged(page, (byte)(offset + 1), HighByte(value));
        }
    }
}
