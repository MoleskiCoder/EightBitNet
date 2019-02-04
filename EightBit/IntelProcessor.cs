// <copyright file="IntelProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class IntelProcessor : LittleEndianProcessor
    {
        private readonly IntelOpCodeDecoded[] decodedOpCodes = new IntelOpCodeDecoded[0x100];
        private ushort sp;
        private ushort memptr;

        private PinLevel haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            this.sp = (ushort)Mask.Mask16;
            this.memptr = (ushort)Mask.Mask16;

            for (int i = 0; i < 0x100; ++i)
            {
                this.decodedOpCodes[i] = new IntelOpCodeDecoded((byte)i);
            }
        }

        public event EventHandler<EventArgs> RaisingHALT;

        public event EventHandler<EventArgs> RaisedHALT;

        public event EventHandler<EventArgs> LoweringHALT;

        public event EventHandler<EventArgs> LoweredHALT;

        public ushort SP { get => this.sp; set => this.sp = value; }

        public ushort MEMPTR { get => this.memptr; set => this.memptr = value; }

        public abstract ushort AF { get; set; }

        public byte A { get => Chip.HighByte(this.AF); set => this.AF = (ushort)(Chip.LowerPart(this.AF) | Chip.PromoteByte(value)); }

        public byte F { get => Chip.LowByte(this.AF); set => this.AF = (ushort)(Chip.HigherPart(this.AF) | value); }

        public abstract ushort BC { get; set; }

        public byte B { get => Chip.HighByte(this.BC); set => this.BC = (ushort)(Chip.LowerPart(this.BC) | Chip.PromoteByte(value)); }

        public byte C { get => Chip.LowByte(this.BC); set => this.BC = (ushort)(Chip.HigherPart(this.BC) | value); }

        public abstract ushort DE { get; set; }

        public byte D { get => Chip.HighByte(this.DE); set => this.DE = (ushort)(Chip.LowerPart(this.DE) | Chip.PromoteByte(value)); }

        public byte E { get => Chip.LowByte(this.DE); set => this.DE = (ushort)(Chip.HigherPart(this.DE) | value); }

        public abstract ushort HL { get; set; }

        public byte H { get => Chip.HighByte(this.HL); set => this.HL = (ushort)(Chip.LowerPart(this.HL) | Chip.PromoteByte(value)); }

        public byte L { get => Chip.LowByte(this.HL); set => this.HL = (ushort)(Chip.HigherPart(this.HL) | value); }

        protected bool Halted => this.HALT().Lowered();

        public ref PinLevel HALT() => ref this.haltLine;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.RaiseHALT();
            this.SP = this.AF = this.BC = this.DE = this.HL = (ushort)Mask.Mask16;
        }

        public virtual void RaiseHALT()
        {
            this.OnRaisingHALT();
            this.HALT().Raise();
            this.OnRaisedHALT();
        }

        public virtual void LowerHALT()
        {
            this.OnLoweringHALT();
            this.HALT().Lower();
            this.OnLoweredHALT();
        }

        protected virtual void OnRaisingHALT() => this.RaisingHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedHALT() => this.RaisedHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringHALT() => this.LoweringHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredHALT() => this.LoweredHALT?.Invoke(this, EventArgs.Empty);

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.PC = 0;
        }

        protected sealed override void Push(byte value) => this.Bus.Write(--this.SP, value);

        protected sealed override byte Pop() => this.Bus.Read(this.SP++);

        protected sealed override ushort GetWord()
        {
            var returned = base.GetWord();
            this.MEMPTR = this.Bus.Address;
            return returned;
        }

        protected sealed override void SetWord(ushort value)
        {
            base.SetWord(value);
            this.MEMPTR = this.Bus.Address;
        }

        ////

        protected void Restart(byte address)
        {
            this.MEMPTR = address;
            this.Call(this.MEMPTR);
        }

        protected bool CallConditional(bool condition)
        {
            this.MEMPTR = this.FetchWord();
            if (condition)
            {
                this.Call(this.MEMPTR);
            }

            return condition;
        }

        protected bool JumpConditional(bool condition)
        {
            this.MEMPTR = this.FetchWord();
            if (condition)
            {
                this.Jump(this.MEMPTR);
            }

            return condition;
        }

        protected bool ReturnConditional(bool condition)
        {
            if (condition)
            {
                this.Return();
            }

            return condition;
        }

        protected void JumpRelative(sbyte offset)
        {
            this.MEMPTR = (ushort)(this.PC + offset);
            this.Jump(this.MEMPTR);
        }

        protected bool JumpRelativeConditional(bool condition)
        {
            var offset = (sbyte)this.FetchByte();
            if (condition)
            {
                this.JumpRelative(offset);
            }

            return condition;
        }

        protected override sealed void Return()
        {
            base.Return();
            this.MEMPTR = this.PC;
        }

        protected void Halt()
        {
            --this.PC;
            this.LowerHALT();
        }

        protected void Proceed()
        {
            ++this.PC;
            this.RaiseHALT();
        }
    }
}
