// <copyright file="LittleEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class LittleEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekWord(ushort address)
        {
            this.Intermediate.Low = this.Bus.Peek(address);
            this.Intermediate.High = this.Bus.Peek(++address);
            return this.Intermediate;
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            this.Intermediate.Low = this.FetchByte();
            this.Intermediate.High = this.FetchByte();
            return this.Intermediate;
        }

        protected override Register16 GetWord()
        {
            this.Intermediate.Low = this.MemoryRead();
            ++this.Bus.Address.Word;
            this.Intermediate.High = this.MemoryRead();
            return this.Intermediate;
        }

        protected override Register16 GetWordPaged()
        {
            this.Intermediate.Low = this.MemoryRead();
            ++this.Bus.Address.Low;
            this.Intermediate.High = this.MemoryRead();
            return this.Intermediate;
        }

        protected override Register16 PopWord()
        {
            this.Intermediate.Low = this.Pop();
            this.Intermediate.High = this.Pop();
            return this.Intermediate;
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

        protected override void SetWordPaged(Register16 value)
        {
            this.MemoryWrite(value.Low);
            ++this.Bus.Address.Low;
            this.MemoryWrite(value.High);
        }
    }
}
