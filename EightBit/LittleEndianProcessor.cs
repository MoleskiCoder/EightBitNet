using System;

namespace EightBit
{
    public abstract class LittleEndianProcessor : Processor
    {
        protected LittleEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override Register16 PeekWord(Register16 address)
        {
            var low = Bus.Peek(address);
            var high = Bus.Peek(++address);
            return new Register16(low, high);
        }

        public override void PokeWord(Register16 address, Register16 value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Bus.Poke(address, value.Low);
            Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            var low = FetchByte();
            var high = FetchByte();
            return new Register16(low, high);
        }

        protected override Register16 GetWord()
        {
            var low = BusRead();
            ++Bus.Address.Word;
            var high = BusRead();
            return new Register16(low, high);
        }

        protected override Register16 GetWordPaged(byte page, byte offset)
        {
            var low = GetBytePaged(page, offset);
            ++Bus.Address.Low;
            var high = BusRead();
            return new Register16(low, high);
        }

        protected override Register16 PopWord()
        {
            var low = Pop();
            var high = Pop();
            return new Register16(low, high);
        }

        protected override void PushWord(Register16 value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Push(value.High);
            Push(value.Low);
        }

        protected override void SetWord(Register16 value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            BusWrite(value.Low);
            ++Bus.Address.Word;
            BusWrite(value.High);
        }

        protected override void SetWordPaged(byte page, byte offset, Register16 value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            SetBytePaged(page, offset, value.Low);
            ++Bus.Address.Low;
            BusWrite(value.High);
        }
    }
}
