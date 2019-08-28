// <copyright file="Intel8080.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Intel8080 : IntelProcessor
    {
        private readonly Register16 af = new Register16();

        private readonly InputOutput ports;

        private bool interruptEnable = false;

        public Intel8080(Bus bus, InputOutput ports)
        : base(bus) => this.ports = ports;

        public event EventHandler<EventArgs> ExecutingInstruction;

        public event EventHandler<EventArgs> ExecutedInstruction;

        public override Register16 AF
        {
            get
            {
                this.af.Low = (byte)((this.af.Low | (byte)Bits.Bit1) & (int)~(Bits.Bit5 | Bits.Bit3));
                return this.af;
            }
        }

        public override Register16 BC { get; } = new Register16((int)Mask.Mask16);

        public override Register16 DE { get; } = new Register16((int)Mask.Mask16);

        public override Register16 HL { get; } = new Register16((int)Mask.Mask16);

        public override int Execute()
        {
            var decoded = this.GetDecodedOpCode(this.OpCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            this.Execute(x, y, z, p, q);

            return this.Cycles;
        }

        public override int Step()
        {
            this.ResetCycles();
            this.OnExecutingInstruction();
            if (this.Powered)
            {
                if (this.RESET.Lowered())
                {
                    this.HandleRESET();
                }
                else if (this.INT.Lowered())
                {
                    this.HandleINT();
                }
                else if (this.Halted)
                {
                    this.Execute(0); // NOP
                }
                else
                {
                    this.Execute(this.FetchByte());
                }
            }

            this.OnExecutedInstruction();
            return this.Cycles;
        }

        protected virtual void OnExecutingInstruction() => this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnExecutedInstruction() => this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DisableInterrupts();
            this.Tick(3);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.RaiseHALT();
            if (this.interruptEnable)
            {
                this.DisableInterrupts();
                this.Execute(this.Bus.Data);
                this.Tick(3);
            }
        }

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private static byte AdjustSign(byte input, byte value) => SetBit(input, StatusBits.SF, value & (byte)StatusBits.SF);

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

        private void DisableInterrupts() => this.interruptEnable = false;

        private void EnableInterrupts() => this.interruptEnable = true;

        private byte R(int r)
        {
            switch (r)
            {
                case 0:
                    return this.B;
                case 1:
                    return this.C;
                case 2:
                    return this.D;
                case 3:
                    return this.E;
                case 4:
                    return this.H;
                case 5:
                    return this.L;
                case 6:
                    return this.BusRead(this.HL.Word);
                case 7:
                    return this.A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

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
                    this.BusWrite(this.HL.Word, value);
                    break;
                case 7:
                    this.A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private Register16 RP(int rp)
        {
            switch (rp)
            {
                case 0:
                    return this.BC;
                case 1:
                    return this.DE;
                case 2:
                    return this.HL;
                case 3:
                    return this.SP;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rp));
            }
        }

        private Register16 RP2(int rp)
        {
            switch (rp)
            {
                case 0:
                    return this.BC;
                case 1:
                    return this.DE;
                case 2:
                    return this.HL;
                case 3:
                    return this.AF;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rp));
            }
        }

        private void Execute(int x, int y, int z, int p, int q)
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
                                    this.Tick(4);
                                    break;
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.RP(p).Word = this.FetchWord().Word;
                                    this.Tick(10);
                                    break;
                                case 1: // ADD HL,rp
                                    this.Add(this.RP(p));
                                    this.Tick(11);
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
                                            this.BusWrite(this.BC, this.A);
                                            this.Tick(7);
                                            break;
                                        case 1: // LD (DE),A
                                            this.BusWrite(this.DE, this.A);
                                            this.Tick(7);
                                            break;
                                        case 2: // LD (nn),HL
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.SetWord(this.HL);
                                            this.Tick(16);
                                            break;
                                        case 3: // LD (nn),A
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.BusWrite(this.A);
                                            this.Tick(13);
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            this.A = this.BusRead(this.BC);
                                            this.Tick(7);
                                            break;
                                        case 1: // LD A,(DE)
                                            this.A = this.BusRead(this.DE);
                                            this.Tick(7);
                                            break;
                                        case 2: // LD HL,(nn)
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.HL.Word = this.GetWord().Word;
                                            this.Tick(16);
                                            break;
                                        case 3: // LD A,(nn)
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.A = this.BusRead();
                                            this.Tick(13);
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
                            switch (q)
                            {
                                case 0: // INC rp
                                    ++this.RP(p).Word;
                                    break;
                                case 1: // DEC rp
                                    --this.RP(p).Word;
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(6);
                            break;
                        case 4: // 8-bit INC
                            this.R(y, this.Increment(this.R(y)));
                            this.Tick(4);
                            break;
                        case 5: // 8-bit DEC
                            this.R(y, this.Decrement(this.R(y)));
                            this.Tick(4);
                            if (y == 6)
                            {
                                this.Tick(7);
                            }

                            break;
                        case 6: // 8-bit load immediate
                            this.R(y, this.FetchByte());
                            this.Tick(7);
                            if (y == 6)
                            {
                                this.Tick(3);
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

                            this.Tick(4);
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 1: // 8-bit loading
                    if (z == 6 && y == 6)
                    {
                        this.Halt(); // Exception (replaces LD (HL), (HL))
                    }
                    else
                    {
                        this.R(y, this.R(z));
                        if ((y == 6) || (z == 6))
                        {
                            this.Tick(3); // M operations
                        }
                    }

                    this.Tick(4);
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

                    this.Tick(4);
                    if (z == 6)
                    {
                        this.Tick(3);
                    }

                    break;
                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            if (this.ReturnConditionalFlag(y))
                            {
                                this.Tick(6);
                            }

                            this.Tick(5);
                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    this.RP2(p).Word = this.PopWord().Word;
                                    this.Tick(10);
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            this.Return();
                                            this.Tick(10);
                                            break;
                                        case 2: // JP HL
                                            this.Jump(this.HL.Word);
                                            this.Tick(4);
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Word = this.HL.Word;
                                            this.Tick(4);
                                            break;
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 2: // Conditional jump
                            this.JumpConditionalFlag(y);
                            this.Tick(10);
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    this.Jump(this.FetchWord().Word);
                                    this.Tick(10);
                                    break;
                                case 2: // OUT (n),A
                                    this.WritePort(this.FetchByte());
                                    this.Tick(11);
                                    break;
                                case 3: // IN A,(n)
                                    this.A = this.ReadPort(this.FetchByte());
                                    this.Tick(11);
                                    break;
                                case 4: // EX (SP),HL
                                    this.XHTL(this.HL);
                                    this.Tick(19);
                                    break;
                                case 5: // EX DE,HL
                                    (this.DE.Word, this.HL.Word) = (this.HL.Word, this.DE.Word);
                                    this.Tick(4);
                                    break;
                                case 6: // DI
                                    this.DisableInterrupts();
                                    this.Tick(4);
                                    break;
                                case 7: // EI
                                    this.EnableInterrupts();
                                    this.Tick(4);
                                    break;
                            }

                            break;
                        case 4: // Conditional call: CALL cc[y], nn
                            if (this.CallConditionalFlag(y))
                            {
                                this.Tick(7);
                            }

                            this.Tick(10);
                            break;
                        case 5: // PUSH & various ops
                            switch (q)
                            {
                                case 0: // PUSH rp2[p]
                                    this.PushWord(this.RP2(p));
                                    this.Tick(11);
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            this.Call(this.FetchWord().Word);
                                            this.Tick(17);
                                            break;
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 6: // Operate on accumulator and immediate operand: alu[y] n
                            switch (y)
                            {
                                case 0: // ADD A,n
                                    this.Add(this.FetchByte());
                                    break;
                                case 1: // ADC A,n
                                    this.ADC(this.FetchByte());
                                    break;
                                case 2: // SUB n
                                    this.SUB(this.FetchByte());
                                    break;
                                case 3: // SBC A,n
                                    this.SBB(this.FetchByte());
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
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(7);
                            break;
                        case 7: // Restart: RST y * 8
                            this.Restart((byte)(y << 3));
                            this.Tick(11);
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

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
            this.F = SetBit(this.F, StatusBits.AC, LowNibble(operand) != (byte)Mask.Mask4);
            return operand;
        }

        private bool JumpConditionalFlag(int flag)
        {
            switch (flag)
            {
                case 0: // NZ
                    return this.JumpConditional((this.F & (byte)StatusBits.ZF) == 0);
                case 1: // Z
                    return this.JumpConditional((this.F & (byte)StatusBits.ZF) != 0);
                case 2: // NC
                    return this.JumpConditional((this.F & (byte)StatusBits.CF) == 0);
                case 3: // C
                    return this.JumpConditional((this.F & (byte)StatusBits.CF) != 0);
                case 4: // PO
                    return this.JumpConditional((this.F & (byte)StatusBits.PF) == 0);
                case 5: // PE
                    return this.JumpConditional((this.F & (byte)StatusBits.PF) != 0);
                case 6: // P
                    return this.JumpConditional((this.F & (byte)StatusBits.SF) == 0);
                case 7: // M
                    return this.JumpConditional((this.F & (byte)StatusBits.SF) != 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
            }
        }

        private bool ReturnConditionalFlag(int flag)
        {
            switch (flag)
            {
                case 0: // NZ
                    return this.ReturnConditional((this.F & (byte)StatusBits.ZF) == 0);
                case 1: // Z
                    return this.ReturnConditional((this.F & (byte)StatusBits.ZF) != 0);
                case 2: // NC
                    return this.ReturnConditional((this.F & (byte)StatusBits.CF) == 0);
                case 3: // C
                    return this.ReturnConditional((this.F & (byte)StatusBits.CF) != 0);
                case 4: // PO
                    return this.ReturnConditional((this.F & (byte)StatusBits.PF) == 0);
                case 5: // PE
                    return this.ReturnConditional((this.F & (byte)StatusBits.PF) != 0);
                case 6: // P
                    return this.ReturnConditional((this.F & (byte)StatusBits.SF) == 0);
                case 7: // M
                    return this.ReturnConditional((this.F & (byte)StatusBits.SF) != 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
            }
        }

        private bool CallConditionalFlag(int flag)
        {
            switch (flag)
            {
                case 0: // NZ
                    return this.CallConditional((this.F & (byte)StatusBits.ZF) == 0);
                case 1: // Z
                    return this.CallConditional((this.F & (byte)StatusBits.ZF) != 0);
                case 2: // NC
                    return this.CallConditional((this.F & (byte)StatusBits.CF) == 0);
                case 3: // C
                    return this.CallConditional((this.F & (byte)StatusBits.CF) != 0);
                case 4: // PO
                    return this.CallConditional((this.F & (byte)StatusBits.PF) == 0);
                case 5: // PE
                    return this.CallConditional((this.F & (byte)StatusBits.PF) != 0);
                case 6: // P
                    return this.CallConditional((this.F & (byte)StatusBits.SF) == 0);
                case 7: // M
                    return this.CallConditional((this.F & (byte)StatusBits.SF) != 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
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

            this.F = SetBit(this.F, StatusBits.CF, this.MEMPTR.High & (byte)StatusBits.CF);
            this.F = AdjustSZP(this.F, this.A);
        }

        private void ADC(byte value) => this.Add(value, this.F & (byte)StatusBits.CF);

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.MEMPTR.Word = (ushort)(operand - value - carry);

            this.F = AdjustAuxiliaryCarrySub(this.F, operand, value, this.MEMPTR.Word);

            var result = this.MEMPTR.Low;

            this.F = SetBit(this.F, StatusBits.CF, this.MEMPTR.High & (byte)StatusBits.CF);
            this.F = AdjustSZP(this.F, result);

            return result;
        }

        private void SUB(byte value, int carry = 0) => this.A = this.Subtract(this.A, value, carry);

        private void SBB(byte value) => this.SUB(value, this.F & (byte)StatusBits.CF);

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
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | carry);
        }

        private byte RR(byte operand)
        {
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            return (byte)((operand >> 1) | (carry << 7));
        }

        private void DAA()
        {
            var before = this.A;
            var carry = (this.F & (byte)StatusBits.CF) != 0;
            byte addition = 0;
            if (((this.F & (byte)StatusBits.AC) != 0) || (LowNibble(before) > (byte)9))
            {
                addition = 0x6;
            }

            if (((this.F & (byte)StatusBits.CF) != 0) || HighNibble(before) > 9 || (HighNibble(before) >= 9 && LowNibble(before) > 9))
            {
                addition |= 0x60;
                carry = true;
            }

            this.Add(addition);
            this.F = SetBit(this.F, StatusBits.CF, carry);
        }

        private void CMA() => this.A = (byte)~this.A;

        private void STC() => this.F = SetBit(this.F, StatusBits.CF);

        private void CMC() => this.F = ClearBit(this.F, StatusBits.CF, this.F & (byte)StatusBits.CF);

        private void XHTL(Register16 exchange)
        {
            this.MEMPTR.Low = this.BusRead(this.SP.Word);
            ++this.Bus.Address.Word;
            this.MEMPTR.High = this.BusRead();
            this.BusWrite(exchange.High);
            exchange.High = this.MEMPTR.High;
            --this.Bus.Address.Word;
            this.BusWrite(exchange.Low);
            exchange.Low = this.MEMPTR.Low;
        }

        private void WritePort(byte port)
        {
            this.Bus.Address.Word = new Register16(port, this.A).Word;
            this.Bus.Data = this.A;
            this.WritePort();
        }

        private void WritePort() => this.ports.Write(this.Bus.Address.Low, this.Bus.Data);

        private byte ReadPort(byte port)
        {
            this.Bus.Address.Word = new Register16(port, this.A).Word;
            return this.ReadPort();
        }

        private byte ReadPort() => this.Bus.Data = this.ports.Read(this.Bus.Address.Low);
    }
}