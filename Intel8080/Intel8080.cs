// <copyright file="Intel8080.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>


namespace Intel8080
{
    using EightBit;
    using System;
    using System.Diagnostics;

    public class Intel8080 : IntelProcessor
    {
        public Intel8080(Bus bus, InputOutput ports)
        : base(bus)
        {
            this._ports = ports;
            this.RaisedPOWER += this.Intel8080_RaisedPOWER;
            this.LoweredRESET += this.Intel8080_LoweredRESET;
            this.LoweredINT += this.Intel8080_LoweredINT;
        }

        private bool _interruptPending;
        private bool _resetPending;

        private readonly InputOutput _ports;

        public InputOutput Ports => this._ports;

        private readonly Register16 _af = new();

        private bool _interruptEnable;

        public override Register16 AF
        {
            get
            {
                this._af.Low = (byte)((this._af.Low | (byte)Bits.Bit1) & (int)~(Bits.Bit5 | Bits.Bit3));
                return this._af;
            }
        }

        public override Register16 BC { get; } = new Register16((int)Mask.Sixteen);

        public override Register16 DE { get; } = new Register16((int)Mask.Sixteen);

        public override Register16 HL { get; } = new Register16((int)Mask.Sixteen);

        public override void Execute()
        {
            var decoded = this.GetDecodedOpCode(this.OpCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            this.Execute(x, y, z, p, q);
        }

        public override void PoweredStep()
        {
            if (this._resetPending)
            {
                this._resetPending = false;
                this.HandleRESET();
                return;
            }
            else if (this._interruptPending)
            {
                this._interruptPending = false;
                if (this._interruptEnable)
                {
                    this.HandleINT();
                    return;
                }
            }

            this.Execute(this.FetchInstruction());
        }

        private void Intel8080_RaisedPOWER(object? sender, EventArgs e)
        {
            this.DisableInterrupts();
            this.ResetRegisterSet();
        }

        private void Intel8080_LoweredINT(object? sender, EventArgs e)
        {
            this._interruptPending = true;
        }

        private void Intel8080_LoweredRESET(object? sender, EventArgs e)
        {
            this._resetPending = true;
        }

        private void MemoryUpdate(int ticks = 1)
        {
            Debug.Assert(ticks > 0, "Ticks must be greater than zero");
            this.OnWritingMemory();
            this.Tick(ticks + 1);
            base.MemoryWrite();
            this.Tick();
            this.OnWrittenMemory();
        }

        protected override void MemoryWrite()
        {
            this.MemoryUpdate();
        }

        protected override byte MemoryRead()
        {
            this.OnReadingMemory();
            this.Tick(2);
            base.MemoryRead();
            this.Tick();
            this.OnReadMemory();
            this.Tick();
            return this.Bus.Data;
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DisableInterrupts();
            this.SP.Word = this.AF.Word = (ushort)Mask.Sixteen;
        }

        private byte ReadDataUnderInterrupt()
        {
            this.Tick(5);
            return this.Bus.Data;
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            var data = this.ReadDataUnderInterrupt();
            this.Tick();
            this.Execute(data);
        }

        protected override void Call(Register16 destination)
        {
            this.Tick();
            base.Call(destination);
        }

        protected override void JumpRelative(sbyte offset)
        {
            base.JumpRelative(offset);
            this.Tick(5);
        }

        private int Zero() => ZeroTest(this.F);

        private int Carry() => CarryTest(this.F);

        private int Parity() => ParityTest(this.F);

        private int Sign() => SignTest(this.F);

        private int AuxiliaryCarry() => AuxiliaryCarryTest(this.F);

        private static int ZeroTest(byte data) => data & (byte)StatusBits.ZF;

        private static int CarryTest(byte data) => data & (byte)StatusBits.CF;

        private static int ParityTest(byte data) => data & (byte)StatusBits.PF;

        private static int SignTest(byte data) => data & (byte)StatusBits.SF;

        private static int AuxiliaryCarryTest(byte data) => data & (byte)StatusBits.AC;

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private void SetBit(StatusBits flag) => this.F = SetBit(this.F, flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private void SetBit(StatusBits flag, int condition) => this.F = SetBit(this.F, flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private void SetBit(StatusBits flag, bool condition) => this.F = SetBit(this.F, flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private void ClearBit(StatusBits flag) => this.F = ClearBit(this.F, flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private void ClearBit(StatusBits flag, int condition) => this.F = ClearBit(this.F, flag, condition);

        private static byte AdjustSign(byte input, byte value) => SetBit(input, StatusBits.SF, SignTest(value));

        private static byte AdjustZero(byte input, byte value) => ClearBit(input, StatusBits.ZF, value);

        private static byte AdjustParity(byte input, byte value) => SetBit(input, StatusBits.PF, EvenParity(value));

        private static byte AdjustSZ(byte input, byte value)
        {
            input = AdjustSign(input, value);
            return AdjustZero(input, value);
        }

        private static byte AdjustSZP(byte input, byte value)
        {
            input = AdjustSZ(input, value);
            return AdjustParity(input, value);
        }

        private void AdjustSZP(byte value) => this.F = AdjustSZP(this.F, value);

        private static byte AdjustAuxiliaryCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.AC, CalculateHalfCarryAdd(before, value, calculation));

        private void AdjustAuxiliaryCarryAdd(byte before, byte value, int calculation) => this.F = AdjustAuxiliaryCarryAdd(this.F, before, value, calculation);

        private static byte AdjustAuxiliaryCarrySub(byte input, byte before, byte value, int calculation) => ClearBit(input, StatusBits.AC, CalculateHalfCarrySub(before, value, calculation));

        private void AdjustAuxiliaryCarrySub(byte before, byte value, int calculation) => this.F = AdjustAuxiliaryCarrySub(this.F, before, value, calculation);

        protected override void DisableInterrupts() => this._interruptEnable = false;

        protected override void EnableInterrupts() => this._interruptEnable = true;

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

        private void Execute(int x, int y, int z, int p, int q)
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
                                default:
                                    break;
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.FetchInto(this.RP(p));
                                    break;
                                case 1: // ADD HL,rp
                                    this.HL.Assign(this.Add(this.HL, this.RP(p)));
                                    this.Tick(6);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 2: // Indirect loading
                            switch (q)
                            {
                                case 0:
                                    switch (p)
                                    {
                                        case 0: // LD (BC),A
                                            this.WriteMemoryIndirect(this.BC, this.A);  //xxxx
                                            break;
                                        case 1: // LD (DE),A
                                            this.WriteMemoryIndirect(this.DE, this.A);
                                            break;
                                        case 2: // LD (nn),HL
                                            this.FetchWordAddress();
                                            this.SetWord(this.HL);
                                            break;
                                        case 3: // LD (nn),A
                                            this.FetchInto(this.MEMPTR);
                                            this.WriteMemoryIndirect(this.A);
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            this.A = this.ReadMemoryIndirect(this.BC);
                                            break;
                                        case 1: // LD A,(DE)
                                            this.A = this.ReadMemoryIndirect(this.DE);
                                            break;
                                        case 2: // LD HL,(nn)
                                            this.FetchWordAddress();
                                            this.GetInto(this.HL);
                                            break;
                                        case 3: // LD A,(nn)
                                            this.FetchInto(this.MEMPTR);
                                            this.A = this.ReadMemoryIndirect();
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 3: // 16-bit INC/DEC
                            _ = q switch
                            {
                                // INC rp
                                0 => this.RP(p).Increment(),
                                // DEC rp
                                1 => this.RP(p).Decrement(),
                                _ => throw new NotSupportedException("Invalid operation mode"),
                            };
                            this.Tick();
                            break;
                        case 4: // 8-bit INC
                            this.R(y, this.Increment(this.R(y)), 2);
                            this.Tick();
                            break;
                        case 5: // 8-bit DEC
                            this.R(y, this.Decrement(this.R(y)), 2);
                            this.Tick();
                            break;
                        case 6: // 8-bit load immediate
                            this.R(y, this.FetchByte(), 2);// LD r,n
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
                                    this.STC();
                                    break;
                                case 7:
                                    this.CMC();
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 1: // 8-bit loading
                    if (memoryZ && memoryY)
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                    }
                    else
                    {
                        this.R(y, this.R(z));
                        this.Tick();
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
                                this.A = this.SUB(this.A, value);
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
                                throw new NotSupportedException("Invalid operation mode");
                        }
                        break;
                    }
                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            this.ReturnConditionalFlag(y);
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
                                        case 2: // JP HL
                                            this.Jump(this.HL);
                                            this.Tick();
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Assign(this.HL);
                                            this.Tick(2);
                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 2: // Conditional jump
                            this.JumpConditionalFlag(y);
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    this.JumpIndirect();
                                    break;
                                case 2: // OUT (n),A
                                    this.WritePort(this.FetchByte());
                                    break;
                                case 3: // IN A,(n)
                                    this.ReadPort(this.FetchByte());
                                    this.A = this.Bus.Data;
                                    break;
                                case 4: // EX (SP),HL
                                    this.XHTL(this.HL);
                                    break;
                                case 5: // EX DE,HL
                                    (this.HL.Low, this.DE.Low) = (this.DE.Low, this.HL.Low);
                                    (this.HL.High, this.DE.High) = (this.DE.High, this.HL.High);
                                    this.Tick();
                                    break;
                                case 6: // DI
                                    this.DisableInterrupts();
                                    break;
                                case 7: // EI
                                    this.EnableInterrupts();
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
                                    this.Tick();
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
                                    throw new NotSupportedException("Invalid operation mode");
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
                                        this.A = this.SUB(this.A, operand);
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
                                        throw new NotSupportedException("Invalid operation mode");
                                }
                                break;
                            }
                        case 7: // Restart: RST y * 8
                            this.Restart((byte)(y << 3));
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                default:
                    break;
            }
        }

