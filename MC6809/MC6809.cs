// <copyright file="MC6809.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    using EightBit;
    using System.Diagnostics;

    // Uses some information from:
    // http://www.cpu-world.com/Arch/6809.html
    // https://web.archive.org/web/20200817115822/http://atjs.mbnet.fi/mc6809/
    // https://colorcomputerarchive.com/repo/Documents/Books/Motorola%206809%20and%20Hitachi%206309%20Programming%20Reference%20(Darren%20Atkinson).pdf

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

        public void Halt() => this.LowerHALT();

        public void Proceed() => this.RaiseHALT();

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

        public bool E => this.EntireRegisterSet != 0;

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
        }

        #endregion

        #region Bus control

        protected override void BusWrite()
        {
            this.Tick();
            this.LowerRW();
            base.BusWrite();
        }

        protected override byte BusRead()
        {
            this.Tick();
            this.RaiseRW();
            return base.BusRead();
        }

        #endregion

        #region Push/Pop

        protected override byte Pop() => this.PopS();

        protected override void Push(byte value) => this.PushS(value);

        private void Push(Register16 stack, byte value)
        {
            this.MemoryWrite(stack.Decrement(), value);
        }

        private void PushS(byte value) => this.Push(this.S, value);

        private void Push(Register16 stack, Register16 value)
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
            this.Intermediate.High = this.Pop(stack);
            this.Intermediate.Low = this.Pop(stack);
            return this.Intermediate;
        }

        #endregion

        #region Addressing modes

        private void RelativeByteAddress()
        {
            var offset = (sbyte)this.FetchByte();
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
            this.Intermediate.Assign(this.FetchByte(), this.DP);
            this.SwallowRead();
        }

        private void ExtendedAddress()
        {
            this.FetchWord();
            this.SwallowRead();
        }

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
            var type = this.FetchByte();
            var r = this.RR((type & (byte)(Bits.Bit6 | Bits.Bit5)) >> 5);

            if ((type & (byte)Bits.Bit7) != 0)
            {
                switch (type & (byte)Mask.Four)
                {
                    case 0b0000: // ,R+
                        this.Intermediate.Assign(r);
                        r.Word++;
                        this.SwallowCurrent();
                        this.SwallowRead(2);
                        break;
                    case 0b0001: // ,R++
                        this.Intermediate.Assign(r);
                        r.Word += 2;
                        this.SwallowCurrent();
                        this.SwallowRead(3);
                        break;
                    case 0b0010: // ,-R
                        --r.Word;
                        this.Intermediate.Assign(r);
                        this.SwallowCurrent();
                        this.SwallowRead(2);
                        break;
                    case 0b0011: // ,--R
                        r.Word -= 2;
                        this.Intermediate.Assign(r);
                        this.SwallowCurrent();
                        this.SwallowRead(3);
                        break;
                    case 0b0100: // ,R
                        this.Intermediate.Assign(r);
                        this.SwallowCurrent();
                        break;
                    case 0b0101: // B,R
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.B);
                        this.SwallowCurrent();
                        this.SwallowRead();
                        break;
                    case 0b0110: // A,R
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.A);
                        this.SwallowCurrent();
                        this.SwallowRead();
                        break;
                    case 0b1000: // n,R (eight-bit)
                        this.FetchByte();
                        this.Intermediate.Word = (ushort)(r.Word + (sbyte)this.Bus.Data);
                        this.SwallowRead();
                        break;
                    case 0b1001: // n,R (sixteen-bit)
                        this.FetchWord();
                        this.Intermediate.Word += r.Word;
                        this.SwallowCurrent();
                        this.SwallowRead(2);
                        break;
                    case 0b1011: // D,R
                        this.Intermediate.Word = (ushort)(r.Word + this.D.Word);
                        this.SwallowCurrent(3);
                        this.SwallowRead(2);
                        break;
                    case 0b1100: // n,PCR (eight-bit)
                        this.RelativeByteAddress();
                        this.SwallowRead();
                        break;
                    case 0b1101: // n,PCR (sixteen-bit)
                        this.RelativeWordAddress();
                        this.SwallowCurrent();
                        this.SwallowRead(3);
                        break;
                    case 0b1111: // [n]
                        this.FetchWord();
                        this.SwallowCurrent();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid index type");
                }

                var indirect = type & (byte)Bits.Bit4;
                if (indirect != 0)
                {
                    var address = this.Intermediate;
                    this.GetWord(address);
                    this.SwallowRead();
                }
            }
            else
            {
                // EA = ,R + 5-bit offset
                this.Intermediate.Word = (ushort)(r.Word + SignExtend(5, (byte)(type & (byte)Mask.Five)));
                this.SwallowCurrent();
                this.SwallowRead();
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

        private void LDA() => this.Assign(ref this.A);
        private void LDB() => this.Assign(ref this.B);

        private void Assign(ref byte destination) => destination = this.Through(this.Bus.Data);

        private byte Through(byte data)
        {
            this.CC = ClearBit(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
            return data;
        }

        private void LDD() => this.Assign(this.D);
        private void LDS() => this.Assign(this.S);
        private void LDU() => this.Assign(this.U);
        private void LDX() => this.Assign(this.X);
        private void LDY() => this.Assign(this.Y);

        private void Assign(Register16 destination) => destination.Assign(this.Through(this.Intermediate));

        private Register16 Through(Register16 data)
        {
            this.CC = ClearBit(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
            return data;
        }

        private void STA() => this.Store(this.A);
        private void STB() => this.Store(this.B);

        private void Store(byte data) => this.MemoryWrite(this.Intermediate, this.Through(data));

        private void STD() => this.Store(this.D);
        private void STU() => this.Store(this.U);
        private void STS() => this.Store(this.S);
        private void STX() => this.Store(this.X);
        private void STY() => this.Store(this.Y);

        private void Store(Register16 data) => this.SetWord(this.Intermediate, this.Through(data));

        #endregion

        #region Branching

        private void LBSR()
        {
            this.RelativeWordAddress();
            this.SwallowRead(4);
            this.Call(this.Intermediate);
        }

        private void BSR()
        {
            this.SwallowRead(3);
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
            this.Branch(this.Intermediate, condition);
            this.SwallowRead();
        }

        private void BranchLong(bool condition)
        {
            this.SwallowRead();
            if (this.Branch(this.Intermediate, condition))
            {
                this.SwallowRead();
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

        private void SaveRegisterState() => this.PSH(this.S, this.E ? (byte)Mask.Eight : (byte)0b10000001);

        private void RestoreRegisterState() => this.PUL(this.S, this.E ? (byte)Mask.Eight : (byte)0b10000001);

        private void PSHS() => this.PSH(this.S);
        private void PSHU() => this.PSH(this.U);

        private void PSH(Register16 stack)
        {
            var control = this.Bus.Data;
            this.SwallowRead(2);
            this.SwallowPop(stack);
            this.PSH(stack, control);
        }

        private void PSH(Register16 stack, byte control)
        {
            // Reverse order of PUL

            // Eight-bit registers

            if ((control & (byte)Bits.Bit7) != 0)
            {
                this.Push(stack, this.PC);
            }

            if ((control & (byte)Bits.Bit6) != 0)
            {
                // Pushing to the S stack means we must be pushing U
                this.Push(stack, ReferenceEquals(stack, this.S) ? this.U : this.S);
            }

            if ((control & (byte)Bits.Bit5) != 0)
            {
                this.Push(stack, this.Y);
            }

            if ((control & (byte)Bits.Bit4) != 0)
            {
                this.Push(stack, this.X);
            }

            // Eight-bit registers

            if ((control & (byte)Bits.Bit3) != 0)
            {
                this.Push(stack, this.DP);
            }

            if ((control & (byte)Bits.Bit2) != 0)
            {
                this.Push(stack, this.B);
            }

            if ((control & (byte)Bits.Bit1) != 0)
            {
                this.Push(stack, this.A);
            }

            if ((control & (byte)Bits.Bit0) != 0)
            {
                this.Push(stack, this.CC);
            }
        }

        private void PULU() => this.PUL(this.U);
        private void PULS() => this.PUL(this.S);

        private void PUL(Register16 stack)
        {
            var control = this.Bus.Data;
            this.SwallowRead(2);
            this.PUL(stack, control);
            this.SwallowPop(stack);
        }

        private void PUL(Register16 stack, byte control)
        {
            // Reverse order of PSH

            // Eight-bit registers

            if ((control & (byte)Bits.Bit0) != 0)
            {
                this.CC = this.Pop(stack);
            }

            if ((control & (byte)Bits.Bit1) != 0)
            {
                this.A = this.Pop(stack);
            }

            if ((control & (byte)Bits.Bit2) != 0)
            {
                this.B = this.Pop(stack);
            }

            if ((control & (byte)Bits.Bit3) != 0)
            {
                this.DP = this.Pop(stack);
            }

            // Sixteen-bit registers

            if ((control & (byte)Bits.Bit4) != 0)
            {
                this.X.Assign(this.PopWord(stack));
            }

            if ((control & (byte)Bits.Bit5) != 0)
            {
                this.Y.Assign(this.PopWord(stack));
            }

            if ((control & (byte)Bits.Bit6) != 0)
            {
                // Pulling from the S stack means we must be pulling U
                (ReferenceEquals(stack, this.S) ? this.U : this.S).Assign(this.PopWord(stack));
            }

            if ((control & (byte)Bits.Bit7) != 0)
            {
                this.PC.Assign(this.PopWord(stack));
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

            this.SwallowRead(6);
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
                    this.ReferenceTransfer16(destinationSpecifier).Assign(sourceRegister);
                }
                else
                {
                    this.ReferenceTransfer8(destinationSpecifier) = sourceRegister.Low;
                }
            }
            else
            {
                ref var sourceRegister = ref this.ReferenceTransfer8(sourceSpecifier);
                if (destinationType == 0)
                {
                    this.ReferenceTransfer16(destinationSpecifier).Assign(sourceRegister, (byte)Mask.Eight);
                }
                else
                {
                    this.ReferenceTransfer8(destinationSpecifier) = sourceRegister;
                }
            }

            this.SwallowRead(4);
        }

        #endregion

        #region Cycle wastage

        private void SwallowRead(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                _ = this.MemoryRead(0xff, 0xff);
            }
        }

        private void SwallowCurrent(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                _ = this.MemoryRead(this.PC);
            }
        }

        private void SwallowPop(Register16 stack) => _ = this.MemoryRead(stack);

        private void SwallowEffectiveAddress() => _ = this.MemoryRead(this.Intermediate);

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
                this.Execute(this.FetchByte());
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
                case 0x3a: this.SwallowCurrent(); this.ABX(); Debug.Assert(this.Cycles == 3); break;        // ABX (inherent)

                // ADC
                case 0x89: this.ImmediateByte(); this.ADCA(); Debug.Assert(this.Cycles == 2); break;        // ADC (ADCA immediate)
                case 0x99: this.DirectByte(); this.ADCA(); Debug.Assert(this.Cycles == 4); break;           // ADC (ADCA direct)
                case 0xa9: this.IndexedByte(); this.ADCA(); Debug.Assert(this.Cycles >= 4); break;          // ADC (ADCA indexed)
                case 0xb9: this.ExtendedByte(); this.ADCA(); Debug.Assert(this.Cycles == 5); break;         // ADC (ADCA extended)

                case 0xc9: this.ImmediateByte(); this.ADCB(); Debug.Assert(this.Cycles == 2); break;        // ADC (ADCB immediate)
                case 0xd9: this.DirectByte(); this.ADCB(); Debug.Assert(this.Cycles == 4); break;           // ADC (ADCB direct)
                case 0xe9: this.IndexedByte(); this.ADCB(); Debug.Assert(this.Cycles >= 4); break;          // ADC (ADCB indexed)
                case 0xf9: this.ExtendedByte(); this.ADCB(); Debug.Assert(this.Cycles == 5); break;         // ADC (ADCB extended)

                // ADD
                case 0x8b: this.ImmediateByte(); this.ADDA(); Debug.Assert(this.Cycles == 2); break;        // ADD (ADDA immediate)
                case 0x9b: this.DirectByte(); this.ADDA(); Debug.Assert(this.Cycles == 4); break;           // ADD (ADDA direct)
                case 0xab: this.IndexedByte(); this.ADDA(); Debug.Assert(this.Cycles >= 4); break;          // ADD (ADDA indexed)
                case 0xbb: this.ExtendedByte(); this.ADDA(); Debug.Assert(this.Cycles == 5); break;         // ADD (ADDA extended)

                case 0xcb: this.ImmediateByte(); this.ADDB(); Debug.Assert(this.Cycles == 2); break;        // ADD (ADDB immediate)
                case 0xdb: this.DirectByte(); this.ADDB(); Debug.Assert(this.Cycles == 4); break;           // ADD (ADDB direct)
                case 0xeb: this.IndexedByte(); this.ADDB(); Debug.Assert(this.Cycles >= 4); break;          // ADD (ADDB indexed)
                case 0xfb: this.ExtendedByte(); this.ADDB(); Debug.Assert(this.Cycles == 5); break;         // ADD (ADDB extended)

                case 0xc3: this.ImmediateWord(); this.ADDD(); Debug.Assert(this.Cycles == 4); break;        // ADD (ADDD immediate)
                case 0xd3: this.DirectWord(); this.ADDD(); Debug.Assert(this.Cycles == 6); break;           // ADD (ADDD direct)
                case 0xe3: this.IndexedWord(); this.ADDD(); Debug.Assert(this.Cycles >= 6); break;          // ADD (ADDD indexed)
                case 0xf3: this.ExtendedWord(); this.ADDD(); Debug.Assert(this.Cycles == 7); break;         // ADD (ADDD extended)

                // AND
                case 0x84: this.ImmediateByte(); this.ANDA(); Debug.Assert(this.Cycles == 2); break;        // AND (ANDA immediate)
                case 0x94: this.DirectByte(); this.ANDA(); Debug.Assert(this.Cycles == 4); break;           // AND (ANDA direct)
                case 0xa4: this.IndexedByte(); this.ANDA(); Debug.Assert(this.Cycles >= 4); break;          // AND (ANDA indexed)
                case 0xb4: this.ExtendedByte(); this.ANDA(); Debug.Assert(this.Cycles == 5); break;         // AND (ANDA extended)

                case 0xc4: this.ImmediateByte(); this.ANDB(); Debug.Assert(this.Cycles == 2); break;        // AND (ANDB immediate)
                case 0xd4: this.DirectByte(); this.ANDB(); Debug.Assert(this.Cycles == 4); break;           // AND (ANDB direct)
                case 0xe4: this.IndexedByte(); this.ANDB(); Debug.Assert(this.Cycles >= 4); break;          // AND (ANDB indexed)
                case 0xf4: this.ExtendedByte(); this.ANDB(); Debug.Assert(this.Cycles == 5); break;         // AND (ANDB extended)

                case 0x1c: this.ImmediateByte(); this.ANDCC(); Debug.Assert(this.Cycles == 3); break;       // AND (ANDCC immediate)

                // ASL/LSL
                case 0x08: this.DirectByte(); this.ASL(); Debug.Assert(this.Cycles == 6); break;            // ASL (direct)
                case 0x48: this.SwallowCurrent(); this.ASLA(); Debug.Assert(this.Cycles == 2); break;       // ASL (ASLA inherent)
                case 0x58: this.SwallowCurrent(); this.ASLB(); Debug.Assert(this.Cycles == 2); break;       // ASL (ASLB inherent)
                case 0x68: this.IndexedByte(); this.ASL(); Debug.Assert(this.Cycles >= 6); break;           // ASL (indexed)
                case 0x78: this.ExtendedByte(); this.ASL(); Debug.Assert(this.Cycles == 7); break;          // ASL (extended)

                // ASR
                case 0x07: this.DirectByte(); this.ASR(); Debug.Assert(this.Cycles == 6); break;            // ASR (direct)
                case 0x47: this.SwallowCurrent(); this.ASRA(); Debug.Assert(this.Cycles == 2); break;       // ASR (ASRA inherent)
                case 0x57: this.SwallowCurrent(); this.ASRB(); Debug.Assert(this.Cycles == 2); break;       // ASR (ASRB inherent)
                case 0x67: this.IndexedByte(); this.ASR(); Debug.Assert(this.Cycles >= 6); break;           // ASR (indexed)
                case 0x77: this.ExtendedByte(); this.ASR(); Debug.Assert(this.Cycles == 7); break;          // ASR (extended)

                // BIT
                case 0x85: this.ImmediateByte(); this.BITA(); Debug.Assert(this.Cycles == 2); break;        // BIT (BITA immediate)
                case 0x95: this.DirectByte(); this.BITA(); Debug.Assert(this.Cycles == 4); break;           // BIT (BITA direct)
                case 0xa5: this.IndexedByte(); this.BITA(); Debug.Assert(this.Cycles >= 4); break;          // BIT (BITA indexed)
                case 0xb5: this.ExtendedByte(); this.BITA(); Debug.Assert(this.Cycles == 5); break;         // BIT (BITA extended)

                case 0xc5: this.ImmediateByte(); this.BITB(); Debug.Assert(this.Cycles == 2); break;        // BIT (BITB immediate)
                case 0xd5: this.DirectByte(); this.BITB(); Debug.Assert(this.Cycles == 4); break;           // BIT (BITB direct)
                case 0xe5: this.IndexedByte(); this.BITB(); Debug.Assert(this.Cycles >= 4); break;          // BIT (BITB indexed)
                case 0xf5: this.ExtendedByte(); this.BITB(); Debug.Assert(this.Cycles == 5); break;         // BIT (BITB extended)

                // CLR
                case 0x0f: this.DirectAddress(); this.CLR(); Debug.Assert(this.Cycles == 6); break;         // CLR (direct)
                case 0x4f: this.SwallowCurrent(); this.CLRA(); Debug.Assert(this.Cycles == 2); break;       // CLR (CLRA implied)
                case 0x5f: this.SwallowCurrent(); this.CLRB(); Debug.Assert(this.Cycles == 2); break;       // CLR (CLRB implied)
                case 0x6f: this.IndexedAddress(); this.CLR(); Debug.Assert(this.Cycles >= 6); break;        // CLR (indexed)
                case 0x7f: this.ExtendedAddress(); this.CLR(); Debug.Assert(this.Cycles == 7); break;       // CLR (extended)

                // CMP

                // CMPA
                case 0x81: this.ImmediateByte(); this.CMPA(); Debug.Assert(this.Cycles == 2); break;        // CMP (CMPA, immediate)
                case 0x91: this.DirectByte(); this.CMPA(); Debug.Assert(this.Cycles == 4); break;           // CMP (CMPA, direct)
                case 0xa1: this.IndexedByte(); this.CMPA(); Debug.Assert(this.Cycles >= 4); break;          // CMP (CMPA, indexed)
                case 0xb1: this.ExtendedByte(); this.CMPA(); Debug.Assert(this.Cycles == 5); break;         // CMP (CMPA, extended)

                // CMPB
                case 0xc1: this.ImmediateByte(); this.CMPB(); Debug.Assert(this.Cycles == 2); break;        // CMP (CMPB, immediate)
                case 0xd1: this.DirectByte(); this.CMPB(); Debug.Assert(this.Cycles == 4); break;           // CMP (CMPB, direct)
                case 0xe1: this.IndexedByte(); this.CMPB(); Debug.Assert(this.Cycles >= 4); break;          // CMP (CMPB, indexed)
                case 0xf1: this.ExtendedByte(); this.CMPB(); Debug.Assert(this.Cycles == 5); break;         // CMP (CMPB, extended)

                // CMPX
                case 0x8c: this.ImmediateWord(); this.CMPX(); Debug.Assert(this.Cycles == 4); break;        // CMP (CMPX, immediate)
                case 0x9c: this.DirectWord(); this.CMPX(); Debug.Assert(this.Cycles == 6); break;           // CMP (CMPX, direct)
                case 0xac: this.IndexedWord(); this.CMPX(); Debug.Assert(this.Cycles >= 6); break;          // CMP (CMPX, indexed)
                case 0xbc: this.ExtendedWord(); this.CMPX(); Debug.Assert(this.Cycles == 7); break;         // CMP (CMPX, extended)

                // COM
                case 0x03: this.DirectByte(); this.COM(); Debug.Assert(this.Cycles == 6); break;            // COM (direct)
                case 0x43: this.SwallowCurrent(); this.COMA(); Debug.Assert(this.Cycles == 2); break;       // COM (COMA inherent)
                case 0x53: this.SwallowCurrent(); this.COMB(); Debug.Assert(this.Cycles == 2); break;       // COM (COMB inherent)
                case 0x63: this.IndexedByte(); this.COM(); Debug.Assert(this.Cycles >= 6); break;           // COM (indexed)
                case 0x73: this.ExtendedByte(); this.COM(); Debug.Assert(this.Cycles == 7); break;          // COM (extended)

                // CWAI
                case 0x3c: this.SwallowCurrent(); this.CWAI(); break;                                       // CWAI (inherent) - cycles omitted: halts before full interrupt response

                // DAA
                case 0x19: this.SwallowCurrent(); this.DAA(); Debug.Assert(this.Cycles == 2); break;        // DAA (inherent)

                // DEC
                case 0x0a: this.DirectByte(); this.DEC(); Debug.Assert(this.Cycles == 6); break;            // DEC (direct)
                case 0x4a: this.SwallowCurrent(); this.DECA(); Debug.Assert(this.Cycles == 2); break;       // DEC (DECA inherent)
                case 0x5a: this.SwallowCurrent(); this.DECB(); Debug.Assert(this.Cycles == 2); break;       // DEC (DECB inherent)
                case 0x6a: this.IndexedByte(); this.DEC(); Debug.Assert(this.Cycles >= 6); break;           // DEC (indexed)
                case 0x7a: this.ExtendedByte(); this.DEC(); Debug.Assert(this.Cycles == 7); break;          // DEC (extended)

                // EOR

                // EORA
                case 0x88: this.ImmediateByte(); this.EORA(); Debug.Assert(this.Cycles == 2); break;        // EOR (EORA immediate)
                case 0x98: this.DirectByte(); this.EORA(); Debug.Assert(this.Cycles == 4); break;           // EOR (EORA direct)
                case 0xa8: this.IndexedByte(); this.EORA(); Debug.Assert(this.Cycles >= 4); break;          // EOR (EORA indexed)
                case 0xb8: this.ExtendedByte(); this.EORA(); Debug.Assert(this.Cycles == 5); break;         // EOR (EORA extended)

                // EORB
                case 0xc8: this.ImmediateByte(); this.EORB(); Debug.Assert(this.Cycles == 2); break;        // EOR (EORB immediate)
                case 0xd8: this.DirectByte(); this.EORB(); Debug.Assert(this.Cycles == 4); break;           // EOR (EORB direct)
                case 0xe8: this.IndexedByte(); this.EORB(); Debug.Assert(this.Cycles >= 4); break;          // EOR (EORB indexed)
                case 0xf8: this.ExtendedByte(); this.EORB(); Debug.Assert(this.Cycles == 5); break;         // EOR (EORB extended)

                // EXG
                case 0x1e: this.ImmediateByte(); this.EXG(); Debug.Assert(this.Cycles == 8); break;         // EXG (R1,R2 immediate)

                // INC
                case 0x0c: this.DirectByte(); this.INC(); Debug.Assert(this.Cycles == 6); break;            // INC (direct)
                case 0x4c: this.SwallowCurrent(); this.INCA(); Debug.Assert(this.Cycles == 2); break;       // INC (INCA inherent)
                case 0x5c: this.SwallowCurrent(); this.INCB(); Debug.Assert(this.Cycles == 2); break;       // INC (INCB inherent)
                case 0x6c: this.IndexedByte(); this.INC(); Debug.Assert(this.Cycles >= 6); break;           // INC (indexed)
                case 0x7c: this.ExtendedByte(); this.INC(); Debug.Assert(this.Cycles == 7); break;          // INC (extended)

                // JMP
                case 0x0e: this.DirectAddress(); this.JMP(); Debug.Assert(this.Cycles == 3); break;         // JMP (direct)
                case 0x6e: this.IndexedAddress(); this.JMP(); Debug.Assert(this.Cycles >= 3); break;        // JMP (indexed)
                case 0x7e: this.ExtendedAddress(); this.JMP(); Debug.Assert(this.Cycles == 4); break;       // JMP (extended)

                // JSR
                case 0x9d: this.DirectAddress(); this.JSR(); Debug.Assert(this.Cycles == 7); break;         // JSR (direct)
                case 0xad: this.IndexedAddress(); this.JSR(); Debug.Assert(this.Cycles >= 7); break;        // JSR (indexed)
                case 0xbd: this.ExtendedAddress(); this.JSR(); Debug.Assert(this.Cycles == 8); break;       // JSR (extended)

                // LD

                // LDA
                case 0x86: this.ImmediateByte(); this.LDA(); Debug.Assert(this.Cycles == 2); break;         // LD (LDA immediate)
                case 0x96: this.DirectByte(); this.LDA(); Debug.Assert(this.Cycles == 4); break;            // LD (LDA direct)
                case 0xa6: this.IndexedByte(); this.LDA(); Debug.Assert(this.Cycles >= 4); break;           // LD (LDA indexed)
                case 0xb6: this.ExtendedByte(); this.LDA(); Debug.Assert(this.Cycles == 5); break;          // LD (LDA extended)

                // LDB
                case 0xc6: this.ImmediateByte(); this.LDB(); Debug.Assert(this.Cycles == 2); break;         // LD (LDB immediate)
                case 0xd6: this.DirectByte(); this.LDB(); Debug.Assert(this.Cycles == 4); break;            // LD (LDB direct)
                case 0xe6: this.IndexedByte(); this.LDB(); Debug.Assert(this.Cycles >= 4); break;           // LD (LDB indexed)
                case 0xf6: this.ExtendedByte(); this.LDB(); Debug.Assert(this.Cycles == 5); break;          // LD (LDB extended)

                // LDD
                case 0xcc: this.ImmediateWord(); this.LDD(); Debug.Assert(this.Cycles == 3); break;         // LD (LDD immediate)
                case 0xdc: this.DirectWord(); this.LDD(); Debug.Assert(this.Cycles == 5); break;            // LD (LDD direct)
                case 0xec: this.IndexedWord(); this.LDD(); Debug.Assert(this.Cycles >= 5); break;           // LD (LDD indexed)
                case 0xfc: this.ExtendedWord(); this.LDD(); Debug.Assert(this.Cycles == 6); break;          // LD (LDD extended)

                // LDU
                case 0xce: this.ImmediateWord(); this.LDU(); Debug.Assert(this.Cycles == 3); break;         // LD (LDU immediate)
                case 0xde: this.DirectWord(); this.LDU(); Debug.Assert(this.Cycles == 5); break;            // LD (LDU direct)
                case 0xee: this.IndexedWord(); this.LDU(); Debug.Assert(this.Cycles >= 5); break;           // LD (LDU indexed)
                case 0xfe: this.ExtendedWord(); this.LDU(); Debug.Assert(this.Cycles == 6); break;          // LD (LDU extended)

                // LDX
                case 0x8e: this.ImmediateWord(); this.LDX(); Debug.Assert(this.Cycles == 3); break;         // LD (LDX immediate)
                case 0x9e: this.DirectWord(); this.LDX(); Debug.Assert(this.Cycles == 5); break;            // LD (LDX direct)
                case 0xae: this.IndexedWord(); this.LDX(); Debug.Assert(this.Cycles >= 5); break;           // LD (LDX indexed)
                case 0xbe: this.ExtendedWord(); this.LDX(); Debug.Assert(this.Cycles == 6); break;          // LD (LDX extended)

                // LEA
                case 0x30: this.IndexedAddress(); this.LEAX(); Debug.Assert(this.Cycles >= 4); break;       // LEA (LEAX indexed)
                case 0x31: this.IndexedAddress(); this.LEAY(); Debug.Assert(this.Cycles >= 4); break;       // LEA (LEAY indexed)
                case 0x32: this.IndexedAddress(); this.LEAS(); Debug.Assert(this.Cycles >= 4); break;       // LEA (LEAS indexed)
                case 0x33: this.IndexedAddress(); this.LEAU(); Debug.Assert(this.Cycles >= 4); break;       // LEA (LEAU indexed)

                // LSR
                case 0x04: this.DirectByte(); this.LSR(); Debug.Assert(this.Cycles == 6); break;            // LSR (direct)
                case 0x44: this.SwallowCurrent(); this.LSRA(); Debug.Assert(this.Cycles == 2); break;       // LSR (LSRA inherent)
                case 0x54: this.SwallowCurrent(); this.LSRB(); Debug.Assert(this.Cycles == 2); break;       // LSR (LSRB inherent)
                case 0x64: this.IndexedByte(); this.LSR(); Debug.Assert(this.Cycles >= 6); break;           // LSR (indexed)
                case 0x74: this.ExtendedByte(); this.LSR(); Debug.Assert(this.Cycles == 7); break;          // LSR (extended)

                // MUL
                case 0x3d: this.SwallowCurrent(); this.MUL(); Debug.Assert(this.Cycles == 11); break;       // MUL (inherent)

                // NEG
                case 0x00: this.DirectByte(); this.NEG(); Debug.Assert(this.Cycles == 6); break;            // NEG (direct)
                case 0x40: this.SwallowCurrent(); this.NEGA(); Debug.Assert(this.Cycles == 2); break;       // NEG (NEGA, inherent)
                case 0x50: this.SwallowCurrent(); this.NEGB(); Debug.Assert(this.Cycles == 2); break;       // NEG (NEGB, inherent)
                case 0x60: this.IndexedByte(); this.NEG(); Debug.Assert(this.Cycles >= 6); break;           // NEG (indexed)
                case 0x70: this.ExtendedByte(); this.NEG(); Debug.Assert(this.Cycles == 7); break;          // NEG (extended)

                // NOP
                case 0x12: this.SwallowCurrent(); NOP(); Debug.Assert(this.Cycles == 2); break;             // NOP (inherent)

                // OR

                // ORA
                case 0x8a: this.ImmediateByte(); this.ORA(); Debug.Assert(this.Cycles == 2); break;         // OR (ORA immediate)
                case 0x9a: this.DirectByte(); this.ORA(); Debug.Assert(this.Cycles == 4); break;            // OR (ORA direct)
                case 0xaa: this.IndexedByte(); this.ORA(); Debug.Assert(this.Cycles >= 4); break;           // OR (ORA indexed)
                case 0xba: this.ExtendedByte(); this.ORA(); Debug.Assert(this.Cycles == 5); break;          // OR (ORA extended)

                // ORB
                case 0xca: this.ImmediateByte(); this.ORB(); Debug.Assert(this.Cycles == 2); break;         // OR (ORB immediate)
                case 0xda: this.DirectByte(); this.ORB(); Debug.Assert(this.Cycles == 4); break;            // OR (ORB direct)
                case 0xea: this.IndexedByte(); this.ORB(); Debug.Assert(this.Cycles >= 4); break;           // OR (ORB indexed)
                case 0xfa: this.ExtendedByte(); this.ORB(); Debug.Assert(this.Cycles == 5); break;          // OR (ORB extended)

                // ORCC
                case 0x1a: this.ImmediateByte(); this.ORCC(); Debug.Assert(this.Cycles == 3); break;        // OR (ORCC immediate)

                // PSH
                case 0x34: this.ImmediateByte(); this.PSHS(); Debug.Assert(this.Cycles >= 5); break;        // PSH (PSHS immediate)
                case 0x36: this.ImmediateByte(); this.PSHU(); Debug.Assert(this.Cycles >= 5); break;        // PSH (PSHU immediate)

                // PUL
                case 0x35: this.ImmediateByte(); this.PULS(); Debug.Assert(this.Cycles >= 5); break;        // PUL (PULS immediate)
                case 0x37: this.ImmediateByte(); this.PULU(); Debug.Assert(this.Cycles >= 5); break;        // PUL (PULU immediate)

                // ROL
                case 0x09: this.DirectByte(); this.ROL(); Debug.Assert(this.Cycles == 6); break;            // ROL (direct)
                case 0x49: this.SwallowCurrent(); this.ROLA(); Debug.Assert(this.Cycles == 2); break;       // ROL (ROLA inherent)
                case 0x59: this.SwallowCurrent(); this.ROLB(); Debug.Assert(this.Cycles == 2); break;       // ROL (ROLB inherent)
                case 0x69: this.IndexedByte(); this.ROL(); Debug.Assert(this.Cycles >= 6); break;           // ROL (indexed)
                case 0x79: this.ExtendedByte(); this.ROL(); Debug.Assert(this.Cycles == 7); break;          // ROL (extended)

                // ROR
                case 0x06: this.DirectByte(); this.ROR(); Debug.Assert(this.Cycles == 6); break;            // ROR (direct)
                case 0x46: this.SwallowCurrent(); this.RORA(); Debug.Assert(this.Cycles == 2); break;       // ROR (RORA inherent)
                case 0x56: this.SwallowCurrent(); this.RORB(); Debug.Assert(this.Cycles == 2); break;       // ROR (RORB inherent)
                case 0x66: this.IndexedByte(); this.ROR(); Debug.Assert(this.Cycles >= 6); break;           // ROR (indexed)
                case 0x76: this.ExtendedByte(); this.ROR(); Debug.Assert(this.Cycles == 7); break;          // ROR (extended)

                // RTI
                case 0x3B: this.SwallowCurrent(); this.RTI(); Debug.Assert(this.Cycles == (this.E ? 15 : 6)); break; // RTI (inherent)

                // RTS
                case 0x39: this.SwallowCurrent(); this.RTS(); Debug.Assert(this.Cycles == 5); break;        // RTS (inherent)

                // SBC

                // SBCA
                case 0x82: this.ImmediateByte(); this.SBCA(); Debug.Assert(this.Cycles == 2); break;        // SBC (SBCA immediate)
                case 0x92: this.DirectByte(); this.SBCA(); Debug.Assert(this.Cycles == 4); break;           // SBC (SBCA direct)
                case 0xa2: this.IndexedByte(); this.SBCA(); Debug.Assert(this.Cycles >= 4); break;          // SBC (SBCA indexed)
                case 0xb2: this.ExtendedByte(); this.SBCA(); Debug.Assert(this.Cycles == 5); break;         // SBC (SBCB extended)

                // SBCB
                case 0xc2: this.ImmediateByte(); this.SBCB(); Debug.Assert(this.Cycles == 2); break;        // SBC (SBCB immediate)
                case 0xd2: this.DirectByte(); this.SBCB(); Debug.Assert(this.Cycles == 4); break;           // SBC (SBCB direct)
                case 0xe2: this.IndexedByte(); this.SBCB(); Debug.Assert(this.Cycles >= 4); break;          // SBC (SBCB indexed)
                case 0xf2: this.ExtendedByte(); this.SBCB(); Debug.Assert(this.Cycles == 5); break;         // SBC (SBCB extended)

                // SEX
                case 0x1d: this.SwallowCurrent(); this.SEX(); Debug.Assert(this.Cycles == 2); break;        // SEX (inherent)

                // ST

                // STA
                case 0x97: this.DirectAddress(); this.STA(); Debug.Assert(this.Cycles == 4); break;         // ST (STA direct)
                case 0xa7: this.IndexedAddress(); this.STA(); Debug.Assert(this.Cycles >= 4); break;        // ST (STA indexed)
                case 0xb7: this.ExtendedAddress(); this.STA(); Debug.Assert(this.Cycles == 5); break;       // ST (STA extended)

                // STB
                case 0xd7: this.DirectAddress(); this.STB(); Debug.Assert(this.Cycles == 4); break;         // ST (STB direct)
                case 0xe7: this.IndexedAddress(); this.STB(); Debug.Assert(this.Cycles >= 4); break;        // ST (STB indexed)
                case 0xf7: this.ExtendedAddress(); this.STB(); Debug.Assert(this.Cycles == 5); break;       // ST (STB extended)

                // STD
                case 0xdd: this.DirectAddress(); this.STD(); Debug.Assert(this.Cycles == 5); break;         // ST (STD direct)
                case 0xed: this.IndexedAddress(); this.STD(); Debug.Assert(this.Cycles >= 5); break;        // ST (STD indexed)
                case 0xfd: this.ExtendedAddress(); this.STD(); Debug.Assert(this.Cycles == 6); break;       // ST (STD extended)

                // STU
                case 0xdf: this.DirectAddress(); this.STU(); Debug.Assert(this.Cycles == 5); break;         // ST (STU direct)
                case 0xef: this.IndexedAddress(); this.STU(); Debug.Assert(this.Cycles >= 5); break;        // ST (STU indexed)
                case 0xff: this.ExtendedAddress(); this.STU(); Debug.Assert(this.Cycles == 6); break;       // ST (STU extended)

                // STX
                case 0x9f: this.DirectAddress(); this.STX(); Debug.Assert(this.Cycles == 5); break;         // ST (STX direct)
                case 0xaf: this.IndexedAddress(); this.STX(); Debug.Assert(this.Cycles >= 5); break;        // ST (STX indexed)
                case 0xbf: this.ExtendedAddress(); this.STX(); Debug.Assert(this.Cycles == 6); break;       // ST (STX extended)

                // SUB

                // SUBA
                case 0x80: this.ImmediateByte(); this.SUBA(); Debug.Assert(this.Cycles == 2); break;        // SUB (SUBA immediate)
                case 0x90: this.DirectByte(); this.SUBA(); Debug.Assert(this.Cycles == 4); break;           // SUB (SUBA direct)
                case 0xa0: this.IndexedByte(); this.SUBA(); Debug.Assert(this.Cycles >= 4); break;          // SUB (SUBA indexed)
                case 0xb0: this.ExtendedByte(); this.SUBA(); Debug.Assert(this.Cycles == 5); break;         // SUB (SUBA extended)

                // SUBB
                case 0xc0: this.ImmediateByte(); this.SUBB(); Debug.Assert(this.Cycles == 2); break;        // SUB (SUBB immediate)
                case 0xd0: this.DirectByte(); this.SUBB(); Debug.Assert(this.Cycles == 4); break;           // SUB (SUBB direct)
                case 0xe0: this.IndexedByte(); this.SUBB(); Debug.Assert(this.Cycles >= 4); break;          // SUB (SUBB indexed)
                case 0xf0: this.ExtendedByte(); this.SUBB(); Debug.Assert(this.Cycles == 5); break;         // SUB (SUBB extended)

                // SUBD
                case 0x83: this.ImmediateWord(); this.SUBD(); Debug.Assert(this.Cycles == 4); break;        // SUB (SUBD immediate)
                case 0x93: this.DirectWord(); this.SUBD(); Debug.Assert(this.Cycles == 6); break;           // SUB (SUBD direct)
                case 0xa3: this.IndexedWord(); this.SUBD(); Debug.Assert(this.Cycles >= 6); break;          // SUB (SUBD indexed)
                case 0xb3: this.ExtendedWord(); this.SUBD(); Debug.Assert(this.Cycles == 7); break;         // SUB (SUBD extended)

                // SWI
                case 0x3f: this.SwallowCurrent(); this.SWI(); Debug.Assert(this.Cycles == 19); break;       // SWI (inherent)

                // SYNC
                case 0x13: this.SwallowCurrent(); this.SYNC(); Debug.Assert(this.Cycles == 2); break;       // SYNC (inherent)

                // TFR
                case 0x1f: this.ImmediateByte(); this.TFR(); Debug.Assert(this.Cycles == 6); break;         // TFR (immediate)

                // TST
                case 0x0d: this.DirectByte(); this.TST(); Debug.Assert(this.Cycles == 6); break;            // TST (direct)
                case 0x4d: this.SwallowCurrent(); this.TSTA(); Debug.Assert(this.Cycles == 2); break;       // TST (TSTA inherent)
                case 0x5d: this.SwallowCurrent(); this.TSTB(); Debug.Assert(this.Cycles == 2); break;       // TST (TSTB inherent)
                case 0x6d: this.IndexedByte(); this.TST(); Debug.Assert(this.Cycles >= 6); break;           // TST (indexed)
                case 0x7d: this.ExtendedByte(); this.TST(); Debug.Assert(this.Cycles == 7); break;          // TST (extended)

                // Branching
                case 0x16: this.RelativeWordAddress(); this.LBRA(); Debug.Assert(this.Cycles == 5); break;  // BRA (LBRA relative)
                case 0x17: this.RelativeWordAddress(); this.LBSR(); Debug.Assert(this.Cycles == 9); break;  // BSR (LBSR relative)
                case 0x20: this.RelativeByteAddress(); this.BRA(); Debug.Assert(this.Cycles == 3); break;   // BRA (relative)
                case 0x21: this.RelativeByteAddress(); this.BRN(); Debug.Assert(this.Cycles == 3); break;   // BRN (relative)
                case 0x22: this.RelativeByteAddress(); this.BHI(); Debug.Assert(this.Cycles == 3); break;   // BHI (relative)
                case 0x23: this.RelativeByteAddress(); this.BLS(); Debug.Assert(this.Cycles == 3); break;   // BLS (relative)
                case 0x24: this.RelativeByteAddress(); this.BCC(); Debug.Assert(this.Cycles == 3); break;   // BCC (relative)
                case 0x25: this.RelativeByteAddress(); this.BCS(); Debug.Assert(this.Cycles == 3); break;   // BCS (relative)
                case 0x26: this.RelativeByteAddress(); this.BNE(); Debug.Assert(this.Cycles == 3); break;   // BNE (relative)
                case 0x27: this.RelativeByteAddress(); this.BEQ(); Debug.Assert(this.Cycles == 3); break;   // BEQ (relative)
                case 0x28: this.RelativeByteAddress(); this.BVC(); Debug.Assert(this.Cycles == 3); break;   // BVC (relative)
                case 0x29: this.RelativeByteAddress(); this.BVS(); Debug.Assert(this.Cycles == 3); break;   // BVS (relative)
                case 0x2a: this.RelativeByteAddress(); this.BPL(); Debug.Assert(this.Cycles == 3); break;   // BPL (relative)
                case 0x2b: this.RelativeByteAddress(); this.BMI(); Debug.Assert(this.Cycles == 3); break;   // BMI (relative)
                case 0x2c: this.RelativeByteAddress(); this.BGE(); Debug.Assert(this.Cycles == 3); break;   // BGE (relative)
                case 0x2d: this.RelativeByteAddress(); this.BLT(); Debug.Assert(this.Cycles == 3); break;   // BLT (relative)
                case 0x2e: this.RelativeByteAddress(); this.BGT(); Debug.Assert(this.Cycles == 3); break;   // BGT (relative)
                case 0x2f: this.RelativeByteAddress(); this.BLE(); Debug.Assert(this.Cycles == 3); break;   // BLE (relative)

                case 0x8d: this.RelativeByteAddress(); this.BSR(); Debug.Assert(this.Cycles == 7); break;   // BSR (relative)

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
                case 0x83: this.ImmediateWord(); this.CMPD(); Debug.Assert(this.Cycles == 5); break;        // CMP (CMPD, immediate)
                case 0x93: this.DirectWord(); this.CMPD(); Debug.Assert(this.Cycles == 7); break;           // CMP (CMPD, direct)
                case 0xa3: this.IndexedWord(); this.CMPD(); Debug.Assert(this.Cycles >= 7); break;          // CMP (CMPD, indexed)
                case 0xb3: this.ExtendedWord(); this.CMPD(); Debug.Assert(this.Cycles == 8); break;         // CMP (CMPD, extended)

                // CMPY
                case 0x8c: this.ImmediateWord(); this.CMPY(); Debug.Assert(this.Cycles == 5); break;        // CMP (CMPY, immediate)
                case 0x9c: this.DirectWord(); this.CMPY(); Debug.Assert(this.Cycles == 7); break;           // CMP (CMPY, direct)
                case 0xac: this.IndexedWord(); this.CMPY(); Debug.Assert(this.Cycles >= 7); break;          // CMP (CMPY, indexed)
                case 0xbc: this.ExtendedWord(); this.CMPY(); Debug.Assert(this.Cycles == 8); break;         // CMP (CMPY, extended)

                // LD

                // LDS
                case 0xce: this.ImmediateWord(); this.LDS(); Debug.Assert(this.Cycles == 4); break;         // LD (LDS immediate)
                case 0xde: this.DirectWord(); this.LDS(); Debug.Assert(this.Cycles == 6); break;            // LD (LDS direct)
                case 0xee: this.IndexedWord(); this.LDS(); Debug.Assert(this.Cycles >= 6); break;           // LD (LDS indexed)
                case 0xfe: this.ExtendedWord(); this.LDS(); Debug.Assert(this.Cycles == 7); break;          // LD (LDS extended)

                // LDY
                case 0x8e: this.ImmediateWord(); this.LDY(); Debug.Assert(this.Cycles == 4); break;         // LD (LDY immediate)
                case 0x9e: this.DirectWord(); this.LDY(); Debug.Assert(this.Cycles == 6); break;            // LD (LDY direct)
                case 0xae: this.IndexedWord(); this.LDY(); Debug.Assert(this.Cycles >= 6); break;           // LD (LDY indexed)
                case 0xbe: this.ExtendedWord(); this.LDY(); Debug.Assert(this.Cycles == 7); break;          // LD (LDY extended)

                // Branching
                case 0x21: this.RelativeWordAddress(); this.LBRN(); Debug.Assert(this.Cycles == 5); break;  // BRN (LBRN relative)
                case 0x22: this.RelativeWordAddress(); this.LBHI(); Debug.Assert(this.Cycles >= 5); break;  // BHI (LBHI relative)
                case 0x23: this.RelativeWordAddress(); this.LBLS(); Debug.Assert(this.Cycles >= 5); break;  // BLS (LBLS relative)
                case 0x24: this.RelativeWordAddress(); this.LBCC(); Debug.Assert(this.Cycles >= 5); break;  // BCC (LBCC relative)
                case 0x25: this.RelativeWordAddress(); this.LBCS(); Debug.Assert(this.Cycles >= 5); break;  // BCS (LBCS relative)
                case 0x26: this.RelativeWordAddress(); this.LBNE(); Debug.Assert(this.Cycles >= 5); break;  // BNE (LBNE relative)
                case 0x27: this.RelativeWordAddress(); this.LBEQ(); Debug.Assert(this.Cycles >= 5); break;  // BEQ (LBEQ relative)
                case 0x28: this.RelativeWordAddress(); this.LBVC(); Debug.Assert(this.Cycles >= 5); break;  // BVC (LBVC relative)
                case 0x29: this.RelativeWordAddress(); this.LBVS(); Debug.Assert(this.Cycles >= 5); break;  // BVS (LBVS relative)
                case 0x2a: this.RelativeWordAddress(); this.LBPL(); Debug.Assert(this.Cycles >= 5); break;  // BPL (LBPL relative)
                case 0x2b: this.RelativeWordAddress(); this.LBMI(); Debug.Assert(this.Cycles >= 5); break;  // BMI (LBMI relative)
                case 0x2c: this.RelativeWordAddress(); this.LBGE(); Debug.Assert(this.Cycles >= 5); break;  // BGE (LBGE relative)
                case 0x2d: this.RelativeWordAddress(); this.LBLT(); Debug.Assert(this.Cycles >= 5); break;  // BLT (LBLT relative)
                case 0x2e: this.RelativeWordAddress(); this.LBGT(); Debug.Assert(this.Cycles >= 5); break;  // BGT (LBGT relative)
                case 0x2f: this.RelativeWordAddress(); this.LBLE(); Debug.Assert(this.Cycles >= 5); break;  // BLE (LBLE relative)

                // STS
                case 0xdf: this.DirectAddress(); this.STS(); Debug.Assert(this.Cycles == 6); break;         // ST (STS direct)
                case 0xef: this.IndexedAddress(); this.STS(); Debug.Assert(this.Cycles >= 6); break;        // ST (STS indexed)
                case 0xff: this.ExtendedAddress(); this.STS(); Debug.Assert(this.Cycles == 7); break;       // ST (STS extended)

                // STY
                case 0x9f: this.DirectAddress(); this.STY(); Debug.Assert(this.Cycles == 6); break;         // ST (STY direct)
                case 0xaf: this.IndexedAddress(); this.STY(); Debug.Assert(this.Cycles >= 6); break;        // ST (STY indexed)
                case 0xbf: this.ExtendedAddress(); this.STY(); Debug.Assert(this.Cycles == 7); break;       // ST (STY extended)

                // SWI
                case 0x3f: this.SwallowCurrent(); this.SWI2(); Debug.Assert(this.Cycles == 20); break;      // SWI (SWI2 inherent)

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
                case 0x83: this.ImmediateWord(); this.CMPU(); Debug.Assert(this.Cycles == 5); break;        // CMP (CMPU, immediate)
                case 0x93: this.DirectWord(); this.CMPU(); Debug.Assert(this.Cycles == 7); break;           // CMP (CMPU, direct)
                case 0xa3: this.IndexedWord(); this.CMPU(); Debug.Assert(this.Cycles >= 7); break;          // CMP (CMPU, indexed)
                case 0xb3: this.ExtendedWord(); this.CMPU(); Debug.Assert(this.Cycles == 8); break;         // CMP (CMPU, extended)

                // CMPS
                case 0x8c: this.ImmediateWord(); this.CMPS(); Debug.Assert(this.Cycles == 5); break;        // CMP (CMPS, immediate)
                case 0x9c: this.DirectWord(); this.CMPS(); Debug.Assert(this.Cycles == 7); break;           // CMP (CMPS, direct)
                case 0xac: this.IndexedWord(); this.CMPS(); Debug.Assert(this.Cycles >= 7); break;          // CMP (CMPS, indexed)
                case 0xbc: this.ExtendedWord(); this.CMPS(); Debug.Assert(this.Cycles == 8); break;         // CMP (CMPS, extended)

                // SWI
                case 0x3f: this.SwallowCurrent(); this.SWI3(); Debug.Assert(this.Cycles == 20); break;      // SWI (SWI3 inherent)

                default:
                    throw new InvalidOperationException("Unknown 11 prefixed op-code");
            }
        }

        #endregion

        #region Miscellaneous instruction implementations

        private static void NOP()
        {
            // No operation!
        }

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
            this.SwallowRead();
        }

        private void ANDCC()
        {
            this.CC &= this.Bus.Data;
            this.SwallowRead();
        }

        private void ANDA() => this.A = this.And(this.A, this.Bus.Data);
        private void ANDB() => this.B = this.And(this.B, this.Bus.Data);

        private byte And(byte operand, byte data) => this.Through((byte)(operand & data));

        private void ASLA() => this.A = this.ArithmeticShiftLeft(this.A);

        private void ASLB() => this.B = this.ArithmeticShiftLeft(this.B);

        private void ASL()
        {
            var result = this.ArithmeticShiftLeft(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

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

        private void ASR()
        {
            var result = this.ArithmeticShiftRight(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

        private byte ArithmeticShiftRight(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)(operand >> 1 | (operand & (byte)Bits.Bit7));
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void BITA() => this.Bit(this.A, this.Bus.Data);
        private void BITB() => this.Bit(this.B, this.Bus.Data);

        private void Bit(byte operand, byte data) => this.And(operand, data);

        private void CLRA() => this.A = this.Clear();

        private void CLRB() => this.B = this.Clear();

        private void CLR()
        {
            this.SwallowEffectiveAddress();
            var result = this.Clear();
            this.SwallowEffectiveAddress();
            this.MemoryWrite(this.Intermediate, result);
        }

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

        private void COM()
        {
            var result = this.Complement(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

        private byte Complement(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF);
            return this.Through((byte)~operand);
        }

        private void CWAI()
        {
            this.SwallowRead();
            this.CC &= this.Bus.Data;
            this.SaveEntireRegisterState();
            this.Halt();
        }

        private void DAA()
        {
            var original = this.A;

            var lowNibble = LowNibble(original);
            var lowAdjust = this.HalfCarry != 0 || lowNibble > 9;

            var highNibble = HighNibble(original);
            var highAdjust = this.Carry != 0 || highNibble > 9 || (highNibble == 9 && lowNibble > 9);

            byte correction = 0;

            if (lowAdjust)
            {
                correction |= 0x06;
            }

            if (highAdjust)
            {
                correction |= 0x60;
            }

            var result = (byte)(original + correction);
            var newCarry = (correction & 0x60) != 0;
            this.A = this.Through(result);
            this.CC = SetBit(this.CC, StatusBits.CF, newCarry);
        }

        private void EORA() => this.A = this.ExclusiveOr(this.A, this.Bus.Data);
        private void EORB() => this.B = this.ExclusiveOr(this.B, this.Bus.Data);

        private byte ExclusiveOr(byte operand, byte data) => this.Through((byte)(operand ^ data));

        private void DECA() => this.A = this.Decrement(this.A);

        private void DECB() => this.B = this.Decrement(this.B);

        private void DEC()
        {
            var result = this.Decrement(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

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

        private void INC()
        {
            var result = this.Increment(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

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

        private void JSR()
        {
            this.SwallowEffectiveAddress();
            this.SwallowRead();
            this.Call(this.Intermediate);
        }

        private void LSRA() => this.A = this.LogicalShiftRight(this.A);

        private void LSRB() => this.B = this.LogicalShiftRight(this.B);

        private void LSR()
        {
            var result = this.LogicalShiftRight(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

        private byte LogicalShiftRight(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            this.CC = this.AdjustNZ(operand >>= 1);
            return operand;
        }

        private void MUL()
        {
            this.SwallowRead(9);
            this.D.Word = (ushort)(this.A * this.B);
            this.CC = this.AdjustZero(this.D);
            this.CC = SetBit(this.CC, StatusBits.CF, this.D.Low & (byte)Bits.Bit7);
        }

        private void NEGA() => this.A = this.Negate(this.A);

        private void NEGB() => this.B = this.Negate(this.B);

        private void NEG()
        {
            var result = this.Negate(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

        private byte Negate(byte operand)
        {
            this.CC = SetBit(this.CC, StatusBits.VF, operand == (byte)Bits.Bit7);
            this.Intermediate.Word = (ushort)(0 - operand);
            operand = this.Intermediate.Low;
            this.CC = this.AdjustNZ(operand);
            this.CC = this.AdjustCarry(this.Intermediate);
            return operand;
        }

        private void ORCC()
        {
            this.CC |= this.Bus.Data;
            this.SwallowRead();
        }

        private void ORA() => this.A = this.Or(this.A);
        private void ORB() => this.B = this.Or(this.B);

        private byte Or(byte operand) => this.Through((byte)(operand | this.Bus.Data));

        private void ROLA() => this.A = this.RotateLeft(this.A);

        private void ROLB() => this.B = this.RotateLeft(this.B);

        private void ROL()
        {
            var result = this.RotateLeft(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

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

        private void ROR()
        {
            var result = this.RotateRight(this.Bus.Data);
            this.SwallowEffectiveAddress();
            this.MemoryWrite(result);
        }

        private byte RotateRight(byte operand)
        {
            var carryIn = this.Carry;
            this.CC = SetBit(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)(operand >> 1 | carryIn << 7);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void RTI()
        {
            this.RestoreRegisterState();
            this.SwallowRead();
        }

        private void RTS()
        {
            this.Return();
            this.SwallowRead();
        }

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
            this.SwallowRead();
        }

        private void SEX() => this.A = this.SEX(this.B);

        private byte SEX(byte from)
        {
            this.CC = this.AdjustNZ(from);
            return (from & (byte)Bits.Bit7) != 0 ? (byte)Mask.Eight : (byte)0;
        }

        private void SWI()
        {
            this.SwallowRead();
            this.SaveEntireRegisterState();
            this.CC = SetBit(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetBit(this.CC, StatusBits.FF);  // Disable FIRQ
            this.SwallowRead();
            this.GetWordPaged(0xff, SWI_vector);
            this.Jump(this.Intermediate);
            this.SwallowRead();
        }

        private void SWI2()
        {
            
            this.SwallowRead();
            this.SaveEntireRegisterState();
            this.SwallowRead();
            this.GetWordPaged(0xff, SWI2_vector);
            this.Jump(this.Intermediate);
            this.SwallowRead();
        }

        private void SWI3()
        {
            
            this.SwallowRead();
            this.SaveEntireRegisterState();
            this.SwallowRead();
            this.GetWordPaged(0xff, SWI3_vector);
            this.Jump(this.Intermediate);
            this.SwallowRead();
        }

        private void TSTA() => this.Test(this.A);

        private void TSTB() => this.Test(this.B);

        private void TST()
        {
            this.Test(this.Bus.Data);
            this.SwallowRead(2);
        }

        private void Test(byte data) => this.Compare(data, 0);

        private void LEAX()
        {
            this.X.Assign(this.Intermediate);
            this.CC = this.AdjustZero(this.X);
        }

        private void LEAY()
        {
            this.Y.Assign(this.Intermediate);
            this.CC = this.AdjustZero(this.Y);
        }

        private void LEAS() => this.S.Assign(this.Intermediate);

        private void LEAU() => this.U.Assign(this.Intermediate);

        private void ABX()
        {
            this.X.Word += this.B;
            this.SwallowRead();
        }

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
