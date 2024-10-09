// <copyright file="BigEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    public abstract class BigEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekWord(ushort address)
        {
            Intermediate.High = Bus.Peek(address);
            Intermediate.Low = Bus.Peek(++address);
            return Intermediate;
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            Bus.Poke(address, value.High);
            Bus.Poke(++address, value.Low);
        }

        protected override Register16 FetchWord()
        {
            Intermediate.High = FetchByte();
            Intermediate.Low = FetchByte();
            return Intermediate;
        }

        protected override Register16 GetWord()
        {
            Intermediate.High = MemoryRead();
            ++Bus.Address.Word;
            Intermediate.Low = MemoryRead();
            return Intermediate;
        }

        protected override Register16 GetWordPaged()
        {
            Intermediate.High = MemoryRead();
            ++Bus.Address.Low;
            Intermediate.Low = MemoryRead();
            return Intermediate;
        }

        protected override Register16 PopWord()
        {
            Intermediate.High = Pop();
            Intermediate.Low = Pop();
            return Intermediate;
        }

        protected override void PushWord(Register16 value)
        {
            Push(value.Low);
            Push(value.High);
        }

        protected override void SetWord(Register16 value)
        {
            MemoryWrite(value.High);
            ++Bus.Address.Word;
            MemoryWrite(value.Low);
        }

        protected override void SetWordPaged(Register16 value)
        {
            MemoryWrite(value.High);
            ++Bus.Address.Low;
            MemoryWrite(value.Low);
        }
    }
}
