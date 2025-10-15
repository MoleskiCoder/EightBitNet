// <copyright file="Intel8080.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>


namespace Intel8080
{
    using EightBit;
    using System;
    using System.Diagnostics;

    public class Intel8080(Bus bus, InputOutput ports) : IntelProcessor(bus)
    {
        private readonly Register16 af = new();

        private readonly InputOutput ports = ports;

        private bool interruptEnable;

        public override Register16 AF
        {
            get
            {
                this.af.Low = (byte)((this.af.Low | (byte)Bits.Bit1) & (int)~(Bits.Bit5 | Bits.Bit3));
                return this.af;
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
            if (this.RESET.Lowered())
            {
                this.HandleRESET();
            }
            else if (this.INT.Lowered())
            {
                if (this.interruptEnable)
                {
                    this.HandleINT();
                }
            }
            else
            {
                this.Execute(this.FetchInstruction());
            }
        }

        private void MemoryUpdate(int ticks)
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
            this.MemoryUpdate(1);
        }

        protected override byte MemoryRead()
        {
            this.OnReadingMemory();
            this.Tick(2);
            base.MemoryRead();
            this.OnReadMemory();
            this.Tick(2);
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
            this.Tick(4);
            return this.Bus.Data;
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

        protected override void HandleINT()
        {
            base.HandleINT();
            var data = this.ReadDataUnderInterrupt();
            this.Tick();
            this.Execute(data);
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

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

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

        private static byte AdjustAuxiliaryCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.AC, CalculateHalfCarryAdd(before, value, calculation));

        private static byte AdjustAuxiliaryCarrySub(byte input, byte before, byte value, int calculation) => ClearBit(input, StatusBits.AC, CalculateHalfCarrySub(before, value, calculation));

        protected override void DisableInterrupts() => this.interruptEnable = false;

        protected override void EnableInterrupts() => this.interruptEnable = true;

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
                                    this.RP(p).Assign(this.FetchWord());
                                    break;
                                case 1: // ADD HL,rp
                                    this.Add(this.RP(p));
                                    this.Tick(7);
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
                                            this.WriteMemoryIndirect(this.BC, this.A);
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
                            this.Tick(2);
                            break;
                        case 4: // 8-bit INC
                            this.R(y, this.Increment(this.R(y)), 2);
                            break;
                        case 5: // 8-bit DEC
                            this.R(y, this.Decrement(this.R(y)), 2);
                            break;
                        case 6: // 8-bit load immediate
                            {
                                _ = this.FetchByte();  // LD r,n
                                if (memoryY)
                                {
                                    this.Tick(2);
                                }
                                this.R(y, this.Bus.Data);
                            }
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
                                    this.CMA();
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
                    }
                    break;
                case 2: // Operate on accumulator and register/memory location
                    switch (y)
                    {
                        case 0: // ADD A,r
                            this.Add(this.R(z));
                            break;
                        case 1: // ADC A,r
                            this.ADC(this.R(z));
                            break;
                        case 2: // SUB r
                            this.SUB(this.R(z));
                            break;
                        case 3: // SBC A,r
                            this.SBB(this.R(z));
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
                            throw new NotSupportedException("Invalid operation mode");
                    }
                    break;
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
                                    this.A = this.ReadPort(this.FetchByte());
                                    break;
                                case 4: // EX (SP),HL
                                    this.XHTL(this.HL);
                                    break;
                                case 5: // EX DE,HL
                                    (this.HL.Low, this.DE.Low) = (this.DE.Low, this.HL.Low);
                                    (this.HL.High, this.DE.High) = (this.DE.High, this.HL.High);
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
                        case 6: // Operate on accumulator and immediate operand: alu[y] n
                            _ = this.FetchByte();
                            switch (y)
                            {
                                case 0: // ADD A,n
                                    this.Add(this.Bus.Data);
                                    break;
                                case 1: // ADC A,n
                                    this.ADC(this.Bus.Data);
                                    break;
                                case 2: // SUB n
                                    this.SUB(this.Bus.Data);
                                    break;
                                case 3: // SBC A,n
                                    this.SBB(this.Bus.Data);
                                    break;
                                case 4: // AND n
                                    this.AndR(this.Bus.Data);
                                    break;
                                case 5: // XOR n
                                    this.XorR(this.Bus.Data);
                                    break;
                                case 6: // OR n
                                    this.OrR(this.Bus.Data);
                                    break;
                                case 7: // CP n
                                    this.Compare(this.Bus.Data);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }
                            break;
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
            this.F = AdjustSZP(this.F, ++operand);
            this.F = ClearBit(this.F, StatusBits.AC, LowNibble(operand));
            return operand;
        }

