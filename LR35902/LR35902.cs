// <copyright file="LR35902.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit.GameBoy
{
    using System;

    public class LR35902 : IntelProcessor
    {
        private readonly Bus bus;
        private readonly Register16 af = new Register16((int)Mask.Sixteen);
        private bool prefixCB = false;

        public LR35902(Bus bus)
        : base(bus) => this.bus = bus;

        public event EventHandler<EventArgs> ExecutingInstruction;

        public event EventHandler<EventArgs> ExecutedInstruction;

        public int ClockCycles => this.Cycles * 4;

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

        private bool IME { get; set; } = false;

        private bool Stopped { get; set; } = false;

        public override int Execute()
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
            return this.ClockCycles;
        }

        public override int Step()
        {
            this.OnExecutingInstruction();
            this.prefixCB = false;
            this.ResetCycles();
            if (this.Powered)
            {
                var interruptEnable = this.Bus.Peek(IoRegisters.BASE + IoRegisters.IE);
                var interruptFlags = this.bus.IO.Peek(IoRegisters.IF);

                var masked = interruptEnable & interruptFlags;
                if (masked != 0)
                {
                    if (this.IME)
                    {
                        this.bus.IO.Poke(IoRegisters.IF, 0);
                        this.LowerINT();
                        var index = Chip.FindFirstSet(masked);
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
                    this.Execute(0);  // NOP
                }
                else
                {
                    this.Execute(this.FetchByte());
                }

                this.bus.IO.CheckTimers(this.ClockCycles);
                this.bus.IO.TransferDma();
            }

            this.OnExecutedInstruction();
            return this.ClockCycles;
        }

        protected virtual void OnExecutingInstruction() => this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnExecutedInstruction() => this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DI();
            this.SP.Word = (ushort)(Mask.Sixteen - 1);
            this.Tick(4);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.RaiseHALT();
            this.DI();
            this.Restart(this.Bus.Data);
            this.Tick(4);
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

        private void ExecuteCB(int x, int y, int z)
        {
            switch (x)
            {
                case 0: // rot[y] r[z]
                {
                    var operand = this.R(z);
                    switch (y)
                    {
                        case 0:
                            operand = this.RLC(operand);
                            break;
                        case 1:
                            operand = this.RRC(operand);
                            break;
                        case 2:
                            operand = this.RL(operand);
                            break;
                        case 3:
                            operand = this.RR(operand);
                            break;
                        case 4:
                            operand = this.SLA(operand);
                            break;
                        case 5:
                            operand = this.SRA(operand);
                            break;
                        case 6: // GB: SWAP r
                            operand = this.Swap(operand);
                            break;
                        case 7:
                            operand = this.SRL(operand);
                            break;
                        default:
                            throw new InvalidOperationException("Unreachable code block reached");
                    }

                    this.Tick(2);
                    this.R(z, operand);
                    this.F = AdjustZero(this.F, operand);
                    if (z == 6)
                    {
                        this.Tick(2);
                    }

                    break;
                }

                case 1: // BIT y, r[z]
                    this.Bit(y, this.R(z));
                    this.Tick(2);
                    if (z == 6)
                    {
                        this.Tick(2);
                    }

                    break;

                case 2: // RES y, r[z]
                    this.R(z, Res(y, this.R(z)));
                    this.Tick(2);
                    if (z == 6)
                    {
                        this.Tick(2);
                    }

                    break;

                case 3: // SET y, r[z]
                    this.R(z, Set(y, this.R(z)));
                    this.Tick(2);
                    if (z == 6)
                    {
                        this.Tick(2);
                    }

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
                                    this.Tick();
                                    break;
                                case 1: // GB: LD (nn),SP
                                    this.Bus.Address.Word = this.FetchWord().Word;
                                    this.SetWord(this.SP);
                                    this.Tick(5);
                                    break;
                                case 2: // GB: STOP
                                    this.Stop();
                                    this.Tick();
                                    break;
                                case 3: // JR d
                                    this.JumpRelative((sbyte)this.FetchByte());
                                    this.Tick(3);
                                    break;
                                case 4: // JR cc,d
                                case 5:
                                case 6:
                                case 7:
                                    if (this.JumpRelativeConditionalFlag(y - 4))
                                    {
                                        this.Tick();
                                    }

                                    this.Tick(2);
                                    break;
                                default:
                                    throw new InvalidOperationException("Unreachable code block reached");
                            }

                            break;

                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.RP(p).Word = this.FetchWord().Word;
                                    this.Tick(3);
                                    break;

                                case 1: // ADD HL,rp
                                    this.Add(this.HL, this.RP(p));
                                    this.Tick(2);
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
                                            this.BusWrite(this.BC, this.A);
                                            this.Tick(2);
                                            break;

                                        case 1: // LD (DE),A
                                            this.BusWrite(this.DE, this.A);
                                            this.Tick(2);
                                            break;

                                        case 2: // GB: LDI (HL),A
                                            this.BusWrite(this.HL.Word++, this.A);
                                            this.Tick(2);
                                            break;

                                        case 3: // GB: LDD (HL),A
                                            this.BusWrite(this.HL.Word--, this.A);
                                            this.Tick(2);
                                            break;

                                        default:
                                            throw new InvalidOperationException("Invalid operation mode");
                                    }

                                    break;

                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            this.A = this.BusRead(this.BC);
                                            this.Tick(2);
                                            break;

                                        case 1: // LD A,(DE)
                                            this.A = this.BusRead(this.DE);
                                            this.Tick(2);
                                            break;

                                        case 2: // GB: LDI A,(HL)
                                            this.A = this.BusRead(this.HL.Word++);
                                            this.Tick(2);
                                            break;

                                        case 3: // GB: LDD A,(HL)
                                            this.A = this.BusRead(this.HL.Word--);
                                            this.Tick(2);
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
                                    ++this.RP(p).Word;
                                    break;

                                case 1: // DEC rp
                                    --this.RP(p).Word;
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            this.Tick(2);
                            break;

                        case 4: // 8-bit INC
                            this.R(y, this.Increment(this.R(y)));
                            this.Tick();
                            if (y == 6)
                            {
                                this.Tick(2);
                            }

                            break;

                        case 5: // 8-bit DEC
                            this.R(y, this.Decrement(this.R(y)));
                            this.Tick();
                            if (y == 6)
                            {
                                this.Tick(2);
                            }

                            break;

                        case 6: // 8-bit load immediate
                            this.R(y, this.FetchByte());
                            this.Tick(2);
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

                            this.Tick();
                            break;

                        default:
                            throw new InvalidOperationException("Invalid operation mode");
                    }

                    break;

                case 1: // 8-bit loading
                    if (z == 6 && y == 6)
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                    }
                    else
                    {
                        this.R(y, this.R(z));
                        if ((y == 6) || (z == 6))
                        {
                            this.Tick(); // M operations
                        }
                    }

                    this.Tick();
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

                    this.Tick();
                    if (z == 6)
                    {
                        this.Tick();
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
                                    if (this.ReturnConditionalFlag(y))
                                    {
                                        this.Tick(3);
                                    }

                                    this.Tick(2);
                                    break;

                                case 4: // GB: LD (FF00 + n),A
                                    this.BusWrite((ushort)(IoRegisters.BASE + this.FetchByte()), this.A);
                                    this.Tick(3);
                                    break;

                                case 5:
                                    { // GB: ADD SP,dd
                                        var before = this.SP.Word;
                                        var value = (sbyte)this.FetchByte();
                                        var result = before + value;
                                        this.SP.Word = (ushort)result;
                                        var carried = before ^ value ^ (result & (int)Mask.Sixteen);
                                        this.F = ClearBit(this.F, StatusBits.ZF | StatusBits.NF);
                                        this.F = SetBit(this.F, StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.F = SetBit(this.F, StatusBits.HC, carried & (int)Bits.Bit4);
                                    }

                                    this.Tick(4);
                                    break;

                                case 6: // GB: LD A,(FF00 + n)
                                    this.A = this.BusRead((ushort)(IoRegisters.BASE + this.FetchByte()));
                                    this.Tick(3);
                                    break;

                                case 7:
                                    { // GB: LD HL,SP + dd
                                        var before = this.SP.Word;
                                        var value = (sbyte)this.FetchByte();
                                        var result = before + value;
                                        this.HL.Word = (ushort)result;
                                        var carried = before ^ value ^ (result & (int)Mask.Sixteen);
                                        this.F = ClearBit(this.F, StatusBits.ZF | StatusBits.NF);
                                        this.F = SetBit(this.F, StatusBits.CF, carried & (int)Bits.Bit8);
                                        this.F = SetBit(this.F, StatusBits.HC, carried & (int)Bits.Bit4);
                                    }

                                    this.Tick(3);
                                    break;

                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    this.RP2(p).Word = this.PopWord().Word;
                                    this.Tick(3);
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            this.Return();
                                            this.Tick(4);
                                            break;
                                        case 1: // GB: RETI
                                            this.RetI();
                                            this.Tick(4);
                                            break;
                                        case 2: // JP HL
                                            this.Jump(this.HL.Word);
                                            this.Tick();
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Word = this.HL.Word;
                                            this.Tick(2);
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
                                    if (this.JumpConditionalFlag(y))
                                    {
                                        this.Tick();
                                    }

                                    this.Tick(3);
                                    break;
                                case 4: // GB: LD (FF00 + C),A
                                    this.BusWrite((ushort)(IoRegisters.BASE + this.C), this.A);
                                    this.Tick(2);
                                    break;
                                case 5: // GB: LD (nn),A
                                    this.Bus.Address.Word = this.MEMPTR.Word = this.FetchWord().Word;
                                    this.BusWrite(this.A);
                                    this.Tick(4);
                                    break;
                                case 6: // GB: LD A,(FF00 + C)
                                    this.A = this.BusRead((ushort)(IoRegisters.BASE + this.C));
                                    this.Tick(2);
                                    break;
                                case 7: // GB: LD A,(nn)
                                    this.Bus.Address.Word = this.MEMPTR.Word = this.FetchWord().Word;
                                    this.A = this.BusRead();
                                    this.Tick(4);
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid operation mode");
                            }

                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    this.Jump(this.FetchWord().Word);
                                    this.Tick(4);
                                    break;
                                case 1: // CB prefix
                                    this.prefixCB = true;
                                    this.Execute(this.FetchByte());
                                    break;
                                case 6: // DI
                                    this.DI();
                                    this.Tick();
                                    break;
                                case 7: // EI
                                    this.EI();
                                    this.Tick();
                                    break;
                            }

                            break;

                        case 4: // Conditional call: CALL cc[y], nn
                            if (this.CallConditionalFlag(y))
                            {
                                this.Tick(3);
                            }

                            this.Tick(3);
                            break;

                        case 5: // PUSH & various ops
                            switch (q)
                            {
                                case 0: // PUSH rp2[p]
                                    this.PushWord(this.RP2(p));
                                    this.Tick(4);
                                    break;

                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            this.Call(this.FetchWord().Word);
                                            this.Tick(6);
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

                            this.Tick(2);
                            break;

                        case 7: // Restart: RST y * 8
                            this.Restart((byte)(y << 3));
                            this.Tick(4);
                            break;

                        default:
                            throw new InvalidOperationException("Invalid operation mode");
                    }

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
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
            }
        }

        private bool JumpRelativeConditionalFlag(int flag)
        {
            switch (flag)
            {
                case 0: // NZ
                    return this.JumpRelativeConditional((this.F & (byte)StatusBits.ZF) == 0);
                case 1: // Z
                    return this.JumpRelativeConditional((this.F & (byte)StatusBits.ZF) != 0);
                case 2: // NC
                    return this.JumpRelativeConditional((this.F & (byte)StatusBits.CF) == 0);
                case 3: // C
                    return this.JumpRelativeConditional((this.F & (byte)StatusBits.CF) != 0);
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
            }
        }

        private void Add(Register16 operand, Register16 value)
        {
            this.MEMPTR.Word = operand.Word;

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

        private byte ADC(byte operand, byte value) => this.Add(operand, value, (this.F & (byte)StatusBits.CF) >> 4);

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

        private byte SBC(byte operand, byte value) => this.Subtract(operand, value, (this.F & (byte)StatusBits.CF) >> 4);

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
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            return (byte)((operand << 1) | (carry >> 4));   // CF at Bit4
        }

        private byte RR(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC | StatusBits.ZF);
            var carry = this.F & (byte)StatusBits.CF;
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
            var carry = this.F & (byte)StatusBits.CF;
            this.AndR(operand, Bit(n));
            this.F = SetBit(this.F, StatusBits.CF, carry);
        }

        private void DAA()
        {
            int updated = this.A;

            if ((this.F & (byte)StatusBits.NF) != 0)
            {
                if ((this.F & (byte)StatusBits.HC) != 0)
                {
                    updated = LowByte(updated - 6);
                }

                if ((this.F & (byte)StatusBits.CF) != 0)
                {
                    updated -= 0x60;
                }
            }
            else
            {
                if (((this.F & (byte)StatusBits.HC) != 0) || LowNibble((byte)updated) > 9)
                {
                    updated += 6;
                }

                if (((this.F & (byte)StatusBits.CF) != 0) || updated > 0x9F)
                {
                    updated += 0x60;
                }
            }

            this.F = ClearBit(this.F, (byte)StatusBits.HC | (byte)StatusBits.ZF);
            this.F = SetBit(this.F, StatusBits.CF, ((this.F & (byte)StatusBits.CF) != 0) || ((updated & (int)Bits.Bit8) != 0));
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
