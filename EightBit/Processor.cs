// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private PinLevel resetLine;
        private PinLevel intLine;
        private Register16 pc;

        protected Processor(Bus memory)
        {
            this.Bus = memory;
        }

        public event EventHandler<EventArgs> RaisingRESET;

        public event EventHandler<EventArgs> RaisedRESET;

        public event EventHandler<EventArgs> LoweringRESET;

        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;

        public event EventHandler<EventArgs> RaisedINT;

        public event EventHandler<EventArgs> LoweringINT;

        public event EventHandler<EventArgs> LoweredINT;

        public Bus Bus { get; }

        protected byte OpCode { get; set; }

        public ref Register16 PC() => ref this.pc;

        public ref PinLevel RESET() => ref this.resetLine;

        public ref PinLevel INT() => ref this.intLine;

        public abstract int Step();

        public abstract int Execute();

        public int Run(int limit)
        {
            int current = 0;
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

        public virtual void RaiseRESET()
        {
            this.OnRaisingRESET();
            this.RESET().Raise();
            this.OnRaisedRESET();
        }

        public virtual void LowerRESET()
        {
            this.OnLoweringRESET();
            this.RESET().Lower();
            this.OnLoweredRESET();
        }

        public virtual void RaiseINT()
        {
            this.OnRaisingINT();
            this.INT().Raise();
            this.OnRaisedINT();
        }

        public virtual void LowerINT()
        {
            this.OnLoweringINT();
            this.INT().Lower();
            this.OnLoweredINT();
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

        protected void BusWrite(byte low, byte high, byte data)
        {
            this.Bus.Address().Low = low;
            this.Bus.Address().High = high;
            this.BusWrite(data);
        }

        protected void BusWrite(ushort address, byte data)
        {
            this.Bus.Address().Word = address;
            this.BusWrite(data);
        }

        protected void BusWrite(Register16 address, byte data) => this.BusWrite(address.Word, data);

        protected void BusWrite(byte data)
        {
            this.Bus.Data = data;
            this.BusWrite();
        }

        protected virtual void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte BusRead(byte low, byte high)
        {
            this.Bus.Address().Low = low;
            this.Bus.Address().High = high;
            return this.BusRead();
        }

        protected byte BusRead(ushort address)
        {
            this.Bus.Address().Word = address;
            return this.BusRead();
        }

        protected byte BusRead(Register16 address) => this.BusRead(address.Word);

        protected virtual byte BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected byte FetchByte() => this.BusRead(this.PC()++);

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
            this.Bus.Address().Word = address;
            return this.GetWord();
        }

        protected void SetWord(ushort address, Register16 value)
        {
            this.Bus.Address().Word = address;
            this.SetWord(value);
        }

        protected void Jump(ushort destination) => this.PC().Word = destination;

        protected void Jump(Register16 destination) => this.Jump(destination.Word);

        protected void Call(ushort destination)
        {
            this.PushWord(this.PC());
            this.Jump(destination);
        }

        protected void Call(Register16 destination) => this.Call(destination.Word);

        protected virtual void Return() => this.Jump(this.PopWord().Word);
    }
}
