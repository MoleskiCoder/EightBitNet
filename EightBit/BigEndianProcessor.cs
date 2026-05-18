// <copyright file="BigEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    public abstract class BigEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekShort(ushort address)
        {
            this.Intermediate.High = this.Bus.Peek(address);
            this.Intermediate.Low = this.Bus.Peek(++address);
            return this.Intermediate;
        }

        public override void PokeShort(ushort address, Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.Bus.Poke(address, value.High);
            this.Bus.Poke(++address, value.Low);
        }

        protected override void FetchInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.FetchByte();
            into.High = this.Bus.Data;
            this.FetchByte();
            into.Low = this.Bus.Data;
        }

        protected override void GetInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.MemoryRead();
            into.High = this.Bus.Data;
            this.Bus.Address.Increment();
            this.MemoryRead();
            into.Low = this.Bus.Data;
        }

        protected override void GetPagedInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.MemoryRead();
            into.High = this.Bus.Data;
            ++this.Bus.Address.Low;
            this.MemoryRead();
            into.Low = this.Bus.Data;
        }

        protected override void PopInto(Register16 into)
        {
            //ArgumentNullException.ThrowIfNull(into);
            this.Pop();
            into.High = this.Bus.Data;
            this.Pop();
            into.Low = this.Bus.Data;
        }

        protected override void PushShort(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.Push(value.Low);
            this.Push(value.High);
        }

        protected override void SetShort(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.High);
            this.Bus.Address.Increment();
            this.MemoryWrite(value.Low);
        }

        protected override void SetPaged(Register16 value)
        {
            //ArgumentNullException.ThrowIfNull(value);
            this.MemoryWrite(value.High);
            ++this.Bus.Address.Low;
            this.MemoryWrite(value.Low);
        }
    }
}
