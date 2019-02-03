namespace EightBit
{
    using System;

    public abstract class IntelProcessor : LittleEndianProcessor
    {
        private readonly IntelOpCodeDecoded[] decodedOpCodes;
        private ushort sp;
        private ushort memptr;

        private PinLevel haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            decodedOpCodes = new IntelOpCodeDecoded[0x100];
            sp = (ushort)Mask.Mask16;
            memptr = (ushort)Mask.Mask16;

            for (int i = 0; i < 0x100; ++i)
                decodedOpCodes[i] = new IntelOpCodeDecoded((byte)i);
        }

        public event EventHandler<EventArgs> RaisingHALT;
        public event EventHandler<EventArgs> RaisedHALT;
        public event EventHandler<EventArgs> LoweringHALT;
        public event EventHandler<EventArgs> LoweredHALT;

        public ref PinLevel HALT() { return ref haltLine; }

        protected bool Halted => HALT().Lowered();

        public ushort SP { get => sp; set => sp = value; }
        public ushort MEMPTR { get => memptr; set => memptr = value; }

        public abstract ushort AF { get; set; }
        public byte A { get { return HighByte(AF); } set { AF = (ushort)(LowerPart(AF) | PromoteByte(value)); } }
        public byte F { get { return LowByte(AF); } set {  AF = (ushort)(HigherPart(AF) | value); } }

        public abstract ushort BC { get; set; }
        public byte B { get { return HighByte(AF); } set { BC = (ushort)(LowerPart(BC) | PromoteByte(value)); } }
        public byte C { get { return LowByte(AF); } set { BC = (ushort)(HigherPart(BC) | value); } }

        public abstract ushort DE { get; set; }
        public byte D { get { return HighByte(AF); } set { DE = (ushort)(LowerPart(DE) | PromoteByte(value)); } }
        public byte E { get { return LowByte(AF); } set { DE = (ushort)(HigherPart(DE) | value); } }

        public abstract ushort HL { get; set; }
        public byte H { get { return HighByte(AF); } set { HL = (ushort)(LowerPart(AF) | PromoteByte(value)); } }
        public byte L { get { return LowByte(AF); } set { HL = (ushort)(HigherPart(AF) | value); } }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            RaiseHALT();
            SP = AF = BC = DE = HL = (ushort)Mask.Mask16;
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
            PC = 0;
        }

        protected sealed override void Push(byte value) => Bus.Write(--SP, value);
        protected sealed override byte Pop() => Bus.Read(SP++);

        protected sealed override ushort GetWord()
        {
            var returned = base.GetWord();
            MEMPTR = Bus.Address;
	        return returned;
        }

        protected sealed override void SetWord(ushort value)
        {
            base.SetWord(value);
            MEMPTR = Bus.Address;
        }

        //

        protected void Restart(byte address)
        {
            MEMPTR = address;
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
            MEMPTR = (ushort)(PC + offset);
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
