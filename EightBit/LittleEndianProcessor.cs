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
            ArgumentNullException.ThrowIfNull(value);
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override void FetchInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.Low = this.FetchByte();
            into.High = this.FetchByte();
        }

        protected override void GetInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.Low = this.MemoryRead();
            _ = this.Bus.Address.Increment();
            into.High = this.MemoryRead();
        }

        protected override Register16 GetWordPaged()
        {
            this.Intermediate.Low = this.MemoryRead();
            ++this.Bus.Address.Low;
            this.Intermediate.High = this.MemoryRead();
            return this.Intermediate;
        }

        protected override void PopInto(Register16 into)
        {
            ArgumentNullException.ThrowIfNull(into);
            into.Low = this.Pop();
            into.High = this.Pop();
        }

        protected override void PushWord(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.Push(value.High);
            this.Push(value.Low);
        }

        protected override void SetWord(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.Low);
            _ = this.Bus.Address.Increment();
            this.MemoryWrite(value.High);
        }

        protected override void SetWordPaged(Register16 value)
        {
            ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.Low);
            ++this.Bus.Address.Low;
            this.MemoryWrite(value.High);
        }
    }
}
