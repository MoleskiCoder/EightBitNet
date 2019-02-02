namespace EightBit
{
    using System;

    public abstract class IntelProcessor : LittleEndianProcessor
    {
        private readonly IntelOpCodeDecoded[] decodedOpCodes;
        private Register16 sp;
        private Register16 memptr;

        private PinLevel haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            decodedOpCodes = new IntelOpCodeDecoded[0x100];
            sp = new Register16((ushort)Mask.Mask16);
            memptr = new Register16();

            for (int i = 0; i < 0x100; ++i)
                decodedOpCodes[i] = new IntelOpCodeDecoded((byte)i);
        }

        public event EventHandler<EventArgs> RaisingHALT;
        public event EventHandler<EventArgs> RaisedHALT;
        public event EventHandler<EventArgs> LoweringHALT;
        public event EventHandler<EventArgs> LoweredHALT;

        public ref PinLevel HALT() { return ref haltLine; }

        protected bool Halted => HALT().Lowered();

        public Register16 SP { get => sp; set => sp = value; }
        public Register16 MEMPTR { get => memptr; set => memptr = value; }

        public abstract Register16 AF { get; set; }
        public byte A { get { return AF.High; } set { AF.High = value; } }
        public byte F { get { return AF.Low; } set { AF.Low = value; } }

        public abstract Register16 BC { get; set; }
        public byte B { get { return BC.High; } set { BC.High = value; } }
        public byte C { get { return BC.Low; } set { BC.Low = value; } }

        public abstract Register16 DE { get; set; }
        public byte D { get { return DE.High; } set { DE.High = value; } }
        public byte E { get { return DE.Low; } set { DE.Low = value; } }

        public abstract Register16 HL { get; set; }
        public byte H { get { return HL.High; } set { HL.High = value; } }
        public byte L { get { return HL.Low; } set { HL.Low = value; } }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            RaiseHALT();
            SP = AF = BC = DE = HL = new Register16((ushort)Mask.Mask16);
        }

        public virtual void RaiseHALT()
        {
            OnRaisingHALT();
            HALT().Raise();
            OnRaisedHALT();
        }

        public virtual void LowerHALT()
        {
            OnLoweringHALT();
            HALT().Lower();
            OnLoweredHALT();
        }

        protected virtual void OnRaisingHALT() => RaisingHALT?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedHALT() => RaisedHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringHALT() => LoweringHALT?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredHALT() => LoweredHALT?.Invoke(this, EventArgs.Empty);

        protected override void HandleRESET()
        {
            base.HandleRESET();
            PC.Word = 0;
        }

        protected sealed override void Push(byte value) => Bus.Write(--SP, value);
        protected sealed override byte Pop() => Bus.Read(SP++);

        protected sealed override Register16 GetWord()
        {
            var returned = base.GetWord();
            MEMPTR = Bus.Address;
	        return returned;
        }

        protected sealed override void SetWord(Register16 value)
        {
            base.SetWord(value);
            MEMPTR = Bus.Address;
        }

        //

        protected void Restart(byte address)
        {
            MEMPTR.Low = address;
            MEMPTR.High = 0;
            Call(MEMPTR);
        }

        protected bool CallConditional(bool condition)
        {
            MEMPTR = FetchWord();
            if (condition)
                Call(MEMPTR);
            return condition;
        }

        protected bool JumpConditional(bool condition)
        {
            MEMPTR = FetchWord();
            if (condition)
                Jump(MEMPTR);
            return condition;
        }

        protected bool ReturnConditional(bool condition)
        {
            if (condition)
                Return();
            return condition;
        }

        protected void JumpRelative(sbyte offset)
        {
            MEMPTR.Word = (ushort)(PC.Word + offset);
            Jump(MEMPTR);
        }

        protected bool JumpRelativeConditional(bool condition)
        {
            var offset = (sbyte)FetchByte();
            if (condition)
                JumpRelative(offset);
            return condition;
        }

        protected override sealed void Return()
        {
            base.Return();
            MEMPTR = PC;
        }

        protected void Halt()
        {
            --PC;
            LowerHALT();
        }

        protected void Proceed()
        {
            ++PC;
            RaiseHALT();
        }
    }
}
