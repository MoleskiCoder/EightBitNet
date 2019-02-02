namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private Bus bus;
        private byte opcode;
        private Register16 pc;

        private PinLevel resetLine;
        private PinLevel intLine;

        protected Processor(Bus memory)
        {
            bus = memory;
            pc = new Register16();
        }

        public event EventHandler<EventArgs> RaisingRESET;
        public event EventHandler<EventArgs> RaisedRESET;
        public event EventHandler<EventArgs> LoweringRESET;
        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;
        public event EventHandler<EventArgs> RaisedINT;
        public event EventHandler<EventArgs> LoweringINT;
        public event EventHandler<EventArgs> LoweredINT;

        public Register16 PC { get => pc; set => pc = value; }
        protected byte OpCode { get => opcode; set => opcode = value; }
        public Bus Bus { get => bus; set => bus = value; }

        public ref PinLevel RESET() { return ref resetLine; }
        public ref PinLevel INT() { return ref intLine; }

        public abstract int Step();
        public abstract int Execute();

        public int Run(int limit)
        {
            int current = 0;
            while (Powered && (current < limit))
                current += Step();
            return current;
        }

        public int Execute(byte value)
        {
            OpCode = value;
	        return Execute();
        }

        public abstract Register16 PeekWord(Register16 address);
        public abstract void PokeWord(Register16 address, Register16 value);

        public Register16 PeekWord(byte low, byte high) => PeekWord(new Register16(low, high));
        public Register16 PeekWord(ushort address) => PeekWord(LowByte(address), HighByte(address));

        public virtual void RaiseRESET()
        {
            OnRaisingRESET();
            RESET().Raise();
            OnRaisedRESET();
        }

        public virtual void LowerRESET()
        {
            OnLoweringRESET();
            RESET().Lower();
            OnLoweredRESET();
        }

        public virtual void RaiseINT()
        {
            OnRaisingINT();
            INT().Raise();
            OnRaisedINT();
        }

        public virtual void LowerINT()
        {
            OnLoweringINT();
            INT().Lower();
            OnLoweredINT();
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

        protected void BusWrite(byte low, byte data) => BusWrite(low, 0, data);

        protected void BusWrite(Register16 address, byte data) => BusWrite(address.Low, address.High, data);

        protected void BusWrite(byte low, byte high, byte data)
        {
            Bus.Address.Low = low;
            Bus.Address.High = high;
            BusWrite(data);
        }

        protected void BusWrite(byte data)
        {
            Bus.Data = data;
            BusWrite();
        }

        protected virtual void BusWrite() => Bus.Write();

        protected byte BusRead(byte low) => BusRead(low, 0);

        protected byte BusRead(Register16 address) => BusRead(address.Low, address.High);

        protected byte BusRead(byte low, byte high)
        {
            Bus.Address.Low = low;
            Bus.Address.High = high;
            return BusRead();
        }

        protected virtual byte BusRead() => Bus.Read();

        protected byte GetBytePaged(byte page, byte offset) => BusRead(new Register16(offset, page));

        protected void SetBytePaged(byte page, byte offset, byte value) => BusWrite(new Register16(offset, page), value);

        protected byte FetchByte() => BusRead(PC++);

        protected abstract Register16 GetWord();
		protected abstract void SetWord(Register16 value);

		protected abstract Register16 GetWordPaged(byte page, byte offset);
		protected abstract void SetWordPaged(byte page, byte offset, Register16 value);

		protected abstract Register16 FetchWord();

		protected abstract void Push(byte value);
		protected abstract byte Pop();

		protected abstract void PushWord(Register16 value);
		protected abstract Register16 PopWord();

        protected Register16 GetWord(Register16 address)
        {
            Bus.Address.Word = address.Word;
			return GetWord();
        }

        protected void SetWord(Register16 address, Register16 value)
        {
            Bus.Address.Word = address.Word;
            SetWord(value);
        }

        protected void Jump(Register16 destination)
        {
			PC = destination;
		}

        protected void Call(Register16 destination)
        {
            PushWord(PC);
            Jump(destination);
        }

        protected virtual void Return()
        {
            Jump(PopWord());
        }
    }
}
