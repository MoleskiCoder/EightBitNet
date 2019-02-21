// <copyright file="LittleEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class LittleEndianProcessor : Processor
    {
        protected LittleEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override Register16 PeekWord(ushort address)
        {
            var low = this.Bus.Peek(address);
            var high = this.Bus.Peek(++address);
            return new Register16(low, high);
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            var low = this.FetchByte();
            var high = this.FetchByte();
            return new Register16(low, high);
        }

        protected override Register16 GetWord()
        {
            var low = this.BusRead();
            ++this.Bus.Address.Word;
            var high = this.BusRead();
            return new Register16(low, high);
        }

        protected override Register16 GetWordPaged(byte page, byte offset)
        {
            var low = this.BusRead(offset, page);
            var high = this.BusRead(++offset, page);
            return new Register16(low, high);
        }

        protected override Register16 PopWord()
        {
            var low = this.Pop();
            var high = this.Pop();
            return new Register16(low, high);
        }

        protected override void PushWord(Register16 value)
        {
            this.Push(value.High);
            this.Push(value.Low);
        }

        protected override void SetWord(Register16 value)
        {
            this.BusWrite(value.Low);
            ++this.Bus.Address.Word;
            this.BusWrite(value.High);
        }

        protected override void SetWordPaged(byte page, byte offset, Register16 value)
        {
            this.BusWrite(offset, page, value.Low);
            this.BusWrite(++offset, page, value.High);
        }
    }
}
