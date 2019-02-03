namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private Bus bus;
        private byte opcode;
        private ushort pc;

        private PinLevel resetLine;
        private PinLevel intLine;

        protected Processor(Bus memory)
        {
            bus = memory;
            pc = 0;
        }

        public event EventHandler<EventArgs> RaisingRESET;
        public event EventHandler<EventArgs> RaisedRESET;
        public event EventHandler<EventArgs> LoweringRESET;
        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;
        public event EventHandler<EventArgs> RaisedINT;
        public event EventHandler<EventArgs> LoweringINT;
        public event EventHandler<EventArgs> LoweredINT;

        public ushort PC { get => pc; set => pc = value; }
        protected byte OpCode { get => opcode; set => opcode = value; }
        public Bus Bus { get => bus; set => bus = value; }

        public ref PinLevel RESET() => ref resetLine;
        public ref PinLevel INT() => ref intLine;

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

        public abstract ushort PeekWord(ushort address);
        public abstract void PokeWord(ushort address, ushort value);

        public ushort PeekWord(byte low, byte high) => PeekWord((ushort)(PromoteByte(high) | low));

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

        #region BusWrite

        protected void BusWrite(byte low, byte high, byte data) => BusWrite(MakeWord(low, high), data);

        protected void BusWrite(ushort address, byte data)
        {
            Bus.Address = address;
            BusWrite(data);
        }

        protected void BusWrite(byte data)
        {
            Bus.Data = data;
            BusWrite();
        }

        protected virtual void BusWrite() => Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        #endregion

        #region BusRead

        protected byte BusRead(byte low, byte high) => BusRead(MakeWord(low, high));

        protected byte BusRead(ushort address)
        {
            Bus.Address = address;
            return BusRead();
        }

        protected virtual byte BusRead() => Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        #endregion

        protected byte FetchByte() => BusRead(PC++);

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
            Bus.Address = address;
			return GetWord();
        }

        protected void SetWord(ushort address, ushort value)
        {
            Bus.Address = address;
            SetWord(value);
        }

        protected void Jump(ushort destination) => PC = destination;

        protected void Call(ushort destination)
        {
            PushWord(PC);
            Jump(destination);
        }

        protected virtual void Return() => Jump(PopWord());
    }
}
