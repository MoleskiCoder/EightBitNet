// <copyright file="IntelProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class IntelProcessor : LittleEndianProcessor
    {
        private static readonly int[] HalfCarryTableAdd = new int[8] { 0, 0, 1, 0, 1, 0, 1, 1 };
        private static readonly int[] HalfCarryTableSub = new int[8] { 0, 1, 1, 1, 0, 0, 0, 1 };

        private readonly IntelOpCodeDecoded[] decodedOpCodes = new IntelOpCodeDecoded[0x100];

        private PinLevel haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            for (var i = 0; i < 0x100; ++i)
            {
                this.decodedOpCodes[i] = new IntelOpCodeDecoded((byte)i);
            }
        }

        public event EventHandler<EventArgs> RaisingHALT;

        public event EventHandler<EventArgs> RaisedHALT;

        public event EventHandler<EventArgs> LoweringHALT;

        public event EventHandler<EventArgs> LoweredHALT;

        public Register16 SP { get; } = new Register16((ushort)Mask.Mask16);

        public Register16 MEMPTR { get; } = new Register16((ushort)Mask.Mask16);

        public abstract Register16 AF { get; }

        public byte A { get => this.AF.High; set => this.AF.High = value; }

        public byte F { get => this.AF.Low; set => this.AF.Low = value; }

        public abstract Register16 BC { get; }

        public byte B { get => this.BC.High; set => this.BC.High = value; }

        public byte C { get => this.BC.Low; set => this.BC.Low = value; }

        public abstract Register16 DE { get; }

        public byte D { get => this.DE.High; set => this.DE.High = value; }

        public byte E { get => this.DE.Low; set => this.DE.Low = value; }

        public abstract Register16 HL { get; }

        public byte H { get => this.HL.High; set => this.HL.High = value; }

        public byte L { get => this.HL.Low; set => this.HL.Low = value; }

        public ref PinLevel HALT => ref this.haltLine;

        public IntelOpCodeDecoded GetDecodedOpCode(byte opCode) => this.decodedOpCodes[opCode];

        public virtual void RaiseHALT()
        {
            if (this.HALT.Lowered())
            {
                this.OnRaisingHALT();
                this.HALT.Raise();
                this.OnRaisedHALT();
            }
        }

        public virtual void LowerHALT()
        {
            if (this.HALT.Raised())
            {
                this.OnLoweringHALT();
                this.HALT.Lower();
                this.OnLoweredHALT();
            }
        }

        protected static int BuildHalfCarryIndex(byte before, byte value, int calculation) => ((before & 0x88) >> 1) | ((value & 0x88) >> 2) | ((calculation & 0x88) >> 3);

        protected static int CalculateHalfCarryAdd(byte before, byte value, int calculation)
        {
            var index = BuildHalfCarryIndex(before, value, calculation);
            return HalfCarryTableAdd[index & (int)Mask.Mask3];
        }

        protected static int CalculateHalfCarrySub(byte before, byte value, int calculation)
        {
            var index = BuildHalfCarryIndex(before, value, calculation);
            return HalfCarryTableSub[index & (int)Mask.Mask3];
        }

        protected override void OnRaisedPOWER()
        {
            this.PC.Word = this.SP.Word = this.AF.Word = this.BC.Word = this.DE.Word = this.HL.Word = (ushort)Mask.Mask16;
            this.RaiseHALT();
            base.OnRaisedPOWER();
        }

        protected virtual void OnRaisingHALT() => this.RaisingHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedHALT()
        {
            ++this.PC.Word; // Release the PC from HALT instruction
            this.RaisedHALT?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLoweringHALT() => this.LoweringHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredHALT()
        {
            --this.PC.Word; // Keep the PC on the HALT instruction (i.e. executing NOP)
            this.LoweredHALT?.Invoke(this, EventArgs.Empty);
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.Jump(0);
        }

        protected sealed override void Push(byte value) => this.BusWrite(--this.SP.Word, value);

        protected sealed override byte Pop() => this.BusRead(this.SP.Word++);

        protected sealed override Register16 GetWord()
        {
            var returned = base.GetWord();
            this.MEMPTR.Word = this.Bus.Address.Word;
            return returned;
        }

        protected sealed override void SetWord(Register16 value)
        {
            base.SetWord(value);
            this.MEMPTR.Word = this.Bus.Address.Word;
        }

        ////

        protected void Restart(byte address)
        {
            this.MEMPTR.Low = address;
            this.MEMPTR.High = 0;
            this.Call(this.MEMPTR.Word);
        }

        protected bool CallConditional(bool condition)
        {
            this.MEMPTR.Word = this.FetchWord().Word;
            if (condition)
            {
                this.Call(this.MEMPTR.Word);
            }

            return condition;
        }

        protected bool JumpConditional(bool condition)
        {
            this.MEMPTR.Word = this.FetchWord().Word;
            if (condition)
            {
                this.Jump(this.MEMPTR.Word);
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
            this.MEMPTR.Word = (ushort)(this.PC.Word + offset);
            this.Jump(this.MEMPTR.Word);
        }

        protected bool JumpRelativeConditional(bool condition)
        {
            var offsetAddress = this.PC.Word++;
            if (condition)
            {
                var offset = (sbyte)this.BusRead(offsetAddress);
                this.JumpRelative(offset);
            }

            return condition;
        }

        protected override sealed void Return()
        {
            base.Return();
            this.MEMPTR.Word = this.PC.Word;
        }
    }
}
