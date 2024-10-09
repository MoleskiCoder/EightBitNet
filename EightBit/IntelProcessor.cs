// <copyright file="IntelProcessor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

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
                _decodedOpCodes[i] = new((byte)i);
            }
        }

        public event EventHandler<EventArgs>? RaisingHALT;

        public event EventHandler<EventArgs>? RaisedHALT;

        public event EventHandler<EventArgs>? LoweringHALT;

        public event EventHandler<EventArgs>? LoweredHALT;

        public Register16 SP { get; } = new((ushort)Mask.Sixteen);

        public Register16 MEMPTR { get; } = new((ushort)Mask.Sixteen);

        public abstract Register16 AF { get; }

        public ref byte A => ref AF.High;

        public ref byte F => ref AF.Low;

        public abstract Register16 BC { get; }

        public ref byte B => ref BC.High;

        public ref byte C => ref BC.Low;

        public abstract Register16 DE { get; }

        public ref byte D => ref DE.High;

        public ref byte E => ref DE.Low;

        public abstract Register16 HL { get; }

        public ref byte H => ref HL.High;

        public ref byte L => ref HL.Low;

        public ref PinLevel HALT => ref _haltLine;

        public IntelOpCodeDecoded GetDecodedOpCode(byte opCode) => _decodedOpCodes[opCode];

        public virtual void RaiseHALT()
        {
            if (HALT.Lowered())
            {
                OnRaisingHALT();
                HALT.Raise();
                OnRaisedHALT();
            }
        }

        public virtual void LowerHALT()
        {
            if (HALT.Raised())
            {
                OnLoweringHALT();
                HALT.Lower();
                OnLoweredHALT();
            }
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

        protected override void OnRaisedPOWER()
        {
            PC.Word = SP.Word = AF.Word = BC.Word = DE.Word = HL.Word = (ushort)Mask.Sixteen;
            RaiseHALT();
            base.OnRaisedPOWER();
        }

        protected virtual void OnRaisingHALT() => RaisingHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedHALT()
        {
            ++PC.Word; // Release the PC from HALT instruction
            RaisedHALT?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLoweringHALT() => LoweringHALT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredHALT()
        {
            --PC.Word; // Keep the PC on the HALT instruction (i.e. executing NOP)
            LoweredHALT?.Invoke(this, EventArgs.Empty);
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            Jump(0);
        }

        protected sealed override void Push(byte value)
        {
            --SP.Word;
            MemoryWrite(SP, value);
        }

        protected sealed override byte Pop()
        {
            var returned = MemoryRead(SP);
            SP.Word++;
            return returned;
        }

        protected sealed override Register16 GetWord()
        {
            var returned = base.GetWord();
            MEMPTR.Assign(Bus.Address);
            return returned;
        }

        protected sealed override void SetWord(Register16 value)
        {
            base.SetWord(value);
            MEMPTR.Assign(Bus.Address);
        }

        ////

        protected void Restart(byte address)
        {
            MEMPTR.Assign(address, 0);
            Call(MEMPTR);
        }

        protected bool CallConditional(bool condition)
        {
            FetchWordMEMPTR();
            if (condition)
            {
                Call(MEMPTR);
            }

            return condition;
        }

        protected bool JumpConditional(bool condition)
        {
            FetchWordMEMPTR();
            if (condition)
            {
                Jump(MEMPTR);
            }

            return condition;
        }

        protected bool ReturnConditional(bool condition)
        {
            if (condition)
            {
                Return();
            }

            return condition;
        }

        protected void FetchWordMEMPTR()
        {
            FetchWord();
            MEMPTR.Assign(Intermediate);
        }

        protected void JumpIndirect()
        {
            FetchWordMEMPTR();
            Jump(MEMPTR);
        }

        protected void CallIndirect()
        {
            FetchWordMEMPTR();
            Call(MEMPTR);
        }

        protected void JumpRelative(sbyte offset)
        {
            MEMPTR.Word = (ushort)(PC.Word + offset);
            Jump(MEMPTR);
        }

        protected bool JumpRelativeConditional(bool condition)
        {
            Intermediate.Assign(PC);
            ++PC.Word;
            if (condition)
            {
                var offset = (sbyte)MemoryRead(Intermediate);
                JumpRelative(offset);
            }

            return condition;
        }

        protected override sealed void Return()
        {
            base.Return();
            MEMPTR.Assign(PC);
        }
    }
}
