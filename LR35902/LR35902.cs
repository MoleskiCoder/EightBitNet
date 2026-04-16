// <copyright file="LR35902.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;
    using System.Diagnostics;

    public sealed class LR35902 : IntelProcessor
    {
        public LR35902(Bus bus)
        : base(bus)
        {
            this._bus = bus;
            this.RaisedPOWER += this.LR35902_RaisedPOWER;
            this.RaisingHALT += this.LR35902_RaisingHALT;
        }

        private readonly Bus _bus;
        private readonly Register16 _af = new((int)Mask.Sixteen);
        private bool _prefixCB;

        public int MachineCycles => this.Cycles / 4;

        public override Register16 AF
        {
            get
            {
                this._af.Low = (byte)HigherNibble(this._af.Low);
                return this._af;
            }
        }

        public override Register16 BC { get; } = new Register16((int)Mask.Sixteen);

        public override Register16 DE { get; } = new Register16((int)Mask.Sixteen);

        public override Register16 HL { get; } = new Register16((int)Mask.Sixteen);

        public bool IME { get; set; }

        private static readonly Register16 _addressIE = new(IoRegisters.IE, IoRegisters.BasePage);

        public byte IE
        {
            get => this.Bus.Peek(_addressIE);
            set => this.Bus.Poke(_addressIE, value);
        }

        public byte IF
        {
            get => this._bus.IO.Peek(IoRegisters.IF);
            set => this._bus.IO.Poke(IoRegisters.IF, value);
        }

        public byte MaskedInterrupts => (byte)(this.IE & this.IF);

        private bool Stopped { get; set; }

        public bool EI { get; set; }

        #region MWR pin

        public event EventHandler<EventArgs>? RaisingMWR;

        public event EventHandler<EventArgs>? RaisedMWR;

        public event EventHandler<EventArgs>? LoweringMWR;

        public event EventHandler<EventArgs>? LoweredMWR;

        private PinLevel _mwrLine = PinLevel.Low;

        public ref PinLevel MWR => ref this._mwrLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseMWR()
        {
            if (this.MWR.Lowered())
            {
                RaisingMWR?.Invoke(this, EventArgs.Empty);
                this.MWR.Raise();
                RaisedMWR?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LowerMWR()
        {
            if (this.MWR.Raised())
            {
                LoweringMWR?.Invoke(this, EventArgs.Empty);
                this.MWR.Lower();
                LoweredMWR?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region RD pin

        public event EventHandler<EventArgs>? RaisingRD;

        public event EventHandler<EventArgs>? RaisedRD;

        public event EventHandler<EventArgs>? LoweringRD;

        public event EventHandler<EventArgs>? LoweredRD;

        private PinLevel _rdLine = PinLevel.Low;

        public ref PinLevel RD => ref this._rdLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseRD()
        {
            if (this.RD.Lowered())
            {
                RaisingRD?.Invoke(this, EventArgs.Empty);
                this.RD.Raise();
                RaisedRD?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LowerRD()
        {
            if (this.RD.Raised())
            {
                LoweringRD?.Invoke(this, EventArgs.Empty);
                this.RD.Lower();
                LoweredRD?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region WR pin

        public event EventHandler<EventArgs>? RaisingWR;

        public event EventHandler<EventArgs>? RaisedWR;

        public event EventHandler<EventArgs>? LoweringWR;

        public event EventHandler<EventArgs>? LoweredWR;

        private PinLevel _wrLine = PinLevel.Low;

        public ref PinLevel WR => ref this._wrLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public void RaiseWR()
        {
            if (this.WR.Lowered())
            {
                RaisingWR?.Invoke(this, EventArgs.Empty);
                this.WR.Raise();
                RaisedWR?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LowerWR()
        {
            if (this.WR.Raised())
            {
                LoweringWR?.Invoke(this, EventArgs.Empty);
                this.WR.Lower();
                LoweredWR?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        public override void Execute()
        {
            var decoded = this.GetDecodedOpCode(this.OpCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            if (this._prefixCB)
            {
                this.ExecuteCB(x, y, z);
            }
            else
            {
                this.ExecuteOther(x, y, z, p, q);
            }

            System.Diagnostics.Debug.Assert(this.Cycles > 0, $"No timing associated with instruction (CB prefixed? {this._prefixCB}) 0x{this.OpCode:X2}");
        }

        public override void PoweredStep()
        {
            this._prefixCB = false;

            if (this.EI)
            {
                this.EnableInterrupts();
                this.EI = false;
            }

            if (this.MaskedInterrupts != 0)
            {
                if (this.IME)
                {
                    var index = FindFirstSet(this.MaskedInterrupts);
                    this.IF = 0;
                    this.LowerINT();
                    this.Bus.Data = (byte)(0x38 + (index << 3));
                }
                else
                {
                    this.RaiseHALT();
                }
            }

            if (this.RESET.Lowered())
            {
                this.HandleRESET();
            }
            else if (this.INT.Lowered())
            {
                this.HandleINT();
            }
            else
            {
                this.Execute(this.FetchInstruction());
            }
        }

        private void LR35902_RaisedPOWER(object? sender, EventArgs e)
        {
            this.RaiseWR();
            this.RaiseRD();
            this.RaiseMWR();
            this.EI = false;
        }

        private void LR35902_RaisingHALT(object? sender, EventArgs e)
        {
            this.PC.Increment();
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.SP.Word = (ushort)(Mask.Sixteen - 1);
            this.TickMachine(4);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.Restart(this.Bus.Data);
        }

        public event EventHandler<EventArgs>? MachineTicked;

        private void TickMachine(int extra)
        {
            for (var i = 0; i < extra; ++i)
            {
                this.TickMachine();
            }
        }

        private void TickMachine()
        {
            this.Tick(4);
            MachineTicked?.Invoke(this, EventArgs.Empty);
        }

        private int Zero() => ZeroTest(this.F);

        private int Carry() => CarryTest(this.F);

        private int HalfCarry() => HalfCarryTest(this.F);

        private int Subtracting() => SubtractingTest(this.F);

        private static int ZeroTest(byte data) => data & (byte)StatusBits.ZF;

        private static int CarryTest(byte data) => data & (byte)StatusBits.CF;

        private static int HalfCarryTest(byte data) => data & (byte)StatusBits.HC;

        private static int SubtractingTest(byte data) => data & (byte)StatusBits.NF;

        private void AdjustStatusFlags(byte value) => this.F = value;

        private void SetBit(StatusBits flag) => this.AdjustStatusFlags(SetBit(this.F, flag));

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private void SetBit(StatusBits flag, int condition) => this.AdjustStatusFlags(SetBit(this.F, flag, condition));

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private void SetBit(StatusBits flag, bool condition) => this.AdjustStatusFlags(SetBit(this.F, flag, condition));

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private void ClearBit(StatusBits flag) => this.AdjustStatusFlags(ClearBit(this.F, flag));

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private void ClearBit(StatusBits flag, int condition) => this.AdjustStatusFlags(ClearBit(this.F, flag, condition));

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private void AdjustZero(byte value) => this.AdjustStatusFlags(AdjustZero(this.F, value));

        private static byte AdjustZero(byte input, byte value) => ClearBit(input, StatusBits.ZF, value);

        private void AdjustHalfCarryAdd(byte before, byte value, int calculation) => this.AdjustStatusFlags(AdjustHalfCarryAdd(this.F, before, value, calculation));

        private static byte AdjustHalfCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarryAdd(before, value, calculation));

        private void AdjustHalfCarrySub(byte before, byte value, int calculation) => this.AdjustStatusFlags(AdjustHalfCarrySub(this.F, before, value, calculation));

        private static byte AdjustHalfCarrySub(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarrySub(before, value, calculation));

        private static byte RES(int n, byte operand) => ClearBit(operand, Bit(n));

        private static byte SET(int n, byte operand) => SetBit(operand, Bit(n));

        protected override void DisableInterrupts() => this.IME = false;

        protected override void EnableInterrupts() => this.IME = true;

        private void Stop() => this.Stopped = true;

        private void Start() => this.Stopped = false;

        private void MemoryUpdate(int ticks = 1)
        {
            Debug.Assert(ticks > 0, "Ticks must be greater than zero");
            this.OnWritingMemory();
            this.LowerMWR();
                this.LowerWR();
                    base.MemoryWrite();
                    this.TickMachine(ticks);
                this.RaiseWR();
            this.RaiseMWR();
            this.OnWrittenMemory();
        }

        protected override void MemoryWrite()
        {
            this.MemoryUpdate();
        }

        protected override byte MemoryRead()
        {
            this.OnReadingMemory();
            this.LowerMWR();
                this.LowerRD();
                    _ = base.MemoryRead();
                    this.TickMachine();
                this.RaiseRD();
            this.RaiseMWR();
            this.OnReadMemory();
            return this.Bus.Data;
        }

        protected override void PushWord(Register16 value)
        {
            this.TickMachine();
            base.PushWord(value);
        }

        protected override void JumpRelative(sbyte offset)
        {
            base.JumpRelative(offset);
            this.TickMachine();
        }

        protected override void JumpConditional(bool condition)
        {
            base.JumpConditional(condition);
            if (condition)
            {
                this.TickMachine();
            }
        }

        protected override void ReturnConditional(bool condition)
        {
            this.TickMachine();
            base.ReturnConditional(condition);
        }

        protected override void Return()
        {
            base.Return();
            this.TickMachine();
        }

        protected override void JumpIndirect()
        {
            base.JumpIndirect();
            this.TickMachine();
        }

        private ref byte R(int r, AccessLevel access = AccessLevel.ReadOnly)
        {
            switch (r)
            {
                case 0:
                    return ref this.B;
                case 1:
                    return ref this.C;
                case 2:
                    return ref this.D;
                case 3:
                    return ref this.E;
                case 4:
                    return ref this.H;
                case 5:
                    return ref this.L;
                case 6:
                    this.Bus.Address.Assign(this.HL);
                    switch (access)
                    {
                        case AccessLevel.ReadOnly:
                            this.MemoryRead();
                            break;
                        case AccessLevel.WriteOnly:
                            break;
                        default:
                            throw new NotSupportedException("Invalid access level");
                    }

                    // Will need a post-MemoryWrite
                    return ref this.Bus.Data;
                case 7:
                    return ref this.A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void R(int r, byte value, int ticks = 1)
        {
            this.R(r, AccessLevel.WriteOnly) = value;
            if (r == 6)
            {
                this.MemoryUpdate(ticks);
            }
        }

        private Register16 RP(int rp) => rp switch
        {
            0 => this.BC,
            1 => this.DE,
            2 => this.HL,
            3 => this.SP,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private Register16 RP2(int rp) => rp switch
        {
            0 => this.BC,
            1 => this.DE,
            2 => this.HL,
            3 => this.AF,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private void ExecuteCB(int x, int y, int z)
        {
            var operand = this.R(z);
            var update = x != 1; // BIT does not update
            switch (x)
            {
                case 0: // rot[y] r[z]
                    {
                        operand = y switch
                        {
                            0 => this.RLC(operand),
                            1 => this.RRC(operand),
                            2 => this.RL(operand),
                            3 => this.RR(operand),
                            4 => this.SLA(operand),
                            5 => this.SRA(operand),
                            6 => this.Swap(operand),    // GB: SWAP r
                            7 => this.SRL(operand),
                            _ => throw new InvalidOperationException("Unreachable code block reached"),
                        };
                        this.AdjustZero(operand);
                    }
                    break;

                case 1: // BIT y, r[z]
                    this.Bit(y, operand);
                    break;

                case 2: // RES y, r[z]
                    operand = RES(y, operand);
                    break;

                case 3: // SET y, r[z]
                    operand = SET(y, operand);
                    break;

                default:
                    throw new InvalidOperationException("Unreachable code block reached");
            }

            if (update)
            {
                this.R(z, operand);
            }
        }

        private void ExecuteOther(int x, int y, int z, int p, int q)
        {
            var memoryY = y == 6;
            var memoryZ = z == 6;
            switch (x)
            {
                case 0:
                    switch (z)
                    {
                        case 0: // Relative jumps and assorted ops
                            switch (y)
                            {
                                case 0: // NOP
                                    break;
                                case 1: // GB: LD (nn),SP
                                    this.FetchWord();
                                    this.Bus.Address.Assign(this.Intermediate);
                                    this.SetWord(this.SP);
                                    break;
                                case 2: // GB: STOP
                                    this.Stop();
                                    this.TickMachine(2);
                                    break;
                                case 3: // JR d
                                    this.JumpRelative(this.FetchByte());
                                    break;
                                case 4: // JR cc,d
                                case 5:
                                case 6:
                                case 7:
                                    this.JumpRelativeConditionalFlag(y - 4);
                                    break;
                                default:
                                    throw new InvalidOperationException("Unreachable code block reached");
                            }

                            break;

                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.FetchInto(this.RP(p));
                                    break;

                                case 1: // ADD HL,rp
                                    this.Add(this.RP(p));
                                    this.TickMachine();
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;

                        case 2: // Indirect loading
                            switch (q)
                            {
                                case 0:
                                    switch (p)
                                    {
                                        case 0: // LD (BC),A
                                            this.WriteMemoryIndirect(this.BC, A);
                                            break;

                                        case 1: // LD (DE),A
                                            this.WriteMemoryIndirect(this.DE, A);
                                            break;

                                        case 2: // GB: LDI (HL),A
                                            this.Bus.Address.Assign(this.HL);
                                            _ = this.HL.Increment();
                                            this.MemoryWrite(this.A);
                                            break;

                                        case 3: // GB: LDD (HL),A
                                            this.Bus.Address.Assign(this.HL);
                                            _ = this.HL.Decrement();
                                            this.MemoryWrite(this.A);
                                            break;

                                        default:
                                            throw new InvalidOperationException("Invalid operation mode");
                                    }
                                    break;

                                case 1:
                                    switch(p)
                                    {
                                        case 0:   // LD A,(BC)
                                            this.A = this.ReadMemoryIndirect(this.BC);
                                            break;
                                        case 1:   // LD A,(DE)
                                            this.A = this.ReadMemoryIndirect(this.DE);
                                            break;
                                        case 2:   // GB: LDI A,(HL)
                                            this.Bus.Address.Assign(this.HL);
                                            _ = this.HL.Increment();
                                            this.A = this.MemoryRead();
                                            break;
                                        case 3:   // GB: LDD A,(HL)
                                            this.Bus.Address.Assign(this.HL);
                                            _ = this.HL.Decrement();
                                            this.A = this.MemoryRead();
                                            break;
                                        default:
                                            throw new InvalidOperationException("Invalid operation mode");
                                    }
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;

                        case 3: // 16-bit INC/DEC
                            switch (q)
                            {
                                case 0: // INC rp
                                    this.RP(p).Increment();
                                    break;
                                
                                case 1: // DEC rp
                                    this.RP(p).Decrement();
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }
                            this.TickMachine();
                            break;

                        case 4: // 8-bit INC
                            this.R(y, this.Increment(this.R(y)));
                            break;

                        case 5: // 8-bit DEC
                            this.R(y, this.Decrement(this.R(y)));
                            break;

                        case 6: // 8-bit load immediate
                            this.R(y, this.FetchByte());
                            break;

                        case 7: // Assorted operations on accumulator/flags
                            switch (y)
                            {
                                case 0:
                                    this.A = this.RLC(this.A);
                                    break;
                                case 1:
                                    this.A = this.RRC(this.A);
                                    break;
                                case 2:
                                    this.A = this.RL(this.A);
                                    break;
                                case 3:
                                    this.A = this.RR(this.A);
                                    break;
                                case 4:
                                    this.DAA();
                                    break;
                                case 5:
                                    this.CPL();
                                    break;
                                case 6:
                                    this.SCF();
                                    break;
                                case 7:
                                    this.CCF();
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }
                            break;

                        default:
                            throw new InvalidOperationException("Invalid operation mode");
                    }

                    break;

                case 1: // 8-bit loading
                    if (memoryZ && memoryY)
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                        this.TickMachine(2);
                    }
                    else
                    {
                        this.R(y, this.R(z));
                    }
                    break;

                case 2:
                    { // Operate on accumulator and register/memory location
                        var value = this.R(z);
                        switch (y)
                        {
                            case 0: // ADD A,r
                                this.A = this.Add(this.A, value);
                                break;
                            case 1: // ADC A,r
                                this.A = this.ADC(this.A, value);
                                break;
                            case 2: // SUB r
                                this.A = this.Subtract(this.A, value);
                                break;
                            case 3: // SBC A,r
                                this.A = this.SBC(this.A, value);
                                break;
                            case 4: // AND r
                                this.AndR(value);
                                break;
                            case 5: // XOR r
                                this.XorR(value);
                                break;
                            case 6: // OR r
                                this.OrR(value);
                                break;
                            case 7: // CP r
                                this.Compare(value);
                                break;
                            default:
                                throw new InvalidOperationException("Invalid operation mode");
                        }
                        break;
                    }
                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            switch (y)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                    this.ReturnConditionalFlag(y);
                                    break;

                                case 4: // GB: LD (FF00 + n),A
                                    this.MemoryWrite(this.FetchByte(), IoRegisters.BasePage, this.A);
                                    break;

                                case 5:
                                    { // GB: ADD SP,dd
                                        var before = this.SP.Word;
                                        var value = (sbyte)this.FetchByte();
                                        this.TickMachine(2);
                                        var result = before + value;
                                        this.SP.Word = (ushort)result;
                                        var carried = before ^ value ^ (result & (int)Mask.Sixteen);
                                        this.ClearBit(StatusBits.ZF | StatusBits.NF);
                                        this.SetBit(StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.SetBit(StatusBits.HC, carried & (int)Bits.Bit4);
                                    }
                                    break;

                                case 6: // GB: LD A,(FF00 + n)
                                    this.A = this.MemoryRead(this.FetchByte(), IoRegisters.BasePage);
                                    break;

                                case 7:
                                    { // GB: LD HL,SP + dd
                                        var before = this.SP.Word;
                                        var value = (sbyte)this.FetchByte();
                                        this.TickMachine();
                                        var result = before + value;
                                        this.HL.Word = (ushort)result;
                                        var carried = before ^ value ^ (result & (int)Mask.Sixteen);
                                        this.ClearBit(StatusBits.ZF | StatusBits.NF);
                                        this.SetBit(StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.SetBit(StatusBits.HC, carried & (int)Bits.Bit4);
                                    }
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    this.PopInto(this.RP2(p));
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            this.Return();
                                            break;
                                        case 1: // GB: RETI
                                            this.RetI();
                                            break;
                                        case 2: // JP HL
                                            this.Jump(this.HL);
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Assign(this.HL);
                                            this.TickMachine();
                                            break;
                                        default:
                                            throw new InvalidOperationException("Invalid operation mode");
                                    }

                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;
                        case 2: // Conditional jump
                            switch (y)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                    this.JumpConditionalFlag(y);
                                    break;
                                case 4: // GB: LD (FF00 + C),A
                                    this.MemoryWrite(this.C, IoRegisters.BasePage, this.A);
                                    break;
                                case 5: // GB: LD (nn),A
                                    this.FetchInto(this.MEMPTR);
                                    this.MemoryWrite(this.MEMPTR, this.A);
                                    break;
                                case 6: // GB: LD A,(FF00 + C)
                                    this.A = this.MemoryRead(this.C, IoRegisters.BasePage);
                                    break;
                                case 7: // GB: LD A,(nn)
                                    this.FetchInto(this.MEMPTR);                                   
                                    this.A = this.MemoryRead(this.MEMPTR);
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    this.JumpIndirect();
                                    break;
                                case 1: // CB prefix
                                    this._prefixCB = true;
                                    this.Execute(this.FetchInstruction());
                                    break;
                                case 6: // DI
                                    this.DisableInterrupts();
                                    break;
                                case 7: // EI
                                    this.EI = true;
                                    break;
                                default:
                                    break;
                            }

                            break;

                        case 4: // Conditional call: CALL cc[y], nn
                            this.CallConditionalFlag(y);
                            break;

                        case 5: // PUSH & various ops
                            switch (q)
                            {
                                case 0: // PUSH rp2[p]
                                    this.PushWord(this.RP2(p));
                                    break;

                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            this.CallIndirect();
                                            break;
                                        default:
                                            break;
                                    }

                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;

                        case 6:
                            { // Operate on accumulator and immediate operand: alu[y] n
                                var operand = this.FetchByte();
                                switch (y)
                                {
                                    case 0: // ADD A,n
                                        this.A = this.Add(this.A, operand);
                                        break;
                                    case 1: // ADC A,n
                                        this.A = this.ADC(this.A, operand);
                                        break;
                                    case 2: // SUB n
                                        this.A = this.Subtract(this.A, operand);
                                        break;
                                    case 3: // SBC A,n
                                        this.A = this.SBC(this.A, operand);
                                        break;
                                    case 4: // AND n
                                        this.AndR(operand);
                                        break;
                                    case 5: // XOR n
                                        this.XorR(operand);
                                        break;
                                    case 6: // OR n
                                        this.OrR(operand);
                                        break;
                                    case 7: // CP n
                                        this.Compare(operand);
                                        break;
                                    default:
                                        throw new InvalidOperationException("Invalid operation mode");
                                }
                                break;
                            }

                        case 7: // Restart: RST y * 8
                            this.Restart((byte)(y << 3));
                            break;

                        default:
                            throw new InvalidOperationException("Invalid operation mode");
                    }

                    break;
                default:
                    break;
            }
        }

        private byte Increment(byte operand)
        {
            this.ClearBit(StatusBits.NF);
            var result = operand; ++result;
            this.AdjustZero(result);
            this.ClearBit(StatusBits.HC, LowNibble(result));
            return result;
        }

        private byte Decrement(byte operand)
        {
            this.SetBit(StatusBits.NF);
            this.ClearBit(StatusBits.HC, LowNibble(operand));
            var result = operand; --result;
            this.AdjustZero(result);
            return result;
        }

        protected sealed override bool ConvertCondition(int flag) => flag switch
        {
            0 => this.Zero() == 0,  // NZ
            1 => this.Zero() != 0,  // Z
            2 => this.Carry() == 0, // NC
            3 => this.Carry() != 0, // C
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        private void Add(Register16 value)
        {
            this.Intermediate.Assign(this.HL);
            var result = this.HL.Word + value.Word;
            this.HL.Word = (ushort)result;
            this.ClearBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, result & (int)Bits.Bit16);
            this.AdjustHalfCarryAdd(this.Intermediate.High, value.High, this.HL.High);
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand + value + carry);
            var result = this.Intermediate.Low;

            this.AdjustHalfCarryAdd(operand, value, result);

            this.ClearBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, this.Intermediate.High & (byte)Bits.Bit0);
            this.AdjustZero(result);

            return result;
        }

        private byte ADC(byte operand, byte value) => this.Add(operand, value, this.Carry() >> 4);

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand - value - carry);
            var result = this.Intermediate.Low;

            this.AdjustHalfCarrySub(operand, value, result);

            this.SetBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, this.Intermediate.High & (byte)Bits.Bit0);
            this.AdjustZero(result);

            return result;
        }

        private byte SBC(byte operand, byte value) => this.Subtract(operand, value, this.Carry() >> 4);

        private void AndR(byte value)
        {
            this.SetBit(StatusBits.HC);
            this.ClearBit(StatusBits.CF | StatusBits.NF);
            this.AdjustZero(this.A &= value);
        }

        private void XorR(byte value)
        {
            this.ClearBit(StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.AdjustZero(this.A ^= value);
        }

        private void OrR(byte value)
        {
            this.ClearBit(StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.AdjustZero(this.A |= value);
        }

        private void Compare(byte value) => this.Subtract(this.A, value);

        private byte RLC(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = operand & (byte)Bits.Bit7;
            this.SetBit(StatusBits.CF, carry);
            return (byte)((operand << 1) | (carry >> 7));
        }

        private byte RRC(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = operand & (byte)Bits.Bit0;
            this.SetBit(StatusBits.CF, carry);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private byte RL(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = this.Carry();
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | (carry >> 4));   // CF at Bit4
        }

        private byte RR(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = this.Carry();
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (carry << 3));   // CF at Bit4
        }

        private byte SLA(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)(operand << 1);
        }

        private byte SRA(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (operand & (byte)Bits.Bit7));
        }

        private byte Swap(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.CF);
            return (byte)(PromoteNibble(operand) | DemoteNibble(operand));
        }

        private byte SRL(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) & ~(byte)Bits.Bit7);
        }

        private void Bit(int n, byte operand)
        {
            var carry = this.Carry();
            this.SetBit(StatusBits.HC);
            this.ClearBit(StatusBits.CF | StatusBits.NF);
            this.AdjustZero((byte)(operand & Bit(n)));
            this.SetBit(StatusBits.CF, carry);
        }

        private void DAA()
        {
            int updated = this.A;

            if (this.Subtracting() != 0)
            {
                if (this.HalfCarry() != 0)
                {
                    updated = LowByte(updated - 6);
                }

                if (this.Carry() != 0)
                {
                    updated -= 0x60;
                }
            }
            else
            {
                if (this.HalfCarry() != 0 || LowNibble((byte)updated) > 9)
                {
                    updated += 6;
                }

                if (this.Carry() != 0 || updated > 0x9F)
                {
                    updated += 0x60;
                }
            }

            this.ClearBit(StatusBits.HC | StatusBits.ZF);
            this.SetBit(StatusBits.CF, this.Carry() != 0 || (updated & (int)Bits.Bit8) != 0);
            this.A = LowByte(updated);

            this.AdjustZero(this.A);
        }

        protected override void CPL()
        {
            base.CPL();
            this.SetBit(StatusBits.HC | StatusBits.NF);
        }

        private void SCF()
        {
            this.SetBit(StatusBits.CF);
            this.ClearBit(StatusBits.HC | StatusBits.NF);
        }

        private void CCF()
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.ClearBit(StatusBits.CF, this.Carry());
        }

        private void RetI()
        {
            this.Return();
            this.EnableInterrupts();
        }
    }
}
