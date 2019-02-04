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

        public override ushort PeekWord(ushort address)
        {
            var low = this.Bus.Peek(address);
            var high = this.Bus.Peek(++address);
            return Chip.MakeWord(low, high);
        }

        public override void PokeWord(ushort address, ushort value)
        {
            this.Bus.Poke(address, Chip.LowByte(value));
            this.Bus.Poke(++address, Chip.HighByte(value));
        }

        protected override ushort FetchWord()
        {
            var low = this.FetchByte();
            var high = this.FetchByte();
            return Chip.MakeWord(low, high);
        }

        protected override ushort GetWord()
        {
            var low = this.BusRead();
            ++this.Bus.Address;
            var high = this.BusRead();
            return Chip.MakeWord(low, high);
        }

        protected override ushort GetWordPaged(byte page, byte offset)
        {
            var low = this.BusRead(offset, page);
            var high = this.BusRead((byte)(offset + 1), page);
            return Chip.MakeWord(low, high);
        }

        protected override ushort PopWord()
        {
            var low = this.Pop();
            var high = this.Pop();
            return Chip.MakeWord(low, high);
        }

        protected override void PushWord(ushort value)
        {
            this.Push(Chip.HighByte(value));
            this.Push(Chip.LowByte(value));
        }

        protected override void SetWord(ushort value)
        {
            this.BusWrite(Chip.LowByte(value));
            ++this.Bus.Address;
            this.BusWrite(Chip.HighByte(value));
        }

        protected override void SetWordPaged(byte page, byte offset, ushort value)
        {
            this.BusWrite(offset, page, Chip.LowByte(value));
            this.BusWrite((byte)(offset + 1), page, Chip.HighByte(value));
        }
    }
}
