// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class Processor(Bus memory) : ClockedChip
    {
        #region Instruction execution events

        public event EventHandler<EventArgs>? ExecutingInstruction;
        public event EventHandler<EventArgs>? ExecutedInstruction;
        protected virtual void OnExecutedInstruction() => ExecutedInstruction?.Invoke(this, EventArgs.Empty);
        protected virtual void OnExecutingInstruction() => ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        #endregion

        #region Bus events

        #region Memory events

        #region Memory read events

        public event EventHandler<EventArgs>? ReadingMemory;
        public event EventHandler<EventArgs>? ReadMemory;
        protected virtual void OnReadingMemory() => ReadingMemory?.Invoke(this, EventArgs.Empty);
        protected virtual void OnReadMemory() => ReadMemory?.Invoke(this, EventArgs.Empty);

        #endregion

        #region Memory write events

        public event EventHandler<EventArgs>? WritingMemory;
        public event EventHandler<EventArgs>? WrittenMemory;
        protected virtual void OnWritingMemory() => WritingMemory?.Invoke(this, EventArgs.Empty);
        protected virtual void OnWrittenMemory() => WrittenMemory?.Invoke(this, EventArgs.Empty);

        #endregion

        #endregion

        #region IO events

        #region IO read events

        public event EventHandler<EventArgs>? ReadingIO;
        public event EventHandler<EventArgs>? ReadIO;
        protected virtual void OnReadingIO() => ReadingIO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnReadIO() => ReadIO?.Invoke(this, EventArgs.Empty);

        #endregion

        #region IO write events

        public event EventHandler<EventArgs>? WritingIO;
        public event EventHandler<EventArgs>? WrittenIO;
        protected virtual void OnWritingIO() => WritingIO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnWrittenIO() => WrittenIO?.Invoke(this, EventArgs.Empty);
        #endregion

        #endregion

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
            this.OnExecutingInstruction();
            if (this.Powered)
            {
                this.PoweredStep();
            }
            this.OnExecutedInstruction();
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

        public void Execute(byte value)
        {
            this.OpCode = value;
            this.Execute();
        }

        public abstract Register16 PeekWord(ushort address);

        public abstract void PokeWord(ushort address, Register16 value);

        public void PokeWord(ushort address, ushort value) => this.PokeWord(address, new Register16(value));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
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

        protected virtual void OnRaisingRESET() => RaisingRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRESET() => RaisedRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRESET() => LoweringRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRESET() => LoweredRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingINT() => RaisingINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedINT() => RaisedINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringINT() => LoweringINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredINT() => LoweredINT?.Invoke(this, EventArgs.Empty);

        protected virtual void HandleRESET() => this.RaiseRESET();

        protected virtual void HandleINT() => this.RaiseINT();

        protected void MemoryWrite(byte low, byte high)
        {
            this.Bus.Address.Assign(low, high);
            this.MemoryWrite();
        }

        protected void MemoryWrite(byte low, byte high, byte data)
        {
            this.Bus.Address.Assign(low, high);
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(ushort address, byte data)
        {
            this.Bus.Address.Word = address;
            this.MemoryWrite(data);
        }

        protected void MemoryWrite(Register16 address, byte data)
        {
            ArgumentNullException.ThrowIfNull(address);
            this.MemoryWrite(address.Low, address.High, data);
        }

        protected void MemoryWrite(Register16 address)
        {
            ArgumentNullException.ThrowIfNull(address);
            this.MemoryWrite(address.Low, address.High);
        }

        protected void MemoryWrite(byte data)
        {
            this.Bus.Data = data;
            this.MemoryWrite();
        }

        protected virtual void MemoryWrite() => this.BusWrite();

        protected virtual void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte MemoryRead(byte low, byte high)
        {
            this.Bus.Address.Assign(low, high);
            return this.MemoryRead();
        }

        protected byte MemoryRead(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.MemoryRead();
        }

        protected byte MemoryRead(Register16 address)
        {
            ArgumentNullException.ThrowIfNull(address);
            return this.MemoryRead(address.Low, address.High);
        }

        protected virtual byte MemoryRead()
        {
            return this.BusRead();
        }

        protected virtual byte BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected virtual byte FetchByte()
        {
            this.Bus.Address.Assign(this.PC);
            this.PC.Word++;
            return this.MemoryRead();
        }

        protected abstract Register16 GetWord();

        protected abstract void SetWord(Register16 value);

        protected abstract Register16 GetWordPaged();

        protected Register16 GetWordPaged(Register16 address)
        {
            ArgumentNullException.ThrowIfNull(address);
            return this.GetWordPaged(address.High, address.Low);
        }

        protected Register16 GetWordPaged(byte page, byte offset)
        {
            this.Bus.Address.Assign(offset, page);
            return this.GetWordPaged();
        }

        protected abstract void SetWordPaged(Register16 value);

        protected void SetWordPaged(Register16 address, Register16 value)
        {
            ArgumentNullException.ThrowIfNull(address);
            this.SetWordPaged(address.High, address.Low, value);
        }

        protected void SetWordPaged(byte page, byte offset, Register16 value)
        {
            this.Bus.Address.Assign(offset, page);
            this.SetWordPaged(value);
        }

        protected abstract Register16 FetchWord();

        protected void FetchWordAddress()
        {
            _ = this.FetchWord();
            this.Bus.Address.Assign(this.Intermediate);
        }

        protected abstract void Push(byte value);

        protected abstract byte Pop();

        protected abstract void PushWord(Register16 value);

        protected abstract Register16 PopWord();

        protected Register16 GetWord(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.GetWord();
        }

        protected Register16 GetWord(Register16 address)
        {
            this.Bus.Address.Assign(address);
            return this.GetWord();
        }

        protected void SetWord(ushort address, Register16 value)
        {
            this.Bus.Address.Word = address;
            this.SetWord(value);
        }

        protected void SetWord(Register16 address, Register16 value)
        {
            this.Bus.Address.Assign(address);
            this.SetWord(value);
        }

        protected void Jump(ushort destination) => this.PC.Word = destination;

        protected void Jump(Register16 destination)
        {
            ArgumentNullException.ThrowIfNull(destination);
            this.PC.Assign(destination);
        }

        protected void Call(ushort destination)
        {
            this.Intermediate.Word = destination;
            this.Call(this.Intermediate);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Not using VB.NET")]
        protected virtual void Call(Register16 destination)
        {
            this.PushWord(this.PC);
            this.Jump(destination);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Not using VB.NET")]
        protected virtual void Return() => this.Jump(this.PopWord());
    }
}
