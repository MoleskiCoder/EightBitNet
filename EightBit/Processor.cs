// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EightBit
{
    public abstract class Processor(Bus memory) : ClockedChip
    {
        #region Instruction execution events

        public event EventHandler<EventArgs>? ExecutingInstruction;
        public event EventHandler<EventArgs>? ExecutedInstruction;

        #endregion

        #region Memory events

        public event EventHandler<EventArgs>? ReadingMemory;
        public event EventHandler<EventArgs>? ReadMemory;
        public event EventHandler<EventArgs>? WritingMemory;
        public event EventHandler<EventArgs>? WrittenMemory;

        #endregion

        private PinLevel _resetLine;
        private PinLevel _intLine;

        public event EventHandler<EventArgs>? RaisingRESET;

        public event EventHandler<EventArgs>? RaisedRESET;

        public event EventHandler<EventArgs>? LoweringRESET;

        public event EventHandler<EventArgs>? LoweredRESET;

        public event EventHandler<EventArgs>? RaisingINT;

        public event EventHandler<EventArgs>? RaisedINT;

        public event EventHandler<EventArgs>? LoweringINT;

        public event EventHandler<EventArgs>? LoweredINT;

        public ref PinLevel RESET => ref this._resetLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Chip pin name")]
        public ref PinLevel INT => ref this._intLine;

        public Bus Bus { get; } = memory;

        public Register16 PC { get; } = new();

        public Register16 Intermediate { get; } = new();

        protected byte OpCode { get; set; }

        // http://graphics.stanford.edu/~seander/bithacks.html#FixedSignExtend
        public static sbyte SignExtend(int b, byte x)
        {
            var m = Bit(b - 1); // mask can be pre-computed if b is fixed
            x &= (byte)(Bit(b) - 1);  // (Skip this if bits in x above position b are already zero.)
            return (sbyte)((x ^ m) - m);
        }

        public static sbyte SignExtend(int b, int x) => SignExtend(b, (byte)x);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Not using VB.NET")]
        public virtual int Step()
        {
            this.ResetCycles();
            this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);
            if (this.Powered)
            {
                this.PoweredStep();
            }
            this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);
            return this.Cycles;
        }

        public abstract void PoweredStep();

        public abstract void Execute();

        public int Run(int limit)
        {
            var current = 0;
            while (this.Powered && (current < limit))
            {
                current += this.Step();
            }

            return current;
        }

        protected virtual byte FetchInstruction()
        {
            this.FetchByte();
            return this.Bus.Data;
        }

        public void Execute(byte value)
        {
            this.OpCode = value;
            this.Execute();
        }

        public abstract Register16 PeekShort(ushort address);

        public abstract void PokeShort(ushort address, Register16 value);

        public void PokeShort(ushort address, ushort value) => this.PokeShort(address, new Register16(value));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRESET()
        {
            if (this.RESET.Lowered())
            {
                this.RaisingRESET?.Invoke(this, EventArgs.Empty);
                this.RESET.Raise();
                this.RaisedRESET?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerRESET()
        {
            if (this.RESET.Raised())
            {
                this.LoweringRESET?.Invoke(this, EventArgs.Empty);
                this.RESET.Lower();
                this.LoweredRESET?.Invoke(this, EventArgs.Empty);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseINT()
        {
            if (this.INT.Lowered())
            {
                this.RaisingINT?.Invoke(this, EventArgs.Empty);
                this.INT.Raise();
                this.RaisedINT?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerINT()
        {
            if (this.INT.Raised())
            {
                this.LoweringINT?.Invoke(this, EventArgs.Empty);
                this.INT.Lower();
                this.LoweredINT?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void HandleRESET() => this.RaiseRESET();

        protected virtual void HandleINT() => this.RaiseINT();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnReadingMemory() => this.ReadingMemory?.Invoke(this, EventArgs.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnReadMemory() => this.ReadMemory?.Invoke(this, EventArgs.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnWritingMemory() => this.WritingMemory?.Invoke(this, EventArgs.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnWrittenMemory() => this.WrittenMemory?.Invoke(this, EventArgs.Empty);

        protected void MemoryWrite(byte low, byte high, byte data)
        {
            this.Bus.Address.Assign(low, high);
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(Register16 address, byte data)
        {
            this.Bus.Address.Assign(address);
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(byte data)
        {
            this.Bus.Data = data;
            this.MemoryWrite();
        }

        protected virtual void MemoryWrite() => this.BusWrite();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected void MemoryRead(byte low, byte high)
        {
            this.Bus.Address.Assign(low, high);
            this.MemoryRead();
        }

        protected void MemoryRead(Register16 address)
        {
            this.Bus.Address.Assign(address);
            this.MemoryRead();
        }

        protected virtual void MemoryRead() => this.BusRead();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected virtual void IncrementPC() => this.PC.Increment();

        protected virtual void ImmediateAddress()
        {
            this.Bus.Address.Assign(this.PC);
            this.IncrementPC();
        }

        protected virtual void FetchByte()
        {
            this.ImmediateAddress();
            this.MemoryRead();
        }

        protected abstract void GetInto(Register16 into);

        protected virtual void GetShort() => this.GetInto(this.Intermediate);

        protected abstract void SetShort(Register16 value);

        protected abstract void GetPagedInto(Register16 into);

        protected virtual void GetShortPaged() => this.GetPagedInto(this.Intermediate);

        protected void GetShortPaged(byte page, byte offset)
        {
            this.Bus.Address.Assign(offset, page);
            this.GetShortPaged();
        }

        protected void GetPagedInto(byte page, byte offset, Register16 into)
        {
            Debug.Assert(into is not null, "into cannot be null");
            this.Bus.Address.Assign(offset, page);
            this.GetPagedInto(into);
        }

        protected abstract void SetPaged(Register16 value);

        protected void SetPaged(Register16 address, Register16 value)
        {
            this.Bus.Address.Assign(address);
            this.SetPaged(value);
        }

        protected void SetPaged(byte page, byte offset, Register16 value)
        {
            this.Bus.Address.Assign(offset, page);
            this.SetPaged(value);
        }

        protected abstract void FetchInto(Register16 into);

        protected void FetchShort() => this.FetchInto(this.Intermediate);

        protected void FetchShortAddress()
        {
            this.FetchShort();
            this.Bus.Address.Assign(this.Intermediate);
        }

        protected abstract void Push(byte value);

        protected abstract void Pop();

        protected abstract void PushShort(Register16 value);

        protected abstract void PopInto(Register16 into);

        protected void GetShort(Register16 address)
        {
            this.Bus.Address.Assign(address);
            this.GetShort();
        }

        protected void SetShort(Register16 address, Register16 value)
        {
            this.Bus.Address.Assign(address);
            this.SetShort(value);
        }

        protected void Jump(ushort destination) => this.PC.Joined = destination;

        protected void Jump(Register16 destination) => this.PC.Assign(destination);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Not using VB.NET")]
        protected virtual void Call(Register16 destination)
        {
            this.PushShort(this.PC);
            this.Jump(destination);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Not using VB.NET")]
        public virtual void Return() => this.PopInto(this.PC);
    }
}
