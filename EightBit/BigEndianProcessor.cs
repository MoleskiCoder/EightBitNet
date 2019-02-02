namespace EightBit
{
    public abstract class BigEndianProcessor : Processor
    {
        protected BigEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override Register16 PeekWord(Register16 address)
        {
            var high = Bus.Peek(address);
            var low = Bus.Peek(++address);
            return new Register16(low, high);
        }

        public override void PokeWord(Register16 address, Register16 value)
        {
            Bus.Poke(address, value.High);
            Bus.Poke(++address, value.Low);
        }

        protected override Register16 FetchWord()
        {
            var high = FetchByte();
            var low = FetchByte();
            return new Register16(low, high);
        }

        protected override Register16 GetWord()
        {
            var high = BusRead();
            ++Bus.Address.Word;
            var low = BusRead();
            return new Register16(low, high);
        }

        protected override Register16 GetWordPaged(byte page, byte offset)
        {
            var high = GetBytePaged(page, offset);
            ++Bus.Address.Low;
            var low = BusRead();
            return new Register16(low, high);
        }

        protected override Register16 PopWord()
        {
            var high = Pop();
            var low = Pop();
            return new Register16(low, high);
        }

        protected override void PushWord(Register16 value)
        {
            Push(value.Low);
            Push(value.High);
        }

        protected override void SetWord(Register16 value)
        {
            BusWrite(value.High);
            ++Bus.Address.Word;
            BusWrite(value.Low);
        }

        protected override void SetWordPaged(byte page, byte offset, Register16 value)
        {
            SetBytePaged(page, offset, value.High);
            ++Bus.Address.Low;
            BusWrite(value.Low);
        }
    }
}
