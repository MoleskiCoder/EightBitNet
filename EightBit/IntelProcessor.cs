// <copyright file="IntelProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class IntelProcessor : LittleEndianProcessor
    {
        private static readonly int[] HalfCarryTableAdd = [0, 0, 1, 0, 1, 0, 1, 1];
        private static readonly int[] HalfCarryTableSub = [0, 1, 1, 1, 0, 0, 0, 1];

        private readonly IntelOpCodeDecoded[] _decodedOpCodes = new IntelOpCodeDecoded[0x100];

        private PinLevel _haltLine;

        protected IntelProcessor(Bus bus)
        : base(bus)
        {
            for (var i = 0; i < 0x100; ++i)
            {
                this._decodedOpCodes[i] = new((byte)i);
            }
            this.RaisedPOWER += this.IntelProcessor_RaisedPOWER;
        }

        public event EventHandler<EventArgs>? RaisingHALT;

        public event EventHandler<EventArgs>? RaisedHALT;

        public event EventHandler<EventArgs>? LoweringHALT;

        public event EventHandler<EventArgs>? LoweredHALT;

        public Register16 SP { get; } = new((ushort)Mask.Sixteen);

        public Register16 MEMPTR { get; } = new((ushort)Mask.Sixteen);

        public abstract Register16 AF { get; }

        public ref byte A => ref this.AF.High;

        public ref byte F => ref this.AF.Low;

        public abstract Register16 BC { get; }

        public ref byte B => ref this.BC.High;

        public ref byte C => ref this.BC.Low;

        public abstract Register16 DE { get; }

        public ref byte D => ref this.DE.High;

        public ref byte E => ref this.DE.Low;

        public abstract Register16 HL { get; }

        public ref byte H => ref this.HL.High;

        public ref byte L => ref this.HL.Low;

        public ref PinLevel HALT => ref this._haltLine;

        public IntelOpCodeDecoded GetDecodedOpCode(byte opCode) => this._decodedOpCodes[opCode];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseHALT()
        {
            if (this.HALT.Lowered())
            {
                RaisingHALT?.Invoke(this, EventArgs.Empty);
                this.HALT.Raise();
                RaisedHALT?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerHALT()
        {
            if (this.HALT.Raised())
            {
                LoweringHALT?.Invoke(this, EventArgs.Empty);
                this.HALT.Lower();
                LoweredHALT?.Invoke(this, EventArgs.Empty);
            }
        }

        protected abstract void DisableInterrupts();

        protected abstract void EnableInterrupts();

        protected override void IncrementPC()
        {
            if (this.HALT.Raised())
            {
                base.IncrementPC();
            }
        }

        protected override byte FetchInstruction()
        {
            var read = this.FetchByte();
            return this.HALT.Lowered() ? (byte)0 : read;
        }


        protected void ResetWorkingRegisters()
        {
            this.AF.Word = this.BC.Word = this.DE.Word = this.HL.Word = (ushort)Mask.Sixteen;
        }

        protected static int BuildHalfCarryIndex(byte before, byte value, int calculation) => ((before & 0x88) >> 1) | ((value & 0x88) >> 2) | ((calculation & 0x88) >> 3);

        protected static int CalculateHalfCarryAdd(byte before, byte value, int calculation)
        {
            var index = BuildHalfCarryIndex(before, value, calculation);
            return HalfCarryTableAdd[index & (int)Mask.Three];
        }

        protected static int CalculateHalfCarrySub(byte before, byte value, int calculation)
        {
            var index = BuildHalfCarryIndex(before, value, calculation);
            return HalfCarryTableSub[index & (int)Mask.Three];
        }

        private void IntelProcessor_RaisedPOWER(object? sender, EventArgs e)
        {
            this.PC.Word = this.SP.Word = this.AF.Word = this.BC.Word = this.DE.Word = this.HL.Word = (ushort)Mask.Sixteen;
            this.RaiseHALT();
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DisableInterrupts();
            this.Jump(0);
        }

        protected sealed override void Push(byte value)
        {
            --this.SP.Word;
            this.MemoryWrite(this.SP, value);
        }

        protected sealed override byte Pop()
        {
            var returned = this.MemoryRead(this.SP);
            this.SP.Word++;
            return returned;
        }

        protected sealed override Register16 GetWord()
        {
            var returned = base.GetWord();
            this.MEMPTR.Assign(this.Bus.Address);
            return returned;
        }

        protected sealed override void SetWord(Register16 value)
        {
            base.SetWord(value);
            this.MEMPTR.Assign(this.Bus.Address);
        }

        ////

        protected void Restart(byte address)
        {
            this.MEMPTR.Assign(address, 0);
            this.Call(this.MEMPTR);
        }

        protected bool CallConditional(bool condition)
        {
            this.FetchWordMEMPTR();
            if (condition)
            {
                this.Call(this.MEMPTR);
            }

            return condition;
        }

        protected virtual bool JumpConditional(bool condition)
        {
            this.FetchWordMEMPTR();
            if (condition)
            {
                this.Jump(this.MEMPTR);
            }

            return condition;
        }

        protected virtual bool JumpRelativeConditional(bool condition)
        {
            var offset = this.FetchByte();
            if (condition)
            {
                this.JumpRelative(offset);
            }
            return condition;
        }

        protected virtual bool ReturnConditional(bool condition)
        {
            if (condition)
            {
                this.Return();
            }

            return condition;
        }

        protected void FetchWordMEMPTR()
        {
            _ = this.FetchWord();
            this.MEMPTR.Assign(this.Intermediate);
        }

        protected virtual void JumpIndirect()
        {
            this.FetchWordMEMPTR();
            this.Jump(this.MEMPTR);
        }

        protected void CallIndirect()
        {
            this.FetchWordMEMPTR();
            this.Call(this.MEMPTR);
        }

        protected virtual void JumpRelative(sbyte offset)
        {
            this.MEMPTR.Word = (ushort)(this.PC.Word + offset);
            this.Jump(this.MEMPTR);
        }

        protected void JumpRelative(byte offset) => this.JumpRelative((sbyte)offset);

        protected override void Return()
        {
            base.Return();
            this.MEMPTR.Assign(this.PC);
        }

        protected abstract bool ConvertCondition(int flag);

        protected virtual bool JumpConditionalFlag(int flag) => this.JumpConditional(this.ConvertCondition(flag));

        protected virtual bool JumpRelativeConditionalFlag(int flag) => this.JumpRelativeConditional(this.ConvertCondition(flag));

        protected virtual bool ReturnConditionalFlag(int flag) => this.ReturnConditional(this.ConvertCondition(flag));

        protected virtual bool CallConditionalFlag(int flag) => this.CallConditional(this.ConvertCondition(flag));
    }
}
