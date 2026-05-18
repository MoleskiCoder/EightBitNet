// <copyright file="LittleEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class LittleEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekShort(ushort address)
        {
            this.Intermediate.Low = this.Bus.Peek(address);
            this.Intermediate.High = this.Bus.Peek(++address);
            return this.Intermediate;
        }

        public override void PokeShort(ushort address, Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.Bus.Poke(address, value.Low);
            this.Bus.Poke(++address, value.High);
        }

        protected override void FetchInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.FetchByte();
            into.Low = this.Bus.Data;
            this.FetchByte();
            into.High = this.Bus.Data;
        }

        protected override void GetInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.MemoryRead();
            into.Low = this.Bus.Data;
            this.Bus.Address.Increment();
            this.MemoryRead();
            into.High = this.Bus.Data;
        }

        protected override void GetPagedInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.MemoryRead();
            into.Low = this.Bus.Data;
            ++this.Bus.Address.Low;
            this.MemoryRead();
            into.High = this.Bus.Data;
        }

        protected override void PopInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            into.Low = this.Pop();
            into.High = this.Pop();
        }

        protected override void PushShort(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.Push(value.High);
            this.Push(value.Low);
        }

        protected override void SetShort(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.Low);
            this.Bus.Address.Increment();
            this.MemoryWrite(value.High);
        }

        protected override void SetPaged(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.Low);
            ++this.Bus.Address.Low;
            this.MemoryWrite(value.High);
        }
    }
}
