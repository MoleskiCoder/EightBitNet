// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private readonly Bus bus;
        private byte opcode;
        private ushort pc = 0;

        private PinLevel resetLine;
        private PinLevel intLine;

        protected Processor(Bus memory)
        {
            this.bus = memory;
        }

        public event EventHandler<EventArgs> RaisingRESET;

        public event EventHandler<EventArgs> RaisedRESET;

        public event EventHandler<EventArgs> LoweringRESET;

        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;

        public event EventHandler<EventArgs> RaisedINT;

        public event EventHandler<EventArgs> LoweringINT;

        public event EventHandler<EventArgs> LoweredINT;

        public ushort PC { get => this.pc; set => this.pc = value; }

        public Bus Bus { get => this.bus; }

        protected byte OpCode { get => this.opcode; set => this.opcode = value; }

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

        public abstract ushort PeekWord(ushort address);

        public abstract void PokeWord(ushort address, ushort value);

        public ushort PeekWord(byte low, byte high) => this.PeekWord((ushort)(Chip.PromoteByte(high) | low));

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

        protected void BusWrite(byte low, byte high, byte data) => this.BusWrite(Chip.MakeWord(low, high), data);

        protected void BusWrite(ushort address, byte data)
        {
            this.Bus.Address = address;
            this.BusWrite(data);
        }

        protected void BusWrite(byte data)
        {
            this.Bus.Data = data;
            this.BusWrite();
        }

        protected virtual void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte BusRead(byte low, byte high) => this.BusRead(Chip.MakeWord(low, high));

        protected byte BusRead(ushort address)
        {
            this.Bus.Address = address;
            return this.BusRead();
        }

        protected virtual byte BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected byte FetchByte() => this.BusRead(this.PC++);

        protected abstract ushort GetWord();

        protected abstract void SetWord(ushort value);

        protected abstract ushort GetWordPaged(byte page, byte offset);

        protected abstract void SetWordPaged(byte page, byte offset, ushort value);

        protected abstract ushort FetchWord();

        protected abstract void Push(byte value);

        protected abstract byte Pop();

        protected abstract void PushWord(ushort value);

        protected abstract ushort PopWord();

        protected ushort GetWord(ushort address)
        {
            this.Bus.Address = address;
            return this.GetWord();
        }

        protected void SetWord(ushort address, ushort value)
        {
            this.Bus.Address = address;
            this.SetWord(value);
        }

        protected void Jump(ushort destination) => this.PC = destination;

        protected void Call(ushort destination)
        {
            this.PushWord(this.PC);
            this.Jump(destination);
        }

        protected virtual void Return() => this.Jump(this.PopWord());
    }
}
