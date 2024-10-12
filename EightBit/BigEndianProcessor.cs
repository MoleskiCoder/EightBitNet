﻿// <copyright file="BigEndianProcessor.cs" company="Adrian Conlon">
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

        protected override Register16 FetchWord()
        {
            this.Intermediate.High = this.FetchByte();
            this.Intermediate.Low = this.FetchByte();
            return this.Intermediate;
        }

        protected override Register16 GetWord()
        {
            this.Intermediate.High = this.MemoryRead();
            ++this.Bus.Address.Word;
            this.Intermediate.Low = this.MemoryRead();
            return this.Intermediate;
        }

        protected override Register16 GetWordPaged()
        {
            this.Intermediate.High = this.MemoryRead();
            ++this.Bus.Address.Low;
            this.Intermediate.Low = this.MemoryRead();
            return this.Intermediate;
        }

        protected override Register16 PopWord()
        {
            this.Intermediate.High = this.Pop();
            this.Intermediate.Low = this.Pop();
            return this.Intermediate;
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
            ++this.Bus.Address.Word;
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
