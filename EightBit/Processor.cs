﻿// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private PinLevel resetLine;
        private PinLevel intLine;

        protected Processor(Bus memory) => this.Bus = memory;

        public event EventHandler<EventArgs> RaisingRESET;

        public event EventHandler<EventArgs> RaisedRESET;

        public event EventHandler<EventArgs> LoweringRESET;

        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;

        public event EventHandler<EventArgs> RaisedINT;

        public event EventHandler<EventArgs> LoweringINT;

        public event EventHandler<EventArgs> LoweredINT;

        public ref PinLevel RESET => ref this.resetLine;

        public ref PinLevel INT => ref this.intLine;

        public Bus Bus { get; }

        public Register16 PC { get; } = new Register16();

        protected byte OpCode { get; set; }

        // http://graphics.stanford.edu/~seander/bithacks.html#FixedSignExtend
        public static sbyte SignExtend(int b, byte x)
        {
            var m = Bit(b - 1); // mask can be pre-computed if b is fixed
            x &= (byte)(Bit(b) - 1);  // (Skip this if bits in x above position b are already zero.)
            return (sbyte)((x ^ m) - m);
        }

        public static sbyte SignExtend(int b, int x) => SignExtend(b, (byte)x);

        public abstract int Step();

        public abstract int Execute();

        public int Run(int limit)
        {
            var current = 0;
            while (this.Powered && (current < limit))
            {
                current += this.Step();
            }

            return current;
        }

        public int Execute(byte value)
        {
            this.OpCode = value;
            return this.Execute();
        }

        public abstract Register16 PeekWord(ushort address);

        public abstract void PokeWord(ushort address, Register16 value);

        public void PokeWord(ushort address, ushort value) => this.PokeWord(address, new Register16(value));

        public virtual void RaiseRESET()
        {
            if (this.RESET.Lowered())
            {
                this.OnRaisingRESET();
                this.RESET.Raise();
                this.OnRaisedRESET();
            }
        }

        public virtual void LowerRESET()
        {
            if (this.RESET.Raised())
            {
                this.OnLoweringRESET();
                this.RESET.Lower();
                this.OnLoweredRESET();
            }
        }

        public virtual void RaiseINT()
        {
            if (this.INT.Lowered())
            {
                this.OnRaisingINT();
                this.INT.Raise();
                this.OnRaisedINT();
            }
        }

        public virtual void LowerINT()
        {
            if (this.INT.Raised())
            {
                this.OnLoweringINT();
                this.INT.Lower();
                this.OnLoweredINT();
            }
        }

        protected virtual void OnRaisingRESET() => this.RaisingRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRESET() => this.RaisedRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRESET() => this.LoweringRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRESET() => this.LoweredRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingINT() => this.RaisingINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedINT() => this.RaisedINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringINT() => this.LoweringINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredINT() => this.LoweredINT?.Invoke(this, EventArgs.Empty);

        protected virtual void HandleRESET() => this.RaiseRESET();

        protected virtual void HandleINT() => this.RaiseINT();

        protected void MemoryWrite(byte low, byte high, byte data)
        {
            this.Bus.Address.Low = low;
            this.Bus.Address.High = high;
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(ushort address, byte data)
        {
            this.Bus.Address.Word = address;
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(Register16 address, byte data) => this.MemoryWrite(address.Word, data);

        protected void MemoryWrite(byte data)
        {
            this.Bus.Data = data;
            this.MemoryWrite();
        }

        protected virtual void MemoryWrite() => this.BusWrite();

        protected virtual void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte MemoryRead(byte low, byte high)
        {
            this.Bus.Address.Low = low;
            this.Bus.Address.High = high;
            return this.MemoryRead();
        }

        protected byte MemoryRead(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.MemoryRead();
        }

        protected byte MemoryRead(Register16 address) => this.MemoryRead(address.Word);

        protected virtual byte MemoryRead() => this.BusRead();

        protected virtual byte BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected byte FetchByte() => this.MemoryRead(this.PC.Word++);

        protected abstract Register16 GetWord();

        protected abstract void SetWord(Register16 value);

        protected abstract Register16 GetWordPaged(byte page, byte offset);

        protected abstract void SetWordPaged(byte page, byte offset, Register16 value);

        protected abstract Register16 FetchWord();

        protected abstract void Push(byte value);

        protected abstract byte Pop();

        protected abstract void PushWord(Register16 value);

        protected abstract Register16 PopWord();

        protected Register16 GetWord(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.GetWord();
        }

        protected Register16 GetWord(Register16 address) => this.GetWord(address.Word);

        protected void SetWord(ushort address, Register16 value)
        {
            this.Bus.Address.Word = address;
            this.SetWord(value);
        }

        protected void SetWord(Register16 address, Register16 value) => this.SetWord(address.Word, value);

        protected void Jump(ushort destination) => this.PC.Word = destination;

        protected virtual void Call(ushort destination)
        {
            this.PushWord(this.PC);
            this.Jump(destination);
        }

        protected virtual void Return() => this.Jump(this.PopWord().Word);
    }
}
