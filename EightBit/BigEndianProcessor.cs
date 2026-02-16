// <copyright file="BigEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    public abstract class BigEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekWord(ushort address)
        {
            this.Intermediate.High = this.Bus.Peek(address);
            this.Intermediate.Low = this.Bus.Peek(++address);
            return this.Intermediate;
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.Bus.Poke(address, value.High);
            this.Bus.Poke(++address, value.Low);
        }

        protected override void FetchInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.High = this.FetchByte();
            into.Low = this.FetchByte();
        }

        protected override void GetInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.High = this.MemoryRead();
            _ = this.Bus.Address.Increment();
            into.Low = this.MemoryRead();
        }

        protected override Register16 GetWordPaged()
        {
            this.Intermediate.High = this.MemoryRead();
            ++this.Bus.Address.Low;
            this.Intermediate.Low = this.MemoryRead();
            return this.Intermediate;
        }

        protected override void PopInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.High = this.Pop();
            into.Low = this.Pop();
        }

        protected override void PushWord(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.Push(value.Low);
            this.Push(value.High);
        }

        protected override void SetWord(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.High);
            _ = this.Bus.Address.Increment();
            this.MemoryWrite(value.Low);
        }

        protected override void SetWordPaged(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.High);
            ++this.Bus.Address.Low;
            this.MemoryWrite(value.Low);
        }
    }
}