        private byte Increment(byte operand)
        {
            var result = ++operand;
            AdjustSZP(result);
            ClearBit(StatusBits.AC, LowNibble(result));
            return result;
        }

        private byte Decrement(byte operand)
        {
            var result = --operand;
            AdjustSZP(result);
            SetBit(StatusBits.AC, LowNibble(result) != (byte)Mask.Four);
            return result;
        }

        protected sealed override bool ConvertCondition(int flag) => flag switch
        {
            0 => this.Zero() == 0,
            1 => this.Zero() != 0,
            2 => this.Carry() == 0,
            3 => this.Carry() != 0,
            4 => this.Parity() == 0,
            5 => this.Parity() != 0,
            6 => this.Sign() == 0,
            7 => this.Sign() != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        protected sealed override void CallConditional(bool condition)
        {
            this.FetchInto(this.MEMPTR);
            if (condition)
            {
                this.Call();
            }
            else
            {
                this.Tick();
            }
        }

        protected sealed override void ReturnConditionalFlag(int flag)
        {
            var condition = this.ConvertCondition(flag);
            this.Tick();
            if (condition)
            {
                this.Return();
            }
        }

        private Register16 Add(Register16 operand, Register16 value)
        {
            var addition = operand.Word + value.Word;
            this.Intermediate.Word = (ushort)addition;

            SetBit(StatusBits.CF, addition & (int)Bits.Bit16);

            return this.Intermediate;
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand + value + carry);
            var result = this.Intermediate.Low;

            AdjustAuxiliaryCarryAdd(operand, value, result);

            SetBit(StatusBits.CF, CarryTest(this.Intermediate.High));
            AdjustSZP(result);

            return result;
        }

