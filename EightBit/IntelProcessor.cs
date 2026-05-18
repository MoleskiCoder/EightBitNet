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
            this.FetchByte();
            return this.HALT.Lowered() ? (byte)0 : this.Bus.Data;
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

        protected void ResetRegisterSet() => this.AF.Joined = this.BC.Joined = this.DE.Joined = this.HL.Joined = (ushort)Mask.Sixteen;

        private void IntelProcessor_RaisedPOWER(object? sender, EventArgs e)
        {
            this.PC.Joined = this.SP.Joined = (ushort)Mask.Sixteen;
            this.ResetRegisterSet();
            this.RaiseHALT();
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DisableInterrupts();
            this.Jump(0);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.DisableInterrupts();
            this.RaiseHALT();
        }

        protected sealed override void Push(byte value)
        {
            this.SP.Decrement();
            this.MemoryWrite(this.SP, value);
        }

        protected sealed override void Pop()
        {
            this.MemoryRead(this.SP);
            this.SP.Increment();
        }

        protected sealed override void GetShort()
        {
            base.GetShort();
            this.MEMPTR.Assign(this.Bus.Address);
        }

        protected sealed override void SetShort(Register16 value)
        {
            base.SetShort(value);
            this.MEMPTR.Assign(this.Bus.Address);
        }

        ////

        protected void ReadMemoryIndirect(Register16 via)
        {
            this.MEMPTR.Assign(via);
            this.ReadMemoryIndirect();
        }

        protected void ReadMemoryIndirect()
        {
            this.Bus.Address.Assign(this.MEMPTR);
            this.MEMPTR.Increment();
            this.MemoryRead();
        }

        protected void WriteMemoryIndirect(Register16 via, byte data)
        {
            this.MEMPTR.Assign(via);
            this.WriteMemoryIndirect(data);
        }

        protected void WriteMemoryIndirect(byte data)
        {
            this.Bus.Address.Assign(this.MEMPTR);
            this.MEMPTR.Increment();
            this.MEMPTR.High = this.Bus.Data = data;
            this.MemoryWrite();
        }

        ////

        protected void Restart(byte address)
        {
            this.MEMPTR.Assign(address);
            this.Call();
        }

        protected virtual void CallConditional(bool condition)
        {
            this.FetchInto(this.MEMPTR);
            if (condition)
            {
                this.Call();
            }
        }

        protected virtual void JumpConditional(bool condition)
        {
            this.FetchInto(this.MEMPTR);
            if (condition)
            {
                this.Jump();
            }
        }

        protected virtual void JumpRelativeConditional(bool condition)
        {
            this.FetchByte();
            if (condition)
            {
                this.JumpRelative(this.Bus.Data);
            }
        }

        protected virtual void ReturnConditional(bool condition)
        {
            if (condition)
            {
                this.Return();
            }
        }

        protected virtual void JumpIndirect()
        {
            this.FetchInto(this.MEMPTR);
            this.Jump();
        }

        protected void Jump() => this.Jump(this.MEMPTR);

        protected void CallIndirect()
        {
            this.FetchInto(this.MEMPTR);
            this.Call();
        }

        protected void Call() => this.Call(this.MEMPTR);

        protected virtual void JumpRelative(sbyte offset)
        {
            this.MEMPTR.Joined = (ushort)(this.PC.Joined + offset);
            this.Jump();
        }

        protected void JumpRelative(byte offset) => this.JumpRelative((sbyte)offset);

        protected override void Return()
        {
            base.Return();
            this.MEMPTR.Assign(this.PC);
        }

        protected abstract bool ConvertCondition(int flag);

        protected virtual void JumpConditionalFlag(int flag) => this.JumpConditional(this.ConvertCondition(flag));

        protected virtual void JumpRelativeConditionalFlag(int flag) => this.JumpRelativeConditional(this.ConvertCondition(flag));

        protected virtual void ReturnConditionalFlag(int flag) => this.ReturnConditional(this.ConvertCondition(flag));

        protected virtual void CallConditionalFlag(int flag) => this.CallConditional(this.ConvertCondition(flag));

        protected virtual void CPL() => this.A = (byte)~this.A;

    }
}
