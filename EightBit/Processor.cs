// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Processor(Bus memory) : ClockedChip
    {
        #region Instruction execution events

        public event EventHandler<EventArgs>? ExecutingInstruction;
        public event EventHandler<EventArgs>? ExecutedInstruction;
        protected virtual void OnExecutedInstruction() => ExecutedInstruction?.Invoke(this, EventArgs.Empty);
        protected virtual void OnExecutingInstruction() => ExecutingInstruction?.Invoke(this, EventArgs.Empty);

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

        public ref PinLevel RESET => ref _resetLine;

        public ref PinLevel INT => ref _intLine;

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

        public virtual int Step()
        {
            ResetCycles();
            OnExecutingInstruction();
            if (Powered)
            {
                PoweredStep();
            }
 
            OnExecutedInstruction();
            return Cycles;
        }

        public abstract void PoweredStep();

        public abstract void Execute();

        public int Run(int limit)
        {
            var current = 0;
            while (Powered && (current < limit))
            {
                current += Step();
            }

            return current;
        }

        public void Execute(byte value)
        {
            OpCode = value;
            Execute();
        }

        public abstract Register16 PeekWord(ushort address);

        public abstract void PokeWord(ushort address, Register16 value);

        public void PokeWord(ushort address, ushort value) => PokeWord(address, new Register16(value));

        public virtual void RaiseRESET()
        {
            if (RESET.Lowered())
            {
                OnRaisingRESET();
                RESET.Raise();
                OnRaisedRESET();
            }
        }

        public virtual void LowerRESET()
        {
            if (RESET.Raised())
            {
                OnLoweringRESET();
                RESET.Lower();
                OnLoweredRESET();
            }
        }

        public virtual void RaiseINT()
        {
            if (INT.Lowered())
            {
                OnRaisingINT();
                INT.Raise();
                OnRaisedINT();
            }
        }

        public virtual void LowerINT()
        {
            if (INT.Raised())
            {
                OnLoweringINT();
                INT.Lower();
                OnLoweredINT();
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

        protected virtual void HandleRESET() => RaiseRESET();

        protected virtual void HandleINT() => RaiseINT();

        protected void MemoryWrite(byte low, byte high)
        {
            Bus.Address.Assign(low, high);
            MemoryWrite();
        }

        protected void MemoryWrite(byte low, byte high, byte data)
        {
            Bus.Address.Assign(low, high);
            MemoryWrite(data);
        }

        protected void MemoryWrite(ushort address, byte data)
        {
            Bus.Address.Word = address;
            MemoryWrite(data);
        }

        protected void MemoryWrite(Register16 address, byte data) => MemoryWrite(address.Low, address.High, data);

        protected void MemoryWrite(Register16 address) => MemoryWrite(address.Low, address.High);

        protected void MemoryWrite(byte data)
        {
            Bus.Data = data;
            MemoryWrite();
        }

        protected virtual void MemoryWrite() => BusWrite();

        protected virtual void BusWrite() => Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte MemoryRead(byte low, byte high)
        {
            Bus.Address.Assign(low, high);
            return MemoryRead();
        }

        protected byte MemoryRead(ushort address)
        {
            Bus.Address.Word = address;
            return MemoryRead();
        }

        protected byte MemoryRead(Register16 address) => MemoryRead(address.Low, address.High);

        protected virtual byte MemoryRead() => BusRead();

        protected virtual byte BusRead() => Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected virtual byte FetchByte()
        {
            Bus.Address.Assign(PC);
            PC.Word++;
            return MemoryRead();
        }

        protected abstract Register16 GetWord();

        protected abstract void SetWord(Register16 value);

        protected abstract Register16 GetWordPaged();

        protected Register16 GetWordPaged(Register16 address)
        {
            return GetWordPaged(address.High, address.Low);
        }

        protected Register16 GetWordPaged(byte page, byte offset)
        {
            Bus.Address.Assign(offset, page);
        	return GetWordPaged();
        }

        protected abstract void SetWordPaged(Register16 value);

        protected void SetWordPaged(Register16 address, Register16 value)
        {
            SetWordPaged(address.High, address.Low, value);
        }

        protected void SetWordPaged(byte page, byte offset, Register16 value)
        {
            Bus.Address.Assign(offset, page);
            SetWordPaged(value);
        }

        protected abstract Register16 FetchWord();

        protected void FetchWordAddress()
        {
            FetchWord();
            Bus.Address.Assign(Intermediate);
        }

        protected abstract void Push(byte value);

        protected abstract byte Pop();

        protected abstract void PushWord(Register16 value);

        protected abstract Register16 PopWord();

        protected Register16 GetWord(ushort address)
        {
            Bus.Address.Word = address;
            return GetWord();
        }

        protected Register16 GetWord(Register16 address)
        {
            Bus.Address.Assign(address);
            return GetWord();
        }

        protected void SetWord(ushort address, Register16 value)
        {
            Bus.Address.Word = address;
            SetWord(value);
        }

        protected void SetWord(Register16 address, Register16 value)
        {
            Bus.Address.Assign(address);
            SetWord(value);
        }

        protected void Jump(ushort destination) => PC.Word = destination;

        protected void Jump(Register16 destination)
        {
            PC.Assign(destination);
        }

        protected void Call(ushort destination)
        {
            Intermediate.Word = destination;
            Call(Intermediate);
        }

        protected virtual void Call(Register16 destination)
        {
            PushWord(PC);
            Jump(destination);
        }

        protected virtual void Return() => Jump(PopWord());
    }
}