        private byte ADC(byte operand, byte value) => this.Add(operand, value, this.Carry());

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand - value - carry);
            var result = this.Intermediate.Low;

            AdjustAuxiliaryCarrySub(operand, value, this.Intermediate.Word);

            SetBit(StatusBits.CF, CarryTest(this.Intermediate.High));
            AdjustSZP(result);

            return result;
        }

        private byte SUB(byte operand, byte value, int carry = 0) => this.Subtract(operand, value, carry);

        private byte SBC(byte operand, byte value) => this.SUB(operand, value, this.Carry());

        private void AndR(byte value)
        {
            SetBit(StatusBits.AC, (this.A | value) & (int)Bits.Bit3);
            ClearBit(StatusBits.CF);
            AdjustSZP(this.A &= value);
        }

        private void XorR(byte value)
        {
            ClearBit(StatusBits.AC | StatusBits.CF);
            AdjustSZP(this.A ^= value);
        }

        private void OrR(byte value)
        {
            ClearBit(StatusBits.AC | StatusBits.CF);
            AdjustSZP(this.A |= value);
        }

        private void Compare(byte value) => this.Subtract(this.A, value);

        private byte RLC(byte operand)
        {
            var carry = operand & (byte)Bits.Bit7;
            SetBit(StatusBits.CF, carry);
            return (byte)((operand << 1) | (carry >> 7));
        }

        private byte RRC(byte operand)
        {
            var carry = operand & (byte)Bits.Bit0;
            SetBit(StatusBits.CF, carry);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private byte RL(byte operand)
        {
            var carry = this.Carry();
            SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | carry);
        }

        private byte RR(byte operand)
        {
            var carry = this.Carry();
            SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private void DAA()
        {
            var before = this.A;
            var carry = this.Carry() != 0;
            byte addition = 0;
            if (this.AuxiliaryCarry() != 0 || LowNibble(before  ) > 9)
            {
                addition = 0x6;
            }

            if (this.Carry() != 0 || HighNibble(before) > 9 || (HighNibble(before) >= 9 && LowNibble(before) > 9))
            {
                addition |= 0x60;
                carry = true;
            }

            this.A = this.Add(this.A, addition);
            SetBit(StatusBits.CF, carry);
        }

        private void STC() => SetBit(StatusBits.CF);

        private void CMC() => ClearBit(StatusBits.CF, this.Carry());

        private void XHTL(Register16 exchange)
        {
            this.MEMPTR.Low = this.MemoryRead(this.SP);
            this.Bus.Address.Increment();
            this.MEMPTR.High = this.MemoryRead();
            this.Bus.Data = exchange.High;
            exchange.High = this.MEMPTR.High;
            this.MemoryUpdate(2);
            _ = this.Bus.Address.Decrement();
            this.Bus.Data = exchange.Low;
            exchange.Low = this.MEMPTR.Low;
            this.MemoryUpdate();
            this.Tick(2);
        }

        #region Input/output port control

        private void WritePort(Register16 port, byte data)
        {
            this.Bus.Data = data;
            this.WritePort(port);
        }

        private void WritePort(byte port)
        {
            this.Bus.Address.Assign(port, this.Bus.Data = this.A);
            this.WritePort();
        }

        private void WritePort()
        {
            this.Tick(3);
            this.Ports.Write(this.Bus.Address, this.Bus.Data);
            this.Tick(1);
        }

        private void WritePort(Register16 port)
        {
            this.Bus.Address.Assign(port);
            this.WritePort();
        }

        private void ReadPort(byte port)
        {
            this.Bus.Address.Assign(port, this.A);
            this.ReadPort();
        }

        private void ReadPort()
        {
            this.Tick(2);
            this.Bus.Data = this.Ports.Read(this.Bus.Address);
            this.Tick(2);
        }

        #endregion

    }
}