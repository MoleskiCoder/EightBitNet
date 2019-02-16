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

        private Register16 sp = new Register16((ushort)Mask.Mask16);

        private Register16 memptr = new Register16((ushort)Mask.Mask16);

        private PinLevel haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            for (int i = 0; i < 0x100; ++i)
            {
                this.decodedOpCodes[i] = new IntelOpCodeDecoded((byte)i);
            }
        }

        public event EventHandler<EventArgs> RaisingHALT;

        public event EventHandler<EventArgs> RaisedHALT;

        public event EventHandler<EventArgs> LoweringHALT;

        public event EventHandler<EventArgs> LoweredHALT;

        protected bool Halted => this.HALT().Lowered();

        public ref Register16 SP() => ref this.sp;

        public ref Register16 MEMPTR() => ref this.memptr;

        public abstract ref Register16 AF();

        public ref byte A() => ref this.AF().High;

        public ref byte F() => ref this.AF().Low;

        public abstract ref Register16 BC();

        public ref byte B() => ref this.BC().High;

        public ref byte C() => ref this.BC().Low;

        public abstract ref Register16 DE();

        public ref byte D() => ref this.DE().High;

        public ref byte E() => ref this.DE().Low;

        public abstract ref Register16 HL();

        public ref byte H() => ref this.HL().High;

        public ref byte L() => ref this.HL().Low;

        public ref PinLevel HALT() => ref this.haltLine;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.RaiseHALT();
            this.SP().Word = this.AF().Word = this.BC().Word = this.DE().Word = this.HL().Word = (ushort)Mask.Mask16;
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

        protected static int BuildHalfCarryIndex(byte before, byte value, int calculation)
        {
            return ((before & 0x88) >> 1) | ((value & 0x88) >> 2) | ((calculation & 0x88) >> 3);
        }

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

        protected virtual void OnRaisingHALT() => this.RaisingHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedHALT() => this.RaisedHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringHALT() => this.LoweringHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredHALT() => this.LoweredHALT?.Invoke(this, EventArgs.Empty);

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.Jump(0);
        }

        protected sealed override void Push(byte value) => this.Bus.Write(--this.SP(), value);

        protected sealed override byte Pop() => this.Bus.Read(this.SP()++);

        protected sealed override Register16 GetWord()
        {
            var returned = base.GetWord();
            this.MEMPTR().Word = this.Bus.Address().Word;
            return returned;
        }

        protected sealed override void SetWord(Register16 value)
        {
            base.SetWord(value);
            this.MEMPTR().Word = this.Bus.Address().Word;
        }

        ////

        protected void Restart(byte address)
        {
            this.MEMPTR().Low = address;
            this.MEMPTR().High = 0;
            this.Call(this.MEMPTR());
        }

        protected bool CallConditional(bool condition)
        {
            this.MEMPTR().Word = this.FetchWord().Word;
            if (condition)
            {
                this.Call(this.MEMPTR());
            }

            return condition;
        }

        protected bool JumpConditional(bool condition)
        {
            this.MEMPTR().Word = this.FetchWord().Word;
            if (condition)
            {
                this.Jump(this.MEMPTR());
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
            this.MEMPTR().Word = (ushort)(this.PC().Word + offset);
            this.Jump(this.MEMPTR());
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
            this.MEMPTR().Word = this.PC().Word;
        }

        protected void Halt()
        {
            --this.PC();
            this.LowerHALT();
        }

        protected void Proceed()
        {
            ++this.PC();
            this.RaiseHALT();
        }

        protected IntelOpCodeDecoded GetDecodedOpCode(byte opCode) => this.decodedOpCodes[opCode];
    }
}