        private byte Decrement(byte operand)
        {
            this.F = AdjustSZP(this.F, --operand);
            this.F = SetBit(this.F, StatusBits.AC, LowNibble(operand) != (byte)Mask.Four);
            return operand;
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

        protected sealed override void ReturnConditionalFlag(int flag)
        {
            var condition = this.ConvertCondition(flag);
            this.Tick();
            if (condition)
            {
                this.Return();
            }
        }

        private void Add(Register16 value)
        {
            var result = this.HL.Word + value.Word;
            this.HL.Word = (ushort)result;
            this.F = SetBit(this.F, StatusBits.CF, result & (int)Bits.Bit16);
        }

        private void Add(byte value, int carry = 0)
        {
            this.MEMPTR.Word = (ushort)(this.A + value + carry);

            this.F = AdjustAuxiliaryCarryAdd(this.F, this.A, value, this.MEMPTR.Low);

            this.A = this.MEMPTR.Low;

            this.F = SetBit(this.F, StatusBits.CF, CarryTest(this.MEMPTR.High));
            this.F = AdjustSZP(this.F, this.A);
        }

        private void ADC(byte value) => this.Add(value, this.Carry());

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.MEMPTR.Word = (ushort)(operand - value - carry);

            this.F = AdjustAuxiliaryCarrySub(this.F, operand, value, this.MEMPTR.Word);

            var result = this.MEMPTR.Low;

            this.F = SetBit(this.F, StatusBits.CF, CarryTest(this.MEMPTR.High));
            this.F = AdjustSZP(this.F, result);

            return result;
        }

        private void SUB(byte value, int carry = 0) => this.A = this.Subtract(this.A, value, carry);

        private void SBB(byte value) => this.SUB(value, this.Carry());

        private void AndR(byte value)
        {
            this.F = SetBit(this.F, StatusBits.AC, (this.A | value) & (int)Bits.Bit3);
            this.F = ClearBit(this.F, StatusBits.CF);
            this.F = AdjustSZP(this.F, this.A &= value);
        }

        private void XorR(byte value)
        {
            this.F = ClearBit(this.F, StatusBits.AC | StatusBits.CF);
            this.F = AdjustSZP(this.F, this.A ^= value);
        }

        private void OrR(byte value)
        {
            this.F = ClearBit(this.F, StatusBits.AC | StatusBits.CF);
            this.F = AdjustSZP(this.F, this.A |= value);
        }

        private void Compare(byte value) => this.Subtract(this.A, value);

        private byte RLC(byte operand)
        {
            var carry = operand & (byte)Bits.Bit7;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            return (byte)((operand << 1) | (carry >> 7));
        }

        private byte RRC(byte operand)
        {
            var carry = operand & (byte)Bits.Bit0;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private byte RL(byte operand)
        {
            var carry = this.Carry();
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | carry);
        }

        private byte RR(byte operand)
        {
            var carry = this.Carry();
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
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

            this.Add(addition);
            this.F = SetBit(this.F, StatusBits.CF, carry);
        }

        private void CMA() => this.A = (byte)~this.A;

        private void STC() => this.F = SetBit(this.F, StatusBits.CF);

        private void CMC() => this.F = ClearBit(this.F, StatusBits.CF, this.Carry());

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
            this.MemoryUpdate(1);
            this.Tick(2);
        }

        private void WritePort(byte port)
        {
            this.Bus.Address.Assign(port, this.A);
            this.Bus.Data = this.A;
            this.WritePort();
        }

        private void WritePort()
        {
            this.Tick(3);
            this.ports.Write(this.Bus.Address, this.Bus.Data);
            this.Tick(1);
        }

        private byte ReadPort(byte port)
        {
            this.Bus.Address.Assign(port, this.A);
            return this.ReadPort();
        }

        private byte ReadPort()
        {
            this.Tick(2);
            this.Bus.Data = this.ports.Read(this.Bus.Address);
            this.Tick(2);
            return this.Bus.Data;
        }
    }
}