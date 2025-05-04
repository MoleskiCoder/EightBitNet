// <copyright file="LR35902.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    public sealed class LR35902 : IntelProcessor
    {
        public LR35902(Bus bus)
        : base(bus)
        {
            this.bus = bus;
            this.RaisedPOWER += this.LR35902_RaisedPOWER;
        }

        private void LR35902_RaisedPOWER(object? sender, EventArgs e)
        {
            this.RaiseWR();
            this.RaiseRD();
            this.RaiseMWR();
        }

        private readonly Bus bus;
        private readonly Register16 af = new((int)Mask.Sixteen);
        private bool prefixCB;

        public int MachineCycles => this.Cycles / 4;

        public override Register16 AF
        {
            get
            {
                this.af.Low = (byte)HigherNibble(this.af.Low);
                return this.af;
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
            get => this.bus.IO.Peek(IoRegisters.IF);
            set => this.bus.IO.Poke(IoRegisters.IF, value);
        }

        public byte MaskedInterrupts => (byte)(this.IE & this.IF);

        private bool Stopped { get; set; }

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

            if (this.prefixCB)
            {
                this.ExecuteCB(x, y, z);
            }
            else
            {
                this.ExecuteOther(x, y, z, p, q);
            }

            System.Diagnostics.Debug.Assert(this.Cycles > 0, $"No timing associated with instruction (CB prefixed? {this.prefixCB}) 0x{this.OpCode:X2}");
        }

        public override void PoweredStep()
        {
            this.prefixCB = false;

            if (this.MaskedInterrupts != 0)
            {
                if (this.IME)
                {
                    this.IF = 0;
                    this.LowerINT();
                    var index = FindFirstSet(this.MaskedInterrupts);
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
            else if (this.HALT.Lowered())
            {
                _ = this.FetchByte();
                this.Execute(0);  // NOP
            }
            else
            {
                this.Execute(this.FetchByte());
            }
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DI();
            this.SP.Word = (ushort)(Mask.Sixteen - 1);
            this.TickMachine(4);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.RaiseHALT();
            this.DI();
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

        private int Zero()
        {
            return ZeroTest(this.F);
        }

        private int Carry()
        {
            return CarryTest(this.F);
        }

        private int HalfCarry()
        {
            return HalfCarryTest(this.F);
        }

        private int Subtracting()
        {
            return SubtractingTest(this.F);
        }

        private static int ZeroTest(byte data)
        {
            return data & (byte)StatusBits.ZF;
        }

        private static int CarryTest(byte data)
        {
            return data & (byte)StatusBits.CF;
        }

        private static int HalfCarryTest(byte data)
        {
            return data & (byte)StatusBits.HC;
        }

        private static int SubtractingTest(byte data)
        {
            return data & (byte)StatusBits.NF;
        }

        protected override void MemoryWrite()
        {
            this.LowerMWR();
            this.LowerWR();
            base.MemoryWrite();
            this.TickMachine();
            this.RaiseWR();
            this.RaiseMWR();
        }

        protected override byte MemoryRead()
        {
            this.LowerMWR();
            this.LowerRD();
            var returned = base.MemoryRead();
            this.TickMachine();
            this.RaiseRD();
            this.RaiseMWR();
            return returned;
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

        protected override bool JumpConditional(bool condition)
        {
            if (base.JumpConditional(condition))
            {
                this.TickMachine();
            }
            return condition;
        }

        protected override bool ReturnConditional(bool condition)
        {
            this.TickMachine();
            return base.ReturnConditional(condition);
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

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private static byte AdjustZero(byte input, byte value) => ClearBit(input, StatusBits.ZF, value);

        private static byte AdjustHalfCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarryAdd(before, value, calculation));

        private static byte AdjustHalfCarrySub(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarrySub(before, value, calculation));

        private static byte Res(int n, byte operand) => ClearBit(operand, Bit(n));

        private static byte Set(int n, byte operand) => SetBit(operand, Bit(n));

        private void DI() => this.IME = false;

        private void EI() => this.IME = true;

        private void Stop() => this.Stopped = true;

        private void Start() => this.Stopped = false;

        private byte R(int r) => r switch
        {
            0 => this.B,
            1 => this.C,
            2 => this.D,
            3 => this.E,
            4 => this.H,
            5 => this.L,
            6 => this.MemoryRead(this.HL),
            7 => this.A,
            _ => throw new ArgumentOutOfRangeException(nameof(r)),
        };

        private void R(int r, byte value)
        {
            switch (r)
            {
                case 0:
                    this.B = value;
                    break;
                case 1:
                    this.C = value;
                    break;
                case 2:
                    this.D = value;
                    break;
                case 3:
                    this.E = value;
                    break;
                case 4:
                    this.H = value;
                    break;
                case 5:
                    this.L = value;
                    break;
                case 6:
                    this.MemoryWrite(this.HL, value);
                    break;
                case 7:
                    this.A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
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
            switch (x)
            {
                case 0: // rot[y] r[z]
                    {
                        var operand = this.R(z);
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
                        this.R(z, operand);
                        this.F = AdjustZero(this.F, operand);
                    }
                    break;

                case 1: // BIT y, r[z]
                    this.Bit(y, this.R(z));
                    break;

                case 2: // RES y, r[z]
                    this.R(z, Res(y, this.R(z)));
                    break;

                case 3: // SET y, r[z]
                    this.R(z, Set(y, this.R(z)));
                    break;

                default:
                    throw new InvalidOperationException("Unreachable code block reached");
            }
        }

        private void ExecuteOther(int x, int y, int z, int p, int q)
        {
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
                                    this.Bus.Address.Assign(this.FetchWord());
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
                                    _ = this.JumpRelativeConditionalFlag(y - 4);
                                    break;
                                default:
                                    throw new InvalidOperationException("Unreachable code block reached");
                            }

                            break;

                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.RP(p).Assign(this.FetchWord());
                                    break;

                                case 1: // ADD HL,rp
                                    this.Add(this.HL, this.RP(p));
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
                                            this.MemoryWrite(this.BC, this.A);
                                            break;

                                        case 1: // LD (DE),A
                                            this.MemoryWrite(this.DE, this.A);
                                            break;

                                        case 2: // GB: LDI (HL),A
                                            this.MemoryWrite(this.HL.Word++, this.A);
                                            break;

                                        case 3: // GB: LDD (HL),A
                                            this.MemoryWrite(this.HL.Word--, this.A);
                                            break;

                                        default:
                                            throw new InvalidOperationException("Invalid operation mode");
                                    }

                                    break;

                                case 1:
                                    this.A = p switch
                                    {
                                        0 => this.MemoryRead(this.BC),          // LD A,(BC)
                                        1 => this.MemoryRead(this.DE),          // LD A,(DE)
                                        2 => this.MemoryRead(this.HL.Word++),   // GB: LDI A,(HL)
                                        3 => this.MemoryRead(this.HL.Word--),   // GB: LDD A,(HL)
                                        _ => throw new InvalidOperationException("Invalid operation mode"),
                                    };
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;

                        case 3: // 16-bit INC/DEC
                            switch (q)
                            {
                                case 0: // INC rp
                                    ++this.RP(p).Word;
                                    break;

                                case 1: // DEC rp
                                    --this.RP(p).Word;
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
                                    this.Cpl();
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
                    if (z == 6 && y == 6)
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                        this.TickMachine(2);
                    }
                    else
                    {
                        this.R(y, this.R(z));
                    }
                    break;

                case 2: // Operate on accumulator and register/memory location
                    switch (y)
                    {
                        case 0: // ADD A,r
                            this.A = this.Add(this.A, this.R(z));
                            break;
                        case 1: // ADC A,r
                            this.A = this.ADC(this.A, this.R(z));
                            break;
                        case 2: // SUB r
                            this.A = this.Subtract(this.A, this.R(z));
                            break;
                        case 3: // SBC A,r
                            this.A = this.SBC(this.A, this.R(z));
                            break;
                        case 4: // AND r
                            this.AndR(this.R(z));
                            break;
                        case 5: // XOR r
                            this.XorR(this.R(z));
                            break;
                        case 6: // OR r
                            this.OrR(this.R(z));
                            break;
                        case 7: // CP r
                            this.Compare(this.R(z));
                            break;
                        default:
                            throw new InvalidOperationException("Invalid operation mode");
                    }
                    break;
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
                                    _ = this.ReturnConditionalFlag(y);
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
                                        this.F = ClearBit(this.F, StatusBits.ZF | StatusBits.NF);
                                        this.F = SetBit(this.F, StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.F = SetBit(this.F, StatusBits.HC, carried & (int)Bits.Bit4);
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
                                        this.F = ClearBit(this.F, StatusBits.ZF | StatusBits.NF);
                                        this.F = SetBit(this.F, StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.F = SetBit(this.F, StatusBits.HC, carried & (int)Bits.Bit4);
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
                                    this.RP2(p).Assign(this.PopWord());
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
                                    _ = this.JumpConditionalFlag(y);
                                    break;
                                case 4: // GB: LD (FF00 + C),A
                                    this.MemoryWrite(this.C, IoRegisters.BasePage, this.A);
                                    break;
                                case 5: // GB: LD (nn),A
                                    this.MEMPTR.Assign(this.FetchWord());
                                    this.MemoryWrite(this.MEMPTR, this.A);
                                    break;
                                case 6: // GB: LD A,(FF00 + C)
                                    this.A = this.MemoryRead(this.C, IoRegisters.BasePage);
                                    break;
                                case 7: // GB: LD A,(nn)
                                    this.MEMPTR.Assign(this.FetchWord());
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
                                    this.prefixCB = true;
                                    this.Execute(this.FetchByte());
                                    break;
                                case 6: // DI
                                    this.DI();
                                    break;
                                case 7: // EI
                                    this.EI();
                                    break;
                                default:
                                    break;
                            }

                            break;

                        case 4: // Conditional call: CALL cc[y], nn
                            _ = this.CallConditionalFlag(y);
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

                        case 6: // Operate on accumulator and immediate operand: alu[y] n
                            switch (y)
                            {
                                case 0: // ADD A,n
                                    this.A = this.Add(this.A, this.FetchByte());
                                    break;
                                case 1: // ADC A,n
                                    this.A = this.ADC(this.A, this.FetchByte());
                                    break;
                                case 2: // SUB n
                                    this.A = this.Subtract(this.A, this.FetchByte());
                                    break;
                                case 3: // SBC A,n
                                    this.A = this.SBC(this.A, this.FetchByte());
                                    break;
                                case 4: // AND n
                                    this.AndR(this.FetchByte());
                                    break;
                                case 5: // XOR n
                                    this.XorR(this.FetchByte());
                                    break;
                                case 6: // OR n
                                    this.OrR(this.FetchByte());
                                    break;
                                case 7: // CP n
                                    this.Compare(this.FetchByte());
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }
                            break;

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
            this.F = ClearBit(this.F, StatusBits.NF);
            this.F = AdjustZero(this.F, ++operand);
            this.F = ClearBit(this.F, StatusBits.HC, LowNibble(operand));
            return operand;
        }

        private byte Decrement(byte operand)
        {
            this.F = SetBit(this.F, StatusBits.NF);
            this.F = ClearBit(this.F, StatusBits.HC, LowNibble(operand));
            this.F = AdjustZero(this.F, --operand);
            return operand;
        }

        protected sealed override bool ConvertCondition(int flag) => flag switch
        {
            0 => this.Zero() == 0,  // NZ
            1 => this.Zero() != 0,  // Z
            2 => this.Carry() == 0, // NC
            3 => this.Carry() != 0, // C
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        private void Add(Register16 operand, Register16 value)
        {
            this.TickMachine();

            this.MEMPTR.Assign(operand);

            var result = this.MEMPTR.Word + value.Word;

            operand.Word = (ushort)result;

            this.F = ClearBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, result & (int)Bits.Bit16);
            this.F = AdjustHalfCarryAdd(this.F, this.MEMPTR.High, value.High, operand.High);
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            this.MEMPTR.Word = (ushort)(operand + value + carry);

            this.F = AdjustHalfCarryAdd(this.F, operand, value, this.MEMPTR.Low);

            operand = this.MEMPTR.Low;

            this.F = ClearBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, this.MEMPTR.Word & (ushort)Bits.Bit8);
            this.F = AdjustZero(this.F, operand);

            return operand;
        }

        private byte ADC(byte operand, byte value) => this.Add(operand, value, this.Carry() >> 4);

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.MEMPTR.Word = (ushort)(operand - value - carry);

            this.F = AdjustHalfCarrySub(this.F, operand, value, this.MEMPTR.Low);

            var result = operand = this.MEMPTR.Low;

            this.F = SetBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, this.MEMPTR.High & (byte)Bits.Bit0);
            this.F = AdjustZero(this.F, operand);

            return result;
        }

        private byte SBC(byte operand, byte value) => this.Subtract(operand, value, this.Carry() >> 4);

        private byte AndR(byte operand, byte value)
        {
            this.F = SetBit(this.F, StatusBits.HC);
            this.F = ClearBit(this.F, StatusBits.CF | StatusBits.NF);
            this.F = AdjustZero(this.F, operand &= value);
            return operand;
        }

        private void AndR(byte value) => this.A = this.AndR(this.A, value);

        private byte XorR(byte operand, byte value)
        {
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.F = AdjustZero(this.F, operand ^= value);
            return operand;
        }

        private void XorR(byte value) => this.A = this.XorR(this.A, value);

        private byte OrR(byte operand, byte value)
        {
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.F = AdjustZero(this.F, operand |= value);
            return operand;
        }

        private void OrR(byte value) => this.A = this.OrR(this.A, value);

        private void Compare(byte value) => this.Subtract(this.A, value);

        private byte RLC(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = operand & (byte)Bits.Bit7;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            return (byte)((operand << 1) | (carry >> 7));
        }

        private byte RRC(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = operand & (byte)Bits.Bit0;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private byte RL(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = this.Carry();
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | (carry >> 4));   // CF at Bit4
        }

        private byte RR(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = this.Carry();
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (carry << 3));   // CF at Bit4
        }

        private byte SLA(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)(operand << 1);
        }

        private byte SRA(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (operand & (byte)Bits.Bit7));
        }

        private byte Swap(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.CF);
            return (byte)(PromoteNibble(operand) | DemoteNibble(operand));
        }

        private byte SRL(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) & ~(byte)Bits.Bit7);
        }

        private void Bit(int n, byte operand)
        {
            var carry = this.Carry();
            _ = this.AndR(operand, Bit(n));
            this.F = SetBit(this.F, StatusBits.CF, carry);
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

            this.F = ClearBit(this.F, (byte)StatusBits.HC | (byte)StatusBits.ZF);
            this.F = SetBit(this.F, StatusBits.CF, this.Carry() != 0 || (updated & (int)Bits.Bit8) != 0);
            this.A = LowByte(updated);

            this.F = AdjustZero(this.F, this.A);
        }

        private void Cpl()
        {
            this.F = SetBit(this.F, StatusBits.HC | StatusBits.NF);
            this.A = (byte)~this.A;
        }

        private void SCF()
        {
            this.F = SetBit(this.F, StatusBits.CF);
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.NF);
        }

        private void CCF()
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = ClearBit(this.F, StatusBits.CF, this.F & (byte)StatusBits.CF);
        }

        private void RetI()
        {
            this.Return();
            this.EI();
        }
    }
}
