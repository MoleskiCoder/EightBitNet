// <copyright file="BigEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    public abstract class BigEndianProcessor : Processor
    {
        protected BigEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override Register16 PeekWord(ushort address)
        {
            var high = this.Bus.Peek(address);
            var low = this.Bus.Peek(++address);
            return new Register16(low, high);
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            var high = this.FetchByte();
            var low = this.FetchByte();
            return new Register16(low, high);
        }

        protected override Register16 GetWord()
        {
            var high = this.BusRead();
            ++this.Bus.Address.Word;
            var low = this.BusRead();
            return new Register16(low, high);
        }

        protected override Register16 GetWordPaged(byte page, byte offset)
        {
            var high = this.BusRead(offset, page);
            var low = this.BusRead(++offset, page);
            return new Register16(low, high);
        }

        protected override Register16 PopWord()
        {
            var high = this.Pop();
            var low = this.Pop();
            return new Register16(low, high);
        }

        protected override void PushWord(Register16 value)
        {
            this.Push(value.Low);
            this.Push(value.High);
        }

        protected override void SetWord(Register16 value)
        {
            this.BusWrite(value.High);
            ++this.Bus.Address.Word;
            this.BusWrite(value.Low);
        }

        protected override void SetWordPaged(byte page, byte offset, Register16 value)
        {
            this.BusWrite(offset, page, value.High);
            this.BusWrite(++offset, page, value.Low);
        }
    }
}
