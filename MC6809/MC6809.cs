// <copyright file="MC6809.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    using EightBit;

    // Uses some information from:
    // http://www.cpu-world.com/Arch/6809.html
    // http://atjs.mbnet.fi/mc6809/Information/6809.htm

    /*
    |---------------|-----------------------------------|
    |   MPU State   |                                   |
    |_______________|   MPU State Definition            |
    |   BA  |   BS  |                                   |
    |_______|_______|___________________________________|
    |   0   |   0   |   Normal (running)                |
    |   0   |   1   |   Interrupt or RESET Acknowledge  |
    |   1   |   0   |   SYNC Acknowledge                |
    |   1   |   1   |   HALT Acknowledge                |
    |-------|-------|-----------------------------------|
    */

    public sealed class MC6809 : BigEndianProcessor
    {
        private const byte RESET_vector = 0xfe; // RESET vector
        private const byte NMI_vector = 0xfc;   // NMI vector
        private const byte SWI_vector = 0xfa;   // SWI vector
        private const byte IRQ_vector = 0xf8;   // IRQ vector
        private const byte FIRQ_vector = 0xf6;  // FIRQ vector
        private const byte SWI2_vector = 0xf4;  // SWI2 vector
        private const byte SWI3_vector = 0xf2;  // SWI3 vector

        private byte cc;
        private byte dp;

        private bool prefix10;
        private bool prefix11;

        public MC6809(Bus bus)
        : base(bus)
        {
            this.RaisedPOWER += this.MC6809_RaisedPOWER;
        }

        private void MC6809_RaisedPOWER(object? sender, EventArgs e)
        {
            this.LowerBA();
            this.LowerBS();
            this.LowerRW();
        }

        #region Pin controls

        #region NMI pin

        private PinLevel nmiLine = PinLevel.Low;

        public ref PinLevel NMI => ref this.nmiLine;

        public event EventHandler<EventArgs>? RaisingNMI;

        public event EventHandler<EventArgs>? RaisedNMI;

        public event EventHandler<EventArgs>? LoweringNMI;

        public event EventHandler<EventArgs>? LoweredNMI;

        private void OnRaisingNMI() => this.RaisingNMI?.Invoke(this, EventArgs.Empty);

        private void OnRaisedNMI() => this.RaisedNMI?.Invoke(this, EventArgs.Empty);

        private void OnLoweringNMI() => this.LoweringNMI?.Invoke(this, EventArgs.Empty);

        private void OnLoweredNMI() => this.LoweredNMI?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseNMI()
        {
            if (this.NMI.Lowered())
            {
                this.OnRaisingNMI();
                this.NMI.Raise();
                this.OnRaisedNMI();
            }
        }

        public void LowerNMI()
        {
            if (this.NMI.Raised())
            {
                this.OnLoweringNMI();
                this.NMI.Lower();
                this.OnLoweredNMI();
            }
        }

        #endregion

        #region FIRQ pin

        private PinLevel firqLine = PinLevel.Low;

        public ref PinLevel FIRQ => ref this.firqLine;

        public event EventHandler<EventArgs>? RaisingFIRQ;

        public event EventHandler<EventArgs>? RaisedFIRQ;

        public event EventHandler<EventArgs>? LoweringFIRQ;

        public event EventHandler<EventArgs>? LoweredFIRQ;

        private void OnRaisingFIRQ() => this.RaisingFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnRaisedFIRQ() => this.RaisedFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnLoweringFIRQ() => this.LoweringFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnLoweredFIRQ() => this.LoweredFIRQ?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseFIRQ()
        {
            if (this.FIRQ.Lowered())
            {
                this.OnRaisingFIRQ();
                this.FIRQ.Raise();
                this.OnRaisedFIRQ();
            }
        }

        public void LowerFIRQ()
        {
            if (this.FIRQ.Raised())
            {
                this.OnLoweringFIRQ();
                this.FIRQ.Lower();
                this.OnLoweredFIRQ();
            }
        }

        #endregion

        #region HALT pin

        private PinLevel haltLine = PinLevel.Low;

        public ref PinLevel HALT => ref this.haltLine;

        public bool Halted => this.HALT.Lowered();

        public event EventHandler<EventArgs>? RaisingHALT;

        public event EventHandler<EventArgs>? RaisedHALT;

        public event EventHandler<EventArgs>? LoweringHALT;

        public event EventHandler<EventArgs>? LoweredHALT;

        private void OnRaisingHALT() => this.RaisingHALT?.Invoke(this, EventArgs.Empty);

        private void OnRaisedHALT() => this.RaisedHALT?.Invoke(this, EventArgs.Empty);

        private void OnLoweringHALT() => this.LoweringHALT?.Invoke(this, EventArgs.Empty);

        private void OnLoweredHALT() => this.LoweredHALT?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseHALT()
        {
            if (this.HALT.Lowered())
            {
                this.OnRaisingHALT();
                this.HALT.Raise();
                this.OnRaisedHALT();
            }
        }

        public void LowerHALT()
        {
            if (this.HALT.Raised())
            {
                this.OnLoweringHALT();
                this.HALT.Lower();
                this.OnLoweredHALT();
            }
        }

        private void SYNC() => this.Halt();

        public void Halt()
        {
            this.LowerHALT();
        }

        public void Proceed()
        {
            this.RaiseHALT();
        }

        #endregion

        #region BA pin

        private PinLevel baLine = PinLevel.Low;

        public ref PinLevel BA => ref this.baLine;

        private PinLevel bsLine = PinLevel.Low;

        public event EventHandler<EventArgs>? RaisingBA;

        public event EventHandler<EventArgs>? RaisedBA;

        public event EventHandler<EventArgs>? LoweringBA;

        public event EventHandler<EventArgs>? LoweredBA;

        private void OnRaisingBA() => this.RaisingBA?.Invoke(this, EventArgs.Empty);

        private void OnRaisedBA() => this.RaisedBA?.Invoke(this, EventArgs.Empty);

        private void OnLoweringBA() => this.LoweringBA?.Invoke(this, EventArgs.Empty);

        private void OnLoweredBA() => this.LoweredBA?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseBA()
        {
            if (this.BA.Lowered())
            {
                this.OnRaisingBA();
                this.BA.Raise();
                this.OnRaisedBA();
            }
        }

        public void LowerBA()
        {
            if (this.BA.Raised())
            {
                this.OnLoweringBA();
                this.BA.Lower();
                this.OnLoweredBA();
            }
        }

        #endregion

        #region BS pin

        public ref PinLevel BS => ref this.bsLine;

        public event EventHandler<EventArgs>? RaisingBS;

        public event EventHandler<EventArgs>? RaisedBS;

        public event EventHandler<EventArgs>? LoweringBS;

        public event EventHandler<EventArgs>? LoweredBS;

        private void OnRaisingBS() => this.RaisingBS?.Invoke(this, EventArgs.Empty);

        private void OnRaisedBS() => this.RaisedBS?.Invoke(this, EventArgs.Empty);

        private void OnLoweringBS() => this.LoweringBS?.Invoke(this, EventArgs.Empty);

        private void OnLoweredBS() => this.LoweredBS?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseBS()
        {
            if (this.BS.Lowered())
            {
                this.OnRaisingBS();
                this.BS.Raise();
                this.OnRaisedBS();
            }
        }

        public void LowerBS()
        {
            if (this.BS.Raised())
            {
                this.OnLoweringBS();
                this.BS.Lower();
                this.OnLoweredBS();
            }
        }

        #endregion

        #region RW pin

        private PinLevel rwLine = PinLevel.Low;

        public ref PinLevel RW => ref this.rwLine;

        public event EventHandler<EventArgs>? RaisingRW;

        public event EventHandler<EventArgs>? RaisedRW;

        public event EventHandler<EventArgs>? LoweringRW;

        public event EventHandler<EventArgs>? LoweredRW;

        private void OnRaisingRW() => this.RaisingRW?.Invoke(this, EventArgs.Empty);

        private void OnRaisedRW() => this.RaisedRW?.Invoke(this, EventArgs.Empty);

        private void OnLoweringRW() => this.LoweringRW?.Invoke(this, EventArgs.Empty);

        private void OnLoweredRW() => this.LoweredRW?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseRW()
        {
            if (this.RW.Lowered())
            {
                this.OnRaisingRW();
                this.RW.Raise();
                this.OnRaisedRW();
            }
        }

        public void LowerRW()
        {
            if (this.RW.Raised())
            {
                this.OnLoweringRW();
                this.RW.Lower();
                this.OnLoweredRW();
            }
        }

        #endregion

        #endregion

        #region Registers

        public Register16 D { get; } = new();

        public ref byte A => ref this.D.High;

        public ref byte B => ref this.D.Low;

        public Register16 X { get; } = new();

        public Register16 Y { get; } = new();

        public Register16 U { get; } = new();

        public Register16 S { get; } = new();

        public ref byte DP => ref this.dp;

        public ref byte CC => ref this.cc;

        #endregion

        #region Status (etc.) bit twiddling

        public int EntireRegisterSet => this.CC & (byte)StatusBits.EF;

        public int FastInterruptMasked => this.CC & (byte)StatusBits.FF;

        public int HalfCarry => this.CC & (byte)StatusBits.HF;

        public int InterruptMasked => this.CC & (byte)StatusBits.IF;

        public int Negative => this.CC & (byte)StatusBits.NF;

        public int Zero => this.CC & (byte)StatusBits.ZF;

        public int Overflow => this.CC & (byte)StatusBits.VF;

        public int Carry => this.CC & (byte)StatusBits.CF;

        private bool LS => this.Carry != 0 || this.Zero != 0;               // (C OR Z)

        private bool HI => !this.LS;                                        // !(C OR Z)

        private bool LT => (this.Negative >> 3 ^ this.Overflow >> 1) != 0;  // (N XOR V)

        private bool GE => !this.LT;                                        // !(N XOR V)

        private bool LE => this.Zero != 0 || this.LT;                       // (Z OR (N XOR V))

        private bool GT => !this.LE;                                        // !(Z OR (N XOR V))

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private byte AdjustZero(byte datum) => ClearBit(this.CC, StatusBits.ZF, datum);

        private byte AdjustZero(ushort datum) => ClearBit(this.CC, StatusBits.ZF, datum);

        private byte AdjustZero(Register16 datum) => this.AdjustZero(datum.Word);

        private byte AdjustNegative(byte datum) => SetBit(this.CC, StatusBits.NF, datum & (byte)Bits.Bit7);

        private byte AdjustNegative(ushort datum) => SetBit(this.CC, StatusBits.NF, datum & (ushort)Bits.Bit15);

        private byte AdjustNZ(byte datum)
        {
            this.CC = this.AdjustZero(datum);
            return this.AdjustNegative(datum);
        }

        private byte AdjustNZ(ushort datum)
        {
            this.CC = this.AdjustZero(datum);
            return this.AdjustNegative(datum);
        }

        private byte AdjustNZ(Register16 datum) => this.AdjustNZ(datum.Word);

        private byte AdjustCarry(ushort datum) => SetBit(this.CC, StatusBits.CF, datum & (ushort)Bits.Bit8);           // 8-bit addition

        private byte AdjustCarry(uint datum) => SetBit(this.CC, StatusBits.CF, (int)(datum & (uint)Bits.Bit16));       // 16-bit addition

        private byte AdjustCarry(Register16 datum) => this.AdjustCarry(datum.Word);

        private byte AdjustOverflow(byte before, byte data, Register16 after)
        {
            var lowAfter = after.Low;
            var highAfter = after.High;
            return SetBit(this.CC, StatusBits.VF, (before ^ data ^ lowAfter ^ highAfter << 7) & (int)Bits.Bit7);
        }

        private byte AdjustOverflow(ushort before, ushort data, uint after)
        {
            var lowAfter = (ushort)(after & (uint)Mask.Sixteen);
            var highAfter = (ushort)(after >> 16);
            return SetBit(this.CC, StatusBits.VF, (before ^ data ^ lowAfter ^ highAfter << 15) & (int)Bits.Bit15);
        }

        private byte AdjustHalfCarry(byte before, byte data, byte after) => SetBit(this.CC, StatusBits.HF, (before ^ data ^ after) & (int)Bits.Bit4);

        private byte AdjustAddition(byte before, byte data, Register16 after)
        {
            var result = after.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            this.CC = this.AdjustOverflow(before, data, after);
            return this.AdjustHalfCarry(before, data, result);
        }

        private byte AdjustAddition(ushort before, ushort data, uint after)
        {
            this.Intermediate.Word = (ushort)after;
            this.CC = this.AdjustNZ(this.Intermediate.Word);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustAddition(Register16 before, Register16 data, uint after) => this.AdjustAddition(before.Word, data.Word, after);

        private byte AdjustSubtraction(byte before, byte data, Register16 after)
        {
            var result = after.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustSubtraction(ushort before, ushort data, uint after)
        {
            this.Intermediate.Word = (ushort)after;
            this.CC = this.AdjustNZ(this.Intermediate.Word);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustSubtraction(Register16 before, Register16 data, uint after) => this.AdjustSubtraction(before.Word, data.Word, after);

        #endregion

        #region Interrupt etc. handlers

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.RaiseNMI();
            this.LowerBA();
            this.RaiseBS();
            this.DP = 0;
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetBit(this.CC, StatusBits.FF);  // Disable FIRQ
            this.GetWordPaged(0xff, RESET_vector);
            this.Jump(this.Intermediate);
            this.Tick(10);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.LowerBA();
            this.RaiseBS();
            this.SaveEntireRegisterState();
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.GetWordPaged(0xff, IRQ_vector);
            this.Jump(this.Intermediate);
            this.Tick(12);
        }

        private void HandleHALT()
        {
            this.RaiseBA();
            this.RaiseBS();
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.LowerBA();
            this.RaiseBS();
            this.SaveEntireRegisterState();
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetBit(this.CC, StatusBits.FF);  // Disable FIRQ
            this.GetWordPaged(0xff, NMI_vector);
            this.Jump(this.Intermediate);
            this.Tick(12);
        }

        private void HandleFIRQ()
        {
            this.RaiseFIRQ();
            this.LowerBA();
            this.RaiseBS();
            this.SavePartialRegisterState();
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetBit(this.CC, StatusBits.FF);  // Disable FIRQ
            this.GetWordPaged(0xff, FIRQ_vector);
            this.Jump(this.Intermediate);
            this.Tick(12);
        }

        #endregion

        #region Bus control

        protected override void BusWrite()
        {
            this.LowerRW();
            base.BusWrite();
        }

        protected override byte BusRead()
        {
            this.RaiseRW();
            return base.BusRead();
        }

        #endregion

        #region Push/Pop

        protected override byte Pop() => this.PopS();

        protected override void Push(byte value) => this.PushS(value);

        private void Push(Register16 stack, byte value)
        {
            stack.Decrement();
            this.MemoryWrite(stack, value);
        }

        private void PushS(byte value) => this.Push(this.S, value);

        private void PushWord(Register16 stack, Register16 value)
        {
            this.Push(stack, value.Low);
            this.Push(stack, value.High);
        }

        private byte Pop(Register16 stack)
        {
            _ = this.MemoryRead(stack);
            _ = stack.Increment();
            return this.Bus.Data;
        }

        private byte PopS() => this.Pop(this.S);

        private Register16 PopWord(Register16 stack)
        {
            this.Pop(stack);
            this.Intermediate.High = this.Bus.Data;
            this.Pop(stack);
            this.Intermediate.Low = this.Bus.Data;
            return this.Intermediate;
        }

        #endregion

        #region Addressing modes

        private void RelativeByteAddress()
        {
            this.FetchByte();
            var offset = (sbyte)this.Bus.Data;
            this.Intermediate.Word = (ushort)(this.PC.Word + offset);
        }

        private void RelativeWordAddress()
        {
            this.FetchWord();
            var offset = (short)this.Intermediate.Word;
            this.Intermediate.Word = (ushort)(this.PC.Word + offset);
        }

        private void DirectAddress()
        {
            this.FetchByte();
            this.Intermediate.Assign(this.Bus.Data, this.DP);
        }

        private void ExtendedAddress() => this.FetchWord();

        private Register16 RR(int which)
        {
            return which switch
            {
                0b00 => this.X,
                0b01 => this.Y,
                0b10 => this.U,
                0b11 => this.S,
                _ => throw new ArgumentOutOfRangeException(nameof(which), which, "Which does not specify a valid register"),
            };
        }

        private void IndexedAddress()
        {
            this.FetchByte();
            var type = this.Bus.Data;
            var r = this.RR((type & (byte)(Bits.Bit6 | Bits.Bit5)) >> 5);

            if ((type & (byte)Bits.Bit7) != 0)
            {
                switch (type & (byte)Mask.Four)
                {
                    case 0b0000: // ,R+
                        this.Tick(2);
                        this.Intermediate.Word = r.Word++;
                        break;
                    case 0b0001: // ,R++
                        this.Tick(3);
                        this.Intermediate.Word = r.Word;
                        r.Word += 2;
                        break;
                    case 0b0010: // ,-R
                        this.Tick(2);
                        this.Intermediate.Word = --r.Word;
                        break;
                    case 0b0011: // ,--R
                        this.Tick(3);
                        r.Word -= 2;
                        this.Intermediate.Word = r.Word;
                        break;
                    case 0b0100: // ,R
                        this.Intermediate.Word = r.Word;
                        break;
                    case 0b0101: // B,R
                        this.Tick();
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.B);
                        break;
                    case 0b0110: // A,R
                        this.Tick();
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.A);
                        break;
                    case 0b1000: // n,R (eight-bit)
                        this.Tick();
                        this.FetchByte();
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.Bus.Data);
                        break;
                    case 0b1001: // n,R (sixteen-bit)
                        this.Tick(4);
                        this.FetchWord();
                        this.Intermediate.Word += r.Word;
                        break;
                    case 0b1011: // D,R
                        this.Tick(4);
                        this.Intermediate.Word = (ushort)(r.Word + this.D.Word);
                        break;
                    case 0b1100: // n,PCR (eight-bit)
                        this.Tick();
                        this.RelativeByteAddress();
                        break;
                    case 0b1101: // n,PCR (sixteen-bit)
                        this.Tick(2);
                        this.RelativeWordAddress();
                        break;
                    case 0b1111: // [n]
                        this.Tick(2);
                        this.ExtendedAddress();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid index type");
                }

                var indirect = type & (byte)Bits.Bit4;
                if (indirect != 0)
                {
                    this.Tick(3);
                    var address = this.Intermediate;
                    this.GetWord(address);
                }
            }
            else
            {
                // EA = ,R + 5-bit offset
                this.Tick();
                this.Intermediate.Word = (ushort)(r.Word + SignExtend(5, (byte)(type & (byte)Mask.Five)));
            }
        }

        private void ImmediateByte() => this.FetchByte();

        private void DirectByte()
        {
            this.DirectAddress();
            this.MemoryRead(this.Intermediate);
        }

        private void IndexedByte()
        {
            this.IndexedAddress();
            this.MemoryRead(this.Intermediate);
        }

        private void ExtendedByte()
        {
            this.ExtendedAddress();
            this.MemoryRead(this.Intermediate);
        }

        private void ImmediateWord() => this.FetchWord();

        private void DirectWord()
        {
            this.DirectAddress();
            this.GetWord(this.Intermediate);
        }

        private void IndexedWord()
        {
            this.IndexedAddress();
            this.GetWord(this.Intermediate);
        }

        private void ExtendedWord()
        {
            this.ExtendedAddress();
            this.GetWord(this.Intermediate);
        }

        #endregion

        #region Load/store 8 or 16-bit data

        private void LDA() => this.A = this.Through(this.Bus.Data);
        private void LDB() => this.B = this.Through(this.Bus.Data);

        private byte Through(byte data)
        {
            this.CC = ClearBit(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
            return data;
        }

        private void LDD()
        {
            this.Through(this.Intermediate);
            this.D.Assign(this.Intermediate);
        }

        private void LDS()
        {
            this.Through(this.Intermediate);
            this.S.Assign(this.Intermediate);
        }

        private void LDU()
        {
            this.Through(this.Intermediate);
            this.U.Assign(this.Intermediate);
        }

        private void LDX()
        {
            this.Through(this.Intermediate);
            this.X.Assign(this.Intermediate);
        }

        private void LDY()
        {
            this.Through(this.Intermediate);
            this.Y.Assign(this.Intermediate);
        }

        private void Through(Register16 data)
        {
            this.CC = ClearBit(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
        }

        private void STA() => this.Store(this.B);
        private void STB() => this.Store(this.B);

        private void Store(byte data) => this.MemoryWrite(this.Intermediate, this.Through(data));

        private void STD()
        {
            this.Through(this.D);
            this.SetWord(this.Intermediate, this.D);
        }

        private void STU()
        {
            this.Through(this.U);
            this.SetWord(this.Intermediate, this.U);
        }

        private void STS()
        {
            this.Through(this.S);
            this.SetWord(this.Intermediate, this.S);
        }

        private void STX()
        {
            this.Through(this.X);
            this.SetWord(this.Intermediate, this.X);
        }

        private void STY()
        {
            this.Through(this.Y);
            this.SetWord(this.Intermediate, this.Y);
        }

        #endregion

        #region Branching

        private void LBSR()
        {
            this.RelativeWordAddress();
            this.Call(this.Intermediate);
        }

        private void BSR()
        {
            this.RelativeByteAddress();
            this.Call(this.Intermediate);
        }

        private void BRA() => this.BranchShort(true);
        private void BRN() => this.BranchShort(false);
        private void BHI() => this.BranchShort(this.HI);
        private void BLS() => this.BranchShort(this.LS);
        private void BCC() => this.BranchShort(this.Carry == 0);
        private void BCS() => this.BranchShort(this.Carry != 0);
        private void BNE() => this.BranchShort(this.Zero == 0);
        private void BEQ() => this.BranchShort(this.Zero != 0);
        private void BVC() => this.BranchShort(this.Overflow == 0);
        private void BVS() => this.BranchShort(this.Overflow != 0);
        private void BPL() => this.BranchShort(this.Negative == 0);
        private void BMI() => this.BranchShort(this.Negative != 0);
        private void BGE() => this.BranchShort(this.GE);
        private void BLT() => this.BranchShort(this.LT);
        private void BGT() => this.BranchShort(this.GT);
        private void BLE() => this.BranchShort(this.LE);

        private void LBRA() => this.BranchLong(true);
        private void LBRN() => this.BranchLong(false);
        private void LBHI() => this.BranchLong(this.HI);
        private void LBLS() => this.BranchLong(this.LS);
        private void LBCC() => this.BranchLong(this.Carry == 0);
        private void LBCS() => this.BranchLong(this.Carry != 0);
        private void LBNE() => this.BranchLong(this.Zero == 0);
        private void LBEQ() => this.BranchLong(this.Zero != 0);
        private void LBVC() => this.BranchLong(this.Overflow == 0);
        private void LBVS() => this.BranchLong(this.Overflow != 0);
        private void LBPL() => this.BranchLong(this.Negative == 0);
        private void LBMI() => this.BranchLong(this.Negative != 0);
        private void LBGE() => this.BranchLong(this.GE);
        private void LBLT() => this.BranchLong(this.LT);
        private void LBGT() => this.BranchLong(this.GT);
        private void LBLE() => this.BranchLong(this.LE);

        private void BranchShort(bool condition)
        {
            this.RelativeByteAddress();
            this.Branch(this.Intermediate, condition);
        }

        private void BranchLong(bool condition)
        {
            this.RelativeWordAddress();
            if (this.Branch(this.Intermediate, condition))
            {
                this.Tick();
            }
        }

        private bool Branch(Register16 destination, bool condition)
        {
            if (condition)
            {
                this.Jump(destination);
            }

            return condition;
        }

        #endregion

        #region Save/restore register state

        private void SaveEntireRegisterState()
        {
            this.CC = SetBit(this.CC, StatusBits.EF);
            this.SaveRegisterState();
        }

        private void SavePartialRegisterState()
        {
            this.CC = ClearBit(this.CC, StatusBits.EF);
            this.SaveRegisterState();
        }

        private void SaveRegisterState() => this.PSH(this.S, this.EntireRegisterSet != 0 ? (byte)Mask.Eight : (byte)0b10000001);

        private void RestoreRegisterState() => this.PUL(this.S, this.EntireRegisterSet != 0 ? (byte)Mask.Eight : (byte)0b10000001);

        private void PSHS() => this.PSH(this.S);
        private void PSHU() => this.PSH(this.U);

        private void PSH(Register16 stack) => this.PSH(stack, this.Bus.Data);

        private void PSH(Register16 stack, byte data)
        {
            if ((data & (byte)Bits.Bit7) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.PC);
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                this.Tick(2);

                // Pushing to the S stack means we must be pushing U
                this.PushWord(stack, ReferenceEquals(stack, this.S) ? this.U : this.S);
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.Y);
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.X);
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                this.Tick();
                this.Push(stack, this.DP);
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                this.Tick();
                this.Push(stack, this.B);
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                this.Tick();
                this.Push(stack, this.A);
            }

            if ((data & (byte)Bits.Bit0) != 0)
            {
                this.Tick();
                this.Push(stack, this.CC);
            }
        }

        private void PULU() => this.PUL(this.U);
        private void PULS() => this.PUL(this.S);

        private void PUL(Register16 stack) => this.PUL(stack, this.Bus.Data);

        private void PUL(Register16 stack, byte data)
        {
            if ((data & (byte)Bits.Bit0) != 0)
            {
                this.Tick();
                this.Pop(stack);
                this.CC = this.Bus.Data;
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                this.Tick();
                this.Pop(stack);
                this.A = this.Bus.Data;
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                this.Tick();
                this.Pop(stack);
                this.B = this.Bus.Data;
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                this.Tick();
                this.Pop(stack);
                this.DP = this.Bus.Data;
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                this.Tick(2);
                this.X.Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                this.Tick(2);
                this.Y.Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                this.Tick(2);

                // Pulling from the S stack means we must be pulling U
                (ReferenceEquals(stack, this.S) ? this.U : this.S).Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit7) != 0)
            {
                this.Tick(2);
                this.PC.Word = this.PopWord(stack).Word;
            }
        }

        #endregion

        #region 8-bit register transfers

        private ref byte ReferenceTransfer8(int specifier)
        {
            switch (specifier)
            {
                case 0b1000:
                    return ref this.A;
                case 0b1001:
                    return ref this.B;
                case 0b1010:
                    return ref this.CC;
                case 0b1011:
                    return ref this.DP;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "Invalid specifier");
            }
        }

        private Register16 ReferenceTransfer16(int specifier)
        {
            return specifier switch
            {
                0b0000 => this.D,
                0b0001 => this.X,
                0b0010 => this.Y,
                0b0011 => this.U,
                0b0100 => this.S,
                0b0101 => this.PC,
                _ => throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "Invalid specifier"),
            };
        }

        private void EXG()
        {
            var data = this.Bus.Data;

            var leftSpecifier = HighNibble(data);
            var leftType = leftSpecifier & (int)Bits.Bit3;

            var rightSpecifier = LowNibble(data);
            var rightType = rightSpecifier & (int)Bits.Bit3;

            if (leftType == 0)
            {
                var leftRegister = this.ReferenceTransfer16(leftSpecifier);
                if (rightType == 0)
                {
                    var rightRegister = this.ReferenceTransfer16(rightSpecifier);
                    (leftRegister.Word, rightRegister.Word) = (rightRegister.Word, leftRegister.Word);
                }
                else
                {
                    ref var rightRegister = ref this.ReferenceTransfer8(rightSpecifier);
                    (leftRegister.Low, rightRegister) = (rightRegister, leftRegister.Low);
                    leftRegister.High = (byte)Mask.Eight;
                }
            }
            else
            {
                ref var leftRegister = ref this.ReferenceTransfer8(leftSpecifier);
                if (rightType == 0)
                {
                    var rightRegister = this.ReferenceTransfer16(rightSpecifier);
                    (leftRegister, rightRegister.Low) = (rightRegister.Low, leftRegister);
                    rightRegister.High = (byte)Mask.Eight;
                }
                else
                {
                    ref var rightRegister = ref this.ReferenceTransfer8(rightSpecifier);
                    (leftRegister, rightRegister) = (rightRegister, leftRegister);
                }
            }
        }

        private void TFR()
        {
            var data = this.Bus.Data;

            var sourceSpecifier = HighNibble(data);
            var sourceType = sourceSpecifier & (int)Bits.Bit3;

            var destinationSpecifier = LowNibble(data);
            var destinationType = destinationSpecifier & (int)Bits.Bit3;

            if (sourceType == 0)
            {
                var sourceRegister = this.ReferenceTransfer16(sourceSpecifier);
                if (destinationType == 0)
                {
                    var destinationRegister = this.ReferenceTransfer16(destinationSpecifier);
                    destinationRegister.Word = sourceRegister.Word;
                }
                else
                {
                    ref var destinationRegister = ref this.ReferenceTransfer8(destinationSpecifier);
                    destinationRegister = sourceRegister.Low;
                }
            }
            else
            {
                ref var sourceRegister = ref this.ReferenceTransfer8(sourceSpecifier);
                if (destinationType == 0)
                {
                    var destinationRegister = this.ReferenceTransfer16(destinationSpecifier);
                    destinationRegister.Low = sourceRegister;
                    destinationRegister.High = (byte)Mask.Eight;
                }
                else
                {
                    ref var destinationRegister = ref this.ReferenceTransfer8(destinationSpecifier);
                    destinationRegister = sourceRegister;
                }
            }
        }

        #endregion

        #region Instruction execution

        public override void PoweredStep()
        {
            this.prefix10 = this.prefix11 = false;
            if (this.Halted)
            {
                this.HandleHALT();
            }
            else if (this.RESET.Lowered())
            {
                this.HandleRESET();
            }
            else if (this.NMI.Lowered())
            {
                this.HandleNMI();
            }
            else if (this.FIRQ.Lowered() && this.FastInterruptMasked == 0)
            {
                this.HandleFIRQ();
            }
            else if (this.INT.Lowered() && this.InterruptMasked == 0)
            {
                this.HandleINT();
            }
            else
            {
                this.FetchByte();
                this.Execute(this.Bus.Data);
            }
        }

        public override void Execute()
        {
            this.LowerBA();
            this.LowerBS();
            if (this.prefix10)
            {
                this.Execute10();
            }
            else if (this.prefix11)
            {
                this.Execute11();
            }
            else
            {
                this.ExecuteUnprefixed();
            }
        }

        #endregion

        #region Instruction dispatching

        private void ExecuteUnprefixed()
        {
            switch (this.OpCode)
            {
                case 0x10: this.Prefix10(); break;
                case 0x11: this.Prefix11(); break;

                // ABX
                case 0x3a: this.Tick(3); this.ABX(); break;                         // ABX (inherent)

                // ADC
                case 0x89: this.Tick(2); this.ImmediateByte(); this.ADCA(); break;  // ADC (ADCA immediate)
                case 0x99: this.Tick(4); this.DirectByte(); this.ADCA(); break;     // ADC (ADCA direct)
                case 0xa9: this.Tick(4); this.IndexedByte(); this.ADCA(); break;    // ADC (ADCA indexed)
                case 0xb9: this.Tick(4); this.ExtendedByte(); this.ADCA(); break;   // ADC (ADCA extended)

                case 0xc9: this.Tick(2); this.ImmediateByte(); this.ADCB(); break;  // ADC (ADCB immediate)
                case 0xd9: this.Tick(4); this.DirectByte(); this.ADCB(); break;     // ADC (ADCB direct)
                case 0xe9: this.Tick(4); this.IndexedByte(); this.ADCB(); break;    // ADC (ADCB indexed)
                case 0xf9: this.Tick(4); this.ExtendedByte(); this.ADCB(); break;   // ADC (ADCB extended)

                // ADD
                case 0x8b: this.Tick(2); this.ImmediateByte(); this.ADDA(); break;  // ADD (ADDA immediate)
                case 0x9b: this.Tick(4); this.DirectByte(); this.ADDA(); break;     // ADD (ADDA direct)
                case 0xab: this.Tick(4); this.IndexedByte(); this.ADDA(); break;    // ADD (ADDA indexed)
                case 0xbb: this.Tick(5); this.ExtendedByte(); this.ADDA(); break;   // ADD (ADDA extended)

                case 0xcb: this.Tick(2); this.ImmediateByte(); this.ADDB(); break;  // ADD (ADDB immediate)
                case 0xdb: this.Tick(4); this.DirectByte(); this.ADDB(); break;     // ADD (ADDB direct)
                case 0xeb: this.Tick(4); this.IndexedByte(); this.ADDB(); break;    // ADD (ADDB indexed)
                case 0xfb: this.Tick(5); this.ExtendedByte(); this.ADDB(); break;   // ADD (ADDB extended)

                case 0xc3: this.Tick(4); this.ImmediateWord(); this.ADDD(); break;  // ADD (ADDD immediate)
                case 0xd3: this.Tick(6); this.DirectWord(); this.ADDD(); break;     // ADD (ADDD direct)
                case 0xe3: this.Tick(6); this.IndexedWord(); this.ADDD(); break;    // ADD (ADDD indexed)
                case 0xf3: this.Tick(7); this.ExtendedWord(); this.ADDD(); break;   // ADD (ADDD extended)

                // AND
                case 0x84: this.Tick(2); this.ImmediateByte(); this.ANDA(); break;  // AND (ANDA immediate)
                case 0x94: this.Tick(4); this.DirectByte(); this.ANDA(); break;     // AND (ANDA direct)
                case 0xa4: this.Tick(4); this.IndexedByte(); this.ANDA(); break;    // AND (ANDA indexed)
                case 0xb4: this.Tick(5); this.ExtendedByte(); this.ANDA(); break;   // AND (ANDA extended)

                case 0xc4: this.Tick(2); this.ImmediateByte(); this.ANDB(); break;  // AND (ANDB immediate)
                case 0xd4: this.Tick(4); this.DirectByte(); this.ANDB(); break;     // AND (ANDB direct)
                case 0xe4: this.Tick(4); this.IndexedByte(); this.ANDB(); break;    // AND (ANDB indexed)
                case 0xf4: this.Tick(5); this.ExtendedByte(); this.ANDB(); break;   // AND (ANDB extended)

                case 0x1c: this.Tick(3); this.ImmediateByte(); this.ANDCC(); break; // AND (ANDCC immediate)

                // ASL/LSL
                case 0x08: this.Tick(6); this.DirectByte(); this.ASL(); break;      // ASL (direct)
                case 0x48: this.Tick(2); this.ASLA(); break;                        // ASL (ASLA inherent)
                case 0x58: this.Tick(2); this.ASLB(); break;                        // ASL (ASLB inherent)
                case 0x68: this.Tick(6); this.IndexedByte(); this.ASL(); break;     // ASL (indexed)
                case 0x78: this.Tick(7); this.ExtendedByte(); this.ASL(); break;    // ASL (extended)

                // ASR
                case 0x07: this.Tick(6); this.DirectByte(); this.ASR(); break;      // ASR (direct)
                case 0x47: this.Tick(2); this.ASRA(); break;                        // ASR (ASRA inherent)
                case 0x57: this.Tick(2); this.ASRB(); break;                        // ASR (ASRB inherent)
                case 0x67: this.Tick(6); this.IndexedByte(); this.ASR(); break;     // ASR (indexed)
                case 0x77: this.Tick(7); this.ExtendedByte(); this.ASR(); break;    // ASR (extended)

                // BIT
                case 0x85: this.Tick(2); this.ImmediateByte(); this.BITA(); break;  // BIT (BITA immediate)
                case 0x95: this.Tick(4); this.DirectByte(); this.BITA(); break;     // BIT (BITA direct)
                case 0xa5: this.Tick(4); this.IndexedByte(); this.BITA(); break;    // BIT (BITA indexed)
                case 0xb5: this.Tick(5); this.ExtendedByte(); this.BITA(); break;   // BIT (BITA extended)

                case 0xc5: this.Tick(2); this.ImmediateByte(); this.BITB(); break;  // BIT (BITB immediate)
                case 0xd5: this.Tick(4); this.DirectByte(); this.BITB(); break;     // BIT (BITB direct)
                case 0xe5: this.Tick(4); this.IndexedByte(); this.BITB(); break;    // BIT (BITB indexed)
                case 0xf5: this.Tick(5); this.ExtendedByte(); this.BITB(); break;   // BIT (BITB extended)

                // CLR
                case 0x0f: this.Tick(6); this.DirectAddress(); this.CLR(); break;   // CLR (direct)
                case 0x4f: this.Tick(2); this.CLRA(); break;                        // CLR (CLRA implied)
                case 0x5f: this.Tick(2); this.CLRB(); break;                        // CLR (CLRB implied)
                case 0x6f: this.Tick(6); this.IndexedAddress(); this.CLR(); break;  // CLR (indexed)
                case 0x7f: this.Tick(7); this.ExtendedAddress(); this.CLR(); break; // CLR (extended)

                // CMP

                // CMPA
                case 0x81: this.Tick(2); this.ImmediateByte(); this.CMPA(); break;  // CMP (CMPA, immediate)
                case 0x91: this.Tick(4); this.DirectByte(); this.CMPA(); break;     // CMP (CMPA, direct)
                case 0xa1: this.Tick(4); this.IndexedByte(); this.CMPA(); break;    // CMP (CMPA, indexed)
                case 0xb1: this.Tick(5); this.ExtendedByte(); this.CMPA(); break;   // CMP (CMPA, extended)

                // CMPB
                case 0xc1: this.Tick(2); this.ImmediateByte(); this.CMPB(); break;  // CMP (CMPB, immediate)
                case 0xd1: this.Tick(4); this.DirectByte(); this.CMPB(); break;     // CMP (CMPB, direct)
                case 0xe1: this.Tick(4); this.IndexedByte(); this.CMPB(); break;    // CMP (CMPB, indexed)
                case 0xf1: this.Tick(5); this.ExtendedByte(); this.CMPB(); break;   // CMP (CMPB, extended)

                // CMPX
                case 0x8c: this.Tick(4); this.ImmediateWord(); this.CMPX(); break;  // CMP (CMPX, immediate)
                case 0x9c: this.Tick(6); this.DirectWord(); this.CMPX(); break;     // CMP (CMPX, direct)
                case 0xac: this.Tick(6); this.IndexedWord(); this.CMPX(); break;    // CMP (CMPX, indexed)
                case 0xbc: this.Tick(7); this.ExtendedWord(); this.CMPX(); break;   // CMP (CMPX, extended)

                // COM
                case 0x03: this.Tick(6); this.DirectByte(); this.COM(); break;      // COM (direct)
                case 0x43: this.Tick(2); this.COMA(); break;                        // COM (COMA inherent)
                case 0x53: this.Tick(2); this.COMB(); break;                        // COM (COMB inherent)
                case 0x63: this.Tick(6); this.IndexedByte(); this.COM(); break;     // COM (indexed)
                case 0x73: this.Tick(7); this.ExtendedByte(); this.COM(); break;    // COM (extended)

                // CWAI
                case 0x3c: this.Tick(11); this.DirectByte(); this.CWAI(); break;    // CWAI (direct)

                // DAA
                case 0x19: this.Tick(2); this.DAA(); break;                         // DAA (inherent)

                // DEC
                case 0x0a: this.Tick(6); this.DirectByte(); this.DEC(); break;      // DEC (direct)
                case 0x4a: this.Tick(2); this.DECA(); break;                        // DEC (DECA inherent)
                case 0x5a: this.Tick(2); this.DECB(); break;                        // DEC (DECB inherent)
                case 0x6a: this.Tick(6); this.IndexedByte(); this.DEC(); break;     // DEC (indexed)
                case 0x7a: this.Tick(7); this.ExtendedByte(); this.DEC(); break;    // DEC (extended)

                // EOR

                // EORA
                case 0x88: this.Tick(2); this.ImmediateByte(); this.EORA(); break;  // EOR (EORA immediate)
                case 0x98: this.Tick(4); this.DirectByte(); this.EORA(); break;     // EOR (EORA direct)
                case 0xa8: this.Tick(4); this.IndexedByte(); this.EORA(); break;    // EOR (EORA indexed)
                case 0xb8: this.Tick(5); this.ExtendedByte(); this.EORA(); break;   // EOR (EORA extended)

                // EORB
                case 0xc8: this.Tick(2); this.ImmediateByte(); this.EORB(); break;  // EOR (EORB immediate)
                case 0xd8: this.Tick(4); this.DirectByte(); this.EORB(); break;     // EOR (EORB direct)
                case 0xe8: this.Tick(4); this.IndexedByte(); this.EORB(); break;    // EOR (EORB indexed)
                case 0xf8: this.Tick(5); this.ExtendedByte(); this.EORB(); break;   // EOR (EORB extended)

                // EXG
                case 0x1e: this.Tick(8); this.ImmediateByte(); this.EXG(); break;   // EXG (R1,R2 immediate)

                // INC
                case 0x0c: this.Tick(6); this.DirectByte(); this.INC(); break;      // INC (direct)
                case 0x4c: this.Tick(2); this.INCA(); break;                        // INC (INCA inherent)
                case 0x5c: this.Tick(2); this.INCB(); break;                        // INC (INCB inherent)
                case 0x6c: this.Tick(6); this.IndexedByte(); this.INC(); break;     // INC (indexed)
                case 0x7c: this.Tick(7); this.ExtendedByte(); this.INC(); break;    // INC (extended)

                // JMP
                case 0x0e: this.Tick(6); this.DirectAddress(); this.JMP(); break;   // JMP (direct)
                case 0x6e: this.Tick(6); this.IndexedAddress(); this.JMP(); break;  // JMP (indexed)
                case 0x7e: this.Tick(7); this.ExtendedAddress(); this.JMP(); break; // JMP (extended)

                // JSR
                case 0x9d: this.Tick(6); this.DirectAddress(); this.JSR(); break;   // JSR (direct)
                case 0xad: this.Tick(6); this.IndexedAddress(); this.JSR(); break;  // JSR (indexed)
                case 0xbd: this.Tick(7); this.ExtendedAddress(); this.JSR(); break; // JSR (extended)

                // LD

                // LDA
                case 0x86: this.Tick(2); this.ImmediateByte(); this.LDA(); break;   // LD (LDA immediate)
                case 0x96: this.Tick(4); this.DirectByte(); this.LDA(); break;      // LD (LDA direct)
                case 0xa6: this.Tick(4); this.IndexedByte(); this.LDA(); break;     // LD (LDA indexed)
                case 0xb6: this.Tick(5); this.ExtendedByte(); this.LDA(); break;    // LD (LDA extended)

                // LDB
                case 0xc6: this.Tick(2); this.ImmediateByte(); this.LDB(); break;   // LD (LDB immediate)
                case 0xd6: this.Tick(4); this.DirectByte(); this.LDB(); break;      // LD (LDB direct)
                case 0xe6: this.Tick(4); this.IndexedByte(); this.LDB(); break;     // LD (LDB indexed)
                case 0xf6: this.Tick(5); this.ExtendedByte(); this.LDB(); break;    // LD (LDB extended)

                // LDD
                case 0xcc: this.Tick(3); this.ImmediateWord(); this.LDD(); break;   // LD (LDD immediate)
                case 0xdc: this.Tick(5); this.DirectWord(); this.LDD(); break;      // LD (LDD direct)
                case 0xec: this.Tick(5); this.IndexedWord(); this.LDD(); break;     // LD (LDD indexed)
                case 0xfc: this.Tick(6); this.ExtendedWord(); this.LDD(); break;    // LD (LDD extended)

                // LDU
                case 0xce: this.Tick(3); this.ImmediateWord(); this.LDU(); break;   // LD (LDU immediate)
                case 0xde: this.Tick(5); this.DirectWord(); this.LDU(); break;      // LD (LDU direct)
                case 0xee: this.Tick(5); this.IndexedWord(); this.LDU(); break;     // LD (LDU indexed)
                case 0xfe: this.Tick(6); this.ExtendedWord(); this.LDU(); break;    // LD (LDU extended)

                // LDX
                case 0x8e: this.Tick(3); this.ImmediateWord(); this.LDX(); break;   // LD (LDX immediate)
                case 0x9e: this.Tick(5); this.DirectWord(); this.LDX(); break;      // LD (LDX direct)
                case 0xae: this.Tick(5); this.IndexedWord(); this.LDX(); break;     // LD (LDX indexed)
                case 0xbe: this.Tick(6); this.ExtendedWord(); this.LDX(); break;    // LD (LDX extended)

                // LEA
                case 0x30: this.Tick(4); this.LEAX(); break;                        // LEA (LEAX indexed)
                case 0x31: this.Tick(4); this.LEAY(); break;                        // LEA (LEAY indexed)
                case 0x32: this.Tick(4); this.LEAS(); break;                        // LEA (LEAS indexed)
                case 0x33: this.Tick(4); this.LEAU(); break;                        // LEA (LEAU indexed)

                // LSR
                case 0x04: this.Tick(6); this.DirectByte(); this.LSR(); break;      // LSR (direct)
                case 0x44: this.Tick(2); this.LSRA(); break;                        // LSR (LSRA inherent)
                case 0x54: this.Tick(2); this.LSRB(); break;                        // LSR (LSRB inherent)
                case 0x64: this.Tick(6); this.IndexedByte(); this.LSR(); break;     // LSR (indexed)
                case 0x74: this.Tick(7); this.ExtendedByte(); this.LSR(); break;    // LSR (extended)

                // MUL
                case 0x3d: this.Tick(11); this.MUL(); break;                        // MUL (inherent)

                // NEG
                case 0x00: this.Tick(6); this.DirectByte(); this.NEG(); break;      // NEG (direct)
                case 0x40: this.Tick(2); this.NEGA(); break;                        // NEG (NEGA, inherent)
                case 0x50: this.Tick(2); this.NEGB(); break;                        // NEG (NEGB, inherent)
                case 0x60: this.Tick(6); this.IndexedByte(); this.NEG(); break;     // NEG (indexed)
                case 0x70: this.Tick(7); this.ExtendedByte(); this.NEG(); break;    // NEG (extended)

                // NOP
                case 0x12: this.Tick(2); break;                                     // NOP (inherent)

                // OR

                // ORA
                case 0x8a: this.Tick(2); this.ImmediateByte(); this.ORA(); break;   // OR (ORA immediate)
                case 0x9a: this.Tick(4); this.DirectByte(); this.ORA(); break;      // OR (ORA direct)
                case 0xaa: this.Tick(4); this.IndexedByte(); this.ORA(); break;     // OR (ORA indexed)
                case 0xba: this.Tick(5); this.ExtendedByte(); this.ORA(); break;    // OR (ORA extended)

                // ORB
                case 0xca: this.Tick(2); this.ImmediateByte(); this.ORB(); break;   // OR (ORB immediate)
                case 0xda: this.Tick(4); this.DirectByte(); this.ORB(); break;      // OR (ORB direct)
                case 0xea: this.Tick(4); this.IndexedByte(); this.ORB(); break;     // OR (ORB indexed)
                case 0xfa: this.Tick(5); this.ExtendedByte(); this.ORB(); break;    // OR (ORB extended)

                // ORCC
                case 0x1a: this.Tick(3); this.ImmediateByte(); this.ORCC(); break;  // OR (ORCC immediate)

                // PSH
                case 0x34: this.Tick(5); this.ImmediateByte(); this.PSHS(); break;  // PSH (PSHS immediate)
                case 0x36: this.Tick(5); this.ImmediateByte(); this.PSHU(); break;  // PSH (PSHU immediate)

                // PUL
                case 0x35: this.Tick(5); this.ImmediateByte(); this.PULS(); break;  // PUL (PULS immediate)
                case 0x37: this.Tick(5); this.ImmediateByte(); this.PULU(); break;  // PUL (PULU immediate)

                // ROL
                case 0x09: this.Tick(6); this.DirectByte(); this.ROL(); break;      // ROL (direct)
                case 0x49: this.Tick(2); this.ROLA(); break;                        // ROL (ROLA inherent)
                case 0x59: this.Tick(2); this.ROLB(); break;                        // ROL (ROLB inherent)
                case 0x69: this.Tick(6); this.IndexedByte(); this.ROL(); break;     // ROL (indexed)
                case 0x79: this.Tick(7); this.ExtendedByte(); this.ROL(); break;    // ROL (extended)

                // ROR
                case 0x06: this.Tick(6); this.DirectByte(); this.ROR(); break;      // ROR (direct)
                case 0x46: this.Tick(2); this.RORA(); break;                        // ROR (RORA inherent)
                case 0x56: this.Tick(2); this.RORB(); break;                        // ROR (RORB inherent)
                case 0x66: this.Tick(6); this.IndexedByte(); this.ROR(); break;     // ROR (indexed)
                case 0x76: this.Tick(7); this.ExtendedByte();this.ROR(); break;     // ROR (extended)

                // RTI
                case 0x3B: this.Tick(6); this.RTI(); break;                         // RTI (inherent)

                // RTS
                case 0x39: this.Tick(5); this.RTS(); break;                         // RTS (inherent)

                // SBC

                // SBCA
                case 0x82: this.Tick(4); this.ImmediateByte(); this.SBCA(); break;  // SBC (SBCA immediate)
                case 0x92: this.Tick(4); this.DirectByte(); this.SBCA(); break;     // SBC (SBCA direct)
                case 0xa2: this.Tick(4); this.IndexedByte(); this.SBCA(); break;    // SBC (SBCA indexed)
                case 0xb2: this.Tick(5); this.ExtendedByte(); this.SBCA(); break;   // SBC (SBCB extended)

                // SBCB
                case 0xc2: this.Tick(4); this.ImmediateByte(); this.SBCB(); break;  // SBC (SBCB immediate)
                case 0xd2: this.Tick(4); this.DirectByte(); this.SBCB(); break;     // SBC (SBCB direct)
                case 0xe2: this.Tick(4); this.IndexedByte(); this.SBCB(); break;    // SBC (SBCB indexed)
                case 0xf2: this.Tick(5); this.ExtendedByte(); this.SBCB(); break;   // SBC (SBCB extended)

                // SEX
                case 0x1d: this.Tick(2); this.SEX(); break;                         // SEX (inherent)

                // ST

                // STA
                case 0x97: this.Tick(4); this.DirectAddress(); this.STA(); break;   // ST (STA direct)
                case 0xa7: this.Tick(4); this.IndexedAddress(); this.STA(); break;  // ST (STA indexed)
                case 0xb7: this.Tick(5); this.ExtendedAddress(); this.STA(); break; // ST (STA extended)

                // STB
                case 0xd7: this.Tick(4); this.DirectAddress(); this.STB(); break;   // ST (STB direct)
                case 0xe7: this.Tick(4); this.IndexedAddress(); this.STB(); break;  // ST (STB indexed)
                case 0xf7: this.Tick(5); this.ExtendedAddress(); this.STB(); break; // ST (STB extended)

                // STD
                case 0xdd: this.Tick(5); this.DirectAddress(); this.STD(); break;   // ST (STD direct)
                case 0xed: this.Tick(5); this.IndexedAddress(); this.STD(); break;  // ST (STD indexed)
                case 0xfd: this.Tick(6); this.ExtendedAddress(); this.STD(); break; // ST (STD extended)

                // STU
                case 0xdf: this.Tick(5); this.DirectAddress(); this.STU(); break;   // ST (STU direct)
                case 0xef: this.Tick(5); this.IndexedAddress(); this.STU(); break;  // ST (STU indexed)
                case 0xff: this.Tick(6); this.ExtendedAddress(); this.STU(); break; // ST (STU extended)

                // STX
                case 0x9f: this.Tick(5); this.DirectAddress(); this.STX(); break;   // ST (STX direct)
                case 0xaf: this.Tick(5); this.IndexedAddress(); this.STX(); break;  // ST (STX indexed)
                case 0xbf: this.Tick(6); this.ExtendedAddress(); this.STX(); break; // ST (STX extended)

                // SUB

                // SUBA
                case 0x80: this.Tick(2); this.ImmediateByte(); this.SUBA(); break;  // SUB (SUBA immediate)
                case 0x90: this.Tick(4); this.DirectByte(); this.SUBA(); break;     // SUB (SUBA direct)
                case 0xa0: this.Tick(4); this.IndexedByte(); this.SUBA(); break;    // SUB (SUBA indexed)
                case 0xb0: this.Tick(5); this.ExtendedByte(); this.SUBA(); break;   // SUB (SUBA extended)

                // SUBB
                case 0xc0: this.Tick(2); this.ImmediateByte(); this.SUBB(); break;  // SUB (SUBB immediate)
                case 0xd0: this.Tick(4); this.DirectByte(); this.SUBB(); break;     // SUB (SUBB direct)
                case 0xe0: this.Tick(4); this.IndexedByte(); this.SUBB(); break;    // SUB (SUBB indexed)
                case 0xf0: this.Tick(5); this.ExtendedByte(); this.SUBB(); break;   // SUB (SUBB extended)

                // SUBD
                case 0x83: this.Tick(4); this.ImmediateWord(); this.SUBD(); break;  // SUB (SUBD immediate)
                case 0x93: this.Tick(6); this.DirectWord(); this.SUBD(); break;     // SUB (SUBD direct)
                case 0xa3: this.Tick(6); this.IndexedWord(); this.SUBD(); break;    // SUB (SUBD indexed)
                case 0xb3: this.Tick(7); this.ExtendedWord(); this.SUBD(); break;   // SUB (SUBD extended)

                // SWI
                case 0x3f: this.Tick(10); this.SWI(); break;                        // SWI (inherent)

                // SYNC
                case 0x13: this.Tick(4); this.SYNC(); break;                        // SYNC (inherent)

                // TFR
                case 0x1f: this.Tick(6); this.ImmediateByte(); this.TFR(); break;   // TFR (immediate)

                // TST
                case 0x0d: this.Tick(6); this.DirectByte(); this.TST(); break;      // TST (direct)
                case 0x4d: this.Tick(2); this.TSTA(); break;                        // TST (TSTA inherent)
                case 0x5d: this.Tick(2); this.TSTB(); break;                        // TST (TSTB inherent)
                case 0x6d: this.Tick(6); this.IndexedByte(); this.TST(); break;     // TST (indexed)
                case 0x7d: this.Tick(7); this.ExtendedByte(); this.TST(); break;    // TST (extended)

                // Branching
                case 0x16: this.Tick(5); this.LBRA(); break;                        // BRA (LBRA relative)
                case 0x17: this.Tick(9); this.LBSR(); break;                        // BSR (LBSR relative)
                case 0x20: this.Tick(3); this.BRA(); break;                         // BRA (relative)
                case 0x21: this.Tick(3); this.BRN(); break;                         // BRN (relative)
                case 0x22: this.Tick(3); this.BHI(); break;                         // BHI (relative)
                case 0x23: this.Tick(3); this.BLS(); break;                         // BLS (relative)
                case 0x24: this.Tick(3); this.BCC(); break;                         // BCC (relative)
                case 0x25: this.Tick(3); this.BCS(); break;                         // BCS (relative)
                case 0x26: this.Tick(3); this.BNE(); break;                         // BNE (relative)
                case 0x27: this.Tick(3); this.BEQ(); break;                         // BEQ (relative)
                case 0x28: this.Tick(3); this.BVC(); break;                         // BVC (relative)
                case 0x29: this.Tick(3); this.BVS(); break;                         // BVS (relative)
                case 0x2a: this.Tick(3); this.BPL(); break;                         // BPL (relative)
                case 0x2b: this.Tick(3); this.BMI(); break;                         // BMI (relative)
                case 0x2c: this.Tick(3); this.BGE(); break;                         // BGE (relative)
                case 0x2d: this.Tick(3); this.BLT(); break;                         // BLT (relative)
                case 0x2e: this.Tick(3); this.BGT(); break;                         // BGT (relative)
                case 0x2f: this.Tick(3); this.BLE(); break;                         // BLE (relative)

                case 0x8d: this.Tick(7); this.BSR(); break;                         // BSR (relative)

                default:
                    throw new InvalidOperationException("Unknown op-code");
            }
        }

        private void Execute10()
        {
            switch (this.OpCode)
            {
                // CMP

                // CMPD
                case 0x83: this.Tick(5); this.ImmediateWord(); this.CMPD(); break;  // CMP (CMPD, immediate)
                case 0x93: this.Tick(7); this.DirectWord(); this.CMPD(); break;     // CMP (CMPD, direct)
                case 0xa3: this.Tick(7); this.IndexedWord(); this.CMPD(); break;    // CMP (CMPD, indexed)
                case 0xb3: this.Tick(8); this.ExtendedWord(); this.CMPD(); break;   // CMP (CMPD, extended)

                // CMPY
                case 0x8c: this.Tick(5); this.ImmediateWord(); this.CMPY(); break;  // CMP (CMPY, immediate)
                case 0x9c: this.Tick(7); this.DirectWord(); this.CMPY(); break;     // CMP (CMPY, direct)
                case 0xac: this.Tick(7); this.IndexedWord(); this.CMPY(); break;    // CMP (CMPY, indexed)
                case 0xbc: this.Tick(8); this.ExtendedWord(); this.CMPY(); break;   // CMP (CMPY, extended)

                // LD

                // LDS
                case 0xce: this.Tick(4); this.ImmediateWord(); this.LDS(); break;   // LD (LDS immediate)
                case 0xde: this.Tick(6); this.DirectWord(); this.LDS(); break;      // LD (LDS direct)
                case 0xee: this.Tick(6); this.IndexedWord(); this.LDS(); break;     // LD (LDS indexed)
                case 0xfe: this.Tick(7); this.ExtendedWord(); this.LDS(); break;    // LD (LDS extended)

                // LDY
                case 0x8e: this.Tick(4); this.ImmediateWord(); this.LDY(); break;   // LD (LDY immediate)
                case 0x9e: this.Tick(6); this.DirectWord(); this.LDY(); break;      // LD (LDY direct)
                case 0xae: this.Tick(6); this.IndexedWord(); this.LDY(); break;     // LD (LDY indexed)
                case 0xbe: this.Tick(7); this.ExtendedWord(); this.LDY(); break;    // LD (LDY extended)

                // Branching
                case 0x21: this.Tick(5); this.LBRN(); break;                        // BRN (LBRN relative)
                case 0x22: this.Tick(5); this.LBHI(); break;                        // BHI (LBHI relative)
                case 0x23: this.Tick(5); this.LBLS(); break;                        // BLS (LBLS relative)
                case 0x24: this.Tick(5); this.LBCC(); break;                        // BCC (LBCC relative)
                case 0x25: this.Tick(5); this.LBCS(); break;                        // BCS (LBCS relative)
                case 0x26: this.Tick(5); this.LBNE(); break;                        // BNE (LBNE relative)
                case 0x27: this.Tick(5); this.LBEQ(); break;                        // BEQ (LBEQ relative)
                case 0x28: this.Tick(5); this.LBVC(); break;                        // BVC (LBVC relative)
                case 0x29: this.Tick(5); this.LBVS(); break;                        // BVS (LBVS relative)
                case 0x2a: this.Tick(5); this.LBPL(); break;                        // BPL (LBPL relative)
                case 0x2b: this.Tick(5); this.LBMI(); break;                        // BMI (LBMI relative)
                case 0x2c: this.Tick(5); this.LBGE(); break;                        // BGE (LBGE relative)
                case 0x2d: this.Tick(5); this.LBLT(); break;                        // BLT (LBLT relative)
                case 0x2e: this.Tick(5); this.LBGT(); break;                        // BGT (LBGT relative)
                case 0x2f: this.Tick(5); this.LBLE(); break;                        // BLE (LBLE relative)

                // STS
                case 0xdf: this.Tick(6); this.DirectAddress(); this.STS(); break;   // ST (STS direct)
                case 0xef: this.Tick(6); this.IndexedAddress(); this.STS(); break;  // ST (STS indexed)
                case 0xff: this.Tick(7); this.ExtendedAddress(); this.STS(); break; // ST (STS extended)

                // STY
                case 0x9f: this.Tick(6); this.DirectAddress(); this.STY(); break;   // ST (STY direct)
                case 0xaf: this.Tick(6); this.IndexedAddress(); this.STY(); break;  // ST (STY indexed)
                case 0xbf: this.Tick(7); this.ExtendedAddress(); this.STY(); break; // ST (STY extended)

                // SWI
                case 0x3f: this.Tick(11); this.SWI2(); break;                       // SWI (SWI2 inherent)

                default:
                    throw new InvalidOperationException("Unknown 10 prefixed op-code");
            }
        }

        private void Execute11()
        {
            switch (this.OpCode)
            {
                // CMP

                // CMPU
                case 0x83: this.Tick(5); this.ImmediateWord(); this.CMPU(); break;  // CMP (CMPU, immediate)
                case 0x93: this.Tick(7); this.DirectWord(); this.CMPU(); break;     // CMP (CMPU, direct)
                case 0xa3: this.Tick(7); this.IndexedWord(); this.CMPU(); break;    // CMP (CMPU, indexed)
                case 0xb3: this.Tick(8); this.ExtendedWord(); this.CMPU(); break;   // CMP (CMPU, extended)

                // CMPS
                case 0x8c: this.Tick(5); this.ImmediateWord(); this.CMPS(); break;  // CMP (CMPS, immediate)
                case 0x9c: this.Tick(7); this.DirectWord(); this.CMPS(); break;     // CMP (CMPS, direct)
                case 0xac: this.Tick(7); this.IndexedWord(); this.CMPS(); break;    // CMP (CMPS, indexed)
                case 0xbc: this.Tick(8); this.ExtendedWord(); this.CMPS(); break;   // CMP (CMPS, extended)

                // SWI
                case 0x3f: this.Tick(11); this.SWI3(); break;                       // SWI (SWI3 inherent)

                default:
                    throw new InvalidOperationException("Unknown 11 prefixed op-code");
            }
        }

        #endregion

        #region Miscellaneous instruction implementations

        private void ADCA() => this.A = this.AddWithCarry(this.A);
        private void ADCB() => this.B = this.AddWithCarry(this.B);

        private byte AddWithCarry(byte operand) => this.Add(operand, this.Bus.Data, (byte)this.Carry);

        private void ADDA() => this.A = this.Add(this.A);
        private void ADDB() => this.B = this.Add(this.B);

        private byte Add(byte operand) => this.Add(operand, this.Bus.Data);

        private byte Add(byte operand, byte data, byte carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand + data + carry);
            this.CC = this.AdjustAddition(operand, data, this.Intermediate);
            return this.Intermediate.Low;
        }

        private void ADDD() => this.Add(this.D, this.Intermediate, this.D);

        private void Add(Register16 operand, Register16 data, Register16 result)
        {
            var addition = operand.Word + data.Word;
            this.CC = this.AdjustAddition(operand, data, (uint)addition);
            result.Word = (ushort)addition;
        }

        private void ANDCC() => this.CC &= this.Bus.Data;

        private void ANDA() => this.A = this.And(this.A, this.Bus.Data);
        private void ANDB() => this.B = this.And(this.B, this.Bus.Data);

        private byte And(byte operand, byte data) => this.Through((byte)(operand & data));

        private void ASLA() => this.A = this.ArithmeticShiftLeft(this.A);
        private void ASLB() => this.B = this.ArithmeticShiftLeft(this.B);

        private void ASL() => this.MemoryWrite(this.ArithmeticShiftLeft(this.Bus.Data));

        private byte ArithmeticShiftLeft(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit7);
            this.CC = this.AdjustNZ(operand <<= 1);
            var overflow = this.Carry ^ this.Negative >> 3;
            this.CC = SetBit(this.CC, StatusBits.VF, overflow);
            return operand;
        }

        private void ASRA() => this.A = this.ArithmeticShiftRight(this.A);
        private void ASRB() => this.B = this.ArithmeticShiftRight(this.B);

        private void ASR() => this.MemoryWrite(this.ArithmeticShiftRight(this.Bus.Data));

        private byte ArithmeticShiftRight(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)(operand >> 1 | (int)Bits.Bit7);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void BITA() => this.Bit(this.A, this.Bus.Data);
        private void BITB() => this.Bit(this.B, this.Bus.Data);

        private void Bit(byte operand, byte data) => this.And(operand, data);

        private void CLRA() => this.A = this.Clear();
        private void CLRB() => this.B = this.Clear();

        private void CLR() => this.MemoryWrite(this.Intermediate, this.Clear());

        private byte Clear()
        {
            this.CC = ClearBit(this.CC, StatusBits.CF);
            return this.Through((byte)0U);
        }

        private void CMPA() => this.Compare(this.A, this.Bus.Data);
        private void CMPB() => this.Compare(this.B, this.Bus.Data);

        private void Compare(byte operand, byte data) => this.Subtract(operand, data);

        private void CMPU() => this.Compare(this.U);
        private void CMPS() => this.Compare(this.S);
        private void CMPD() => this.Compare(this.D);
        private void CMPX() => this.Compare(this.X);
        private void CMPY() => this.Compare(this.Y);

        private void Compare(Register16 operand) => this.Subtract(operand, this.Intermediate, this.Intermediate);

        private void COMA() => this.A = this.Complement(this.A);
        private void COMB() => this.B = this.Complement(this.B);

        private void COM() => this.MemoryWrite(this.Complement(this.Bus.Data));

        private byte Complement(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF);
            return this.Through((byte)~operand);
        }

        private void CWAI()
        {
            this.CC &= this.Bus.Data;
            this.SaveEntireRegisterState();
            this.Halt();
        }

        private void DAA()
        {
            var adjusted = this.A;

            this.CC = SetBit(this.CC, StatusBits.CF, adjusted > 0x99);

            var lowAdjust = this.HalfCarry != 0 || LowNibble(adjusted) > 9;
            var highAdjust = this.Carry != 0 || adjusted > 0x99;

            if (lowAdjust)
            {
                adjusted += 6;
            }

            if (highAdjust)
            {
                adjusted += 0x60;
            }

            this.A = this.Through(adjusted);
        }

        private void EORA() => this.A = this.ExclusiveOr(this.A, this.Bus.Data);
        private void EORB() => this.B = this.ExclusiveOr(this.B, this.Bus.Data);

        private byte ExclusiveOr(byte operand, byte data) => this.Through((byte)(operand ^ data));

        private void DECA() => this.A = this.Decrement(this.A);
        private void DECB() => this.B = this.Decrement(this.B);

        private void DEC() => this.MemoryWrite(this.Decrement(this.Bus.Data));

        private byte Decrement(byte operand)
        {
            this.Intermediate.Word = (ushort)(operand - 1);
            var result = this.Intermediate.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustOverflow(operand, 1, this.Intermediate);
            return result;
        }

        private void INCA() => this.A = this.Increment(this.A);
        private void INCB() => this.B = this.Increment(this.B);

        private void INC() => this.MemoryWrite(this.Increment(this.Bus.Data));

        private byte Increment(byte operand)
        {
            this.Intermediate.Word = (ushort)(operand + 1);
            var result = this.Intermediate.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustOverflow(operand, 1, this.Intermediate);
            this.CC = this.AdjustHalfCarry(operand, 1, result);
            return result;
        }

        private void JMP() => this.Jump(this.Intermediate);

        private void JSR() => this.Call(this.Intermediate);

        private void LSRA() => this.A = this.LogicalShiftRight(this.A);
        private void LSRB() => this.B = this.LogicalShiftRight(this.B);

        private void LSR() => this.MemoryWrite(this.LogicalShiftRight(this.Bus.Data));

        private byte LogicalShiftRight(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            this.CC = this.AdjustNZ(operand >>= 1);
            return operand;
        }

        private void MUL()
        {
            this.D.Word = (ushort)(this.A * this.B);
            this.CC = this.AdjustZero(this.D);
            this.CC = SetBit(this.CC, StatusBits.CF, this.D.Low & (byte)Bits.Bit7);
        }

        private void NEGA() => this.A = this.Negate(this.A);
        private void NEGB() => this.B = this.Negate(this.B);

        private void NEG() => this.MemoryWrite(this.Negate(this.Bus.Data));

        private byte Negate(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.VF, operand == (byte)Bits.Bit7);
            this.Intermediate.Word = (ushort)(0 - operand);
            operand = this.Intermediate.Low;
            this.CC = this.AdjustNZ(operand);
            this.CC = this.AdjustCarry(this.Intermediate);
            return operand;
        }

        private void ORCC() => this.CC |= this.Bus.Data;

        private void ORA() => this.A = this.Or(this.A);
        private void ORB() => this.B = this.Or(this.B);

        private byte Or(byte operand) => this.Through((byte)(operand | this.Bus.Data));

        private void ROLA() => this.A = this.RotateLeft(this.A);
        private void ROLB() => this.B = this.RotateLeft(this.B);

        private void ROL() => this.MemoryWrite(this.RotateLeft(this.Bus.Data));

        private byte RotateLeft(byte operand)
        {
            var carryIn = this.Carry;
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit7);
            this.CC = SetBit(this.CC, StatusBits.VF, (operand & (byte)Bits.Bit7) >> 7 ^ (operand & (byte)Bits.Bit6) >> 6);
            var result = (byte)(operand << 1 | carryIn);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void RORA() => this.A = this.RotateRight(this.A);
        private void RORB() => this.B = this.RotateRight(this.B);

        private void ROR() => this.MemoryWrite(this.RotateRight(this.Bus.Data));

        private byte RotateRight(byte operand)
        {
            var carryIn = this.Carry;
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)(operand >> 1 | carryIn << 7);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void RTI() => this.RestoreRegisterState();

        private void RTS() => this.Return();

        private void SBCA() => this.A = this.SubtractWithCarry(this.A);
        private void SBCB() => this.B = this.SubtractWithCarry(this.B);

        private byte SubtractWithCarry(byte operand) => this.Subtract(operand, this.Bus.Data, (byte)this.Carry);

        private void SUBA() => this.A = this.Subtract(this.A);
        private void SUBB() => this.B = this.Subtract(this.B);

        private byte Subtract(byte operand) => this.Subtract(operand, this.Bus.Data);

        private byte Subtract(byte operand, byte data, byte carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand - data - carry);
            this.CC = this.AdjustSubtraction(operand, data, this.Intermediate);
            return this.Intermediate.Low;
        }

        private void SUBD() => this.Subtract(this.D, this.Intermediate, this.D);

        private void Subtract(Register16 operand, Register16 data, Register16 result)
        {
            var subtraction = operand.Word - data.Word;
            this.CC = this.AdjustSubtraction(operand, data, (uint)subtraction);
            result.Word = (ushort)subtraction;
        }

        private void SEX() => this.A = this.SEX(this.B);

        private byte SEX(byte from)
        {
            this.CC = this.AdjustNZ(from);
            return (from & (byte)Bits.Bit7) != 0 ? (byte)Mask.Eight : (byte)0;
        }

        private void SWI()
        {
            this.SaveEntireRegisterState();
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetBit(this.CC, StatusBits.FF);  // Disable FIRQ
            this.GetWordPaged(0xff, SWI_vector);
            this.Jump(this.Intermediate);
        }

        private void SWI2()
        {
            this.SaveEntireRegisterState();
            this.GetWordPaged(0xff, SWI2_vector);
            this.Jump(this.Intermediate);
        }

        private void SWI3()
        {
            this.SaveEntireRegisterState();
            this.GetWordPaged(0xff, SWI3_vector);
            this.Jump(this.Intermediate);
        }

        private void TSTA() => this.Test(this.A);
        private void TSTB() => this.Test(this.B);

        private void TST() => this.Test(this.Bus.Data);

        private void Test(byte data) => this.Compare(data, 0);

        private void LEAX()
        {
            this.IndexedAddress();
            this.X.Assign(this.Intermediate);
            this.CC = this.AdjustZero(this.X);
        }

        private void LEAY()
        {
            this.IndexedAddress();
            this.Y.Assign(this.Intermediate);
            this.CC = this.AdjustZero(this.Y);
        }

        private void LEAS()
        {
            this.IndexedAddress();
            this.S.Assign(this.Intermediate);
        }

        private void LEAU()
        {
            this.IndexedAddress();
            this.U.Assign(this.Intermediate);
        }

        private void ABX() => this.X.Word += this.B;

        private void Prefix10()
        {
            this.prefix10 = true;
            this.Execute(this.FetchByte());
        }

        private void Prefix11()
        {
            this.prefix11 = true;
            this.Execute(this.FetchByte());
        }

        #endregion
        }
    }
