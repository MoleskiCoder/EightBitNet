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

        public override ushort PeekWord(ushort address)
        {
            var high = this.Bus.Peek(address);
            var low = this.Bus.Peek(++address);
            return Chip.MakeWord(low, high);
        }

        public override void PokeWord(ushort address, ushort value)
        {
            this.Bus.Poke(address, Chip.LowByte(value));
            this.Bus.Poke(++address, Chip.HighByte(value));
        }

        protected override ushort FetchWord()
        {
            var high = this.FetchByte();
            var low = this.FetchByte();
            return Chip.MakeWord(low, high);
        }

        protected override ushort GetWord()
        {
            var high = this.BusRead();
            ++this.Bus.Address;
            var low = this.BusRead();
            return Chip.MakeWord(low, high);
        }

        protected override ushort GetWordPaged(byte page, byte offset)
        {
            var high = this.BusRead(offset, page);
            var low = this.BusRead(++offset, page);
            return Chip.MakeWord(low, high);
        }

        protected override ushort PopWord()
        {
            var high = this.Pop();
            var low = this.Pop();
            return Chip.MakeWord(low, high);
        }

        protected override void PushWord(ushort value)
        {
            this.Push(Chip.LowByte(value));
            this.Push(Chip.HighByte(value));
        }

        protected override void SetWord(ushort value)
        {
            this.BusWrite(Chip.HighByte(value));
            ++this.Bus.Address;
            this.BusWrite(Chip.LowByte(value));
        }

        protected override void SetWordPaged(byte page, byte offset, ushort value)
        {
            this.BusWrite(offset, page, Chip.HighByte(value));
            this.BusWrite(++offset, page, Chip.LowByte(value));
        }
    }
}
