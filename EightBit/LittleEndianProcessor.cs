// <copyright file="LittleEndianProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class LittleEndianProcessor(Bus memory) : Processor(memory)
    {
        public override Register16 PeekWord(ushort address)
        {
            Intermediate.Low = Bus.Peek(address);
            Intermediate.High = Bus.Peek(++address);
            return Intermediate;
        }

        public override void PokeWord(ushort address, Register16 value)
        {
            Bus.Poke(address, value.Low);
            Bus.Poke(++address, value.High);
        }

        protected override Register16 FetchWord()
        {
            Intermediate.Low = FetchByte();
            Intermediate.High = FetchByte();
            return Intermediate;
        }

        protected override Register16 GetWord()
        {
            Intermediate.Low = MemoryRead();
            ++Bus.Address.Word;
            Intermediate.High = MemoryRead();
            return Intermediate;
        }

        protected override Register16 GetWordPaged()
        {
            Intermediate.Low = MemoryRead();
            ++Bus.Address.Low;
            Intermediate.High = MemoryRead();
            return Intermediate;
        }

        protected override Register16 PopWord()
        {
            Intermediate.Low = Pop();
            Intermediate.High = Pop();
            return Intermediate;
        }

        protected override void PushWord(Register16 value)
        {
            Push(value.High);
            Push(value.Low);
        }

        protected override void SetWord(Register16 value)
        {
            MemoryWrite(value.Low);
            ++Bus.Address.Word;
            MemoryWrite(value.High);
        }

        protected override void SetWordPaged(Register16 value)
        {
            MemoryWrite(value.Low);
            ++Bus.Address.Low;
            MemoryWrite(value.High);
        }
    }
}
