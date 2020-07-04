// <copyright file="LittleEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class LittleEndianProcessor : Processor
    {
        private readonly Register16 intermediate = new Register16();

        protected LittleEndianProcessor(Bus memory)
        : base(memory)
        {
        }

        public override Register16 PeekWord(ushort address)
        {
            this.intermediate.Low = this.Bus.Peek(address);
            this.intermediate.High = this.Bus.Peek(++address);
            return this.intermediate;
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            this.intermediate.Low = this.FetchByte();
            this.intermediate.High = this.FetchByte();
            return this.intermediate;
        }

        protected override Register16 GetWord()
        {
            this.intermediate.Low = this.MemoryRead();
            ++this.Bus.Address.Word;
            this.intermediate.High = this.MemoryRead();
            return this.intermediate;
        }

        protected override Register16 GetWordPaged(byte page, byte offset)
        {
            this.intermediate.Low = this.MemoryRead(offset, page);
            this.intermediate.High = this.MemoryRead(++offset, page);
            return this.intermediate;
        }

        protected override Register16 PopWord()
        {
            this.intermediate.Low = this.Pop();
            this.intermediate.High = this.Pop();
            return this.intermediate;
        }

        protected override void PushWord(Register16 value)
        {
            this.Push(value.High);
            this.Push(value.Low);
        }

        protected override void SetWord(Register16 value)
        {
            this.MemoryWrite(value.Low);
            ++this.Bus.Address.Word;
            this.MemoryWrite(value.High);
        }

        protected override void SetWordPaged(byte page, byte offset, Register16 value)
        {
            this.MemoryWrite(offset, page, value.Low);
            this.MemoryWrite(++offset, page, value.High);
        }
    }
}
