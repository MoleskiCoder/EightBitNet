// <copyright file="Z80.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Z80 : IntelProcessor
    {
        private readonly InputOutput ports;

        private readonly Register16[] accumulatorFlags = { new Register16(), new Register16() };
        private readonly Register16[,] registers =
        {
            {
                new Register16(), new Register16(), new Register16(),
            },
            {
                new Register16(), new Register16(), new Register16(),
            },
        };

        private readonly Register16 intermediate = new Register16();

        private RefreshRegister refresh = new RefreshRegister(0x7f);

        private bool prefixCB = false;
        private bool prefixDD = false;
        private bool prefixED = false;

        private PinLevel nmiLine = PinLevel.Low;
        private PinLevel m1Line = PinLevel.Low;
        private PinLevel mreqLine = PinLevel.Low;
        private PinLevel iorqLine = PinLevel.Low;
        private PinLevel rdLine = PinLevel.Low;
        private PinLevel wrLine = PinLevel.Low;

        private int accumulatorFlagsSet = 0;

        private int registerSet = 0;
        private sbyte displacement = 0;
        private bool displaced = false;

        public Z80(Bus bus, InputOutput ports)
        : base(bus) => this.ports = ports;

        public event EventHandler<EventArgs> ExecutingInstruction;

        public event EventHandler<EventArgs> ExecutedInstruction;

        public event EventHandler<EventArgs> RaisingNMI;

        public event EventHandler<EventArgs> RaisedNMI;

        public event EventHandler<EventArgs> LoweringNMI;

        public event EventHandler<EventArgs> LoweredNMI;

        public event EventHandler<EventArgs> RaisingM1;

        public event EventHandler<EventArgs> RaisedM1;

        public event EventHandler<EventArgs> LoweringM1;

        public event EventHandler<EventArgs> LoweredM1;

        public event EventHandler<EventArgs> RaisingMREQ;

        public event EventHandler<EventArgs> RaisedMREQ;

        public event EventHandler<EventArgs> LoweringMREQ;

        public event EventHandler<EventArgs> LoweredMREQ;

        public event EventHandler<EventArgs> RaisingIORQ;

        public event EventHandler<EventArgs> RaisedIORQ;

        public event EventHandler<EventArgs> LoweringIORQ;

        public event EventHandler<EventArgs> LoweredIORQ;

        public event EventHandler<EventArgs> RaisingRD;

        public event EventHandler<EventArgs> RaisedRD;

        public event EventHandler<EventArgs> LoweringRD;

        public event EventHandler<EventArgs> LoweredRD;

        public event EventHandler<EventArgs> RaisingWR;

        public event EventHandler<EventArgs> RaisedWR;

        public event EventHandler<EventArgs> LoweringWR;

        public event EventHandler<EventArgs> LoweredWR;

        public byte IV { get; set; } = 0xff;

        public int IM { get; set; } = 0;

        public bool IFF1 { get; set; } = false;

        public bool IFF2 { get; set; } = false;

        public override Register16 AF => this.accumulatorFlags[this.accumulatorFlagsSet];

        public override Register16 BC => this.registers[this.registerSet, (int)RegisterIndex.IndexBC];

        public override Register16 DE => this.registers[this.registerSet, (int)RegisterIndex.IndexDE];

        public override Register16 HL => this.registers[this.registerSet, (int)RegisterIndex.IndexHL];

        public Register16 IX { get; } = new Register16(0xffff);

        public byte IXH { get => this.IX.High; set => this.IX.High = value; }

        public byte IXL { get => this.IX.Low; set => this.IX.Low = value; }

        public Register16 IY { get; } = new Register16(0xffff);

        public byte IYH { get => this.IY.High; set => this.IY.High = value; }

        public byte IYL { get => this.IY.Low; set => this.IY.Low = value; }

        public ref RefreshRegister REFRESH => ref this.refresh;

        public ref PinLevel NMI => ref this.nmiLine;

        public ref PinLevel M1 => ref this.m1Line;

        public ref PinLevel MREQ => ref this.mreqLine;

        public ref PinLevel IORQ => ref this.iorqLine;

        public ref PinLevel RD => ref this.rdLine;

        public ref PinLevel WR => ref this.wrLine;

        private ushort DisplacedAddress
        {
            get
            {
                var returned = (this.prefixDD ? this.IX : this.IY).Word + this.displacement;
                return this.MEMPTR.Word = (ushort)returned;
            }
        }

        public void Exx() => this.registerSet ^= 1;

        public void ExxAF() => this.accumulatorFlagsSet ^= 1;

        public virtual void RaiseNMI()
        {
            if (this.NMI.Lowered())
            {
                this.OnRaisingNMI();
                this.NMI.Raise();
                this.OnRaisedNMI();
            }
        }

        public virtual void LowerNMI()
        {
            if (this.NMI.Raised())
            {
                this.OnLoweringNMI();
                this.NMI.Lower();
                this.OnLoweredNMI();
            }
        }

        public virtual void RaiseM1()
        {
            if (this.M1.Lowered())
            {
                this.OnRaisingM1();
                this.M1.Raise();
                this.OnRaisedM1();
            }
        }

        public virtual void LowerM1()
        {
            if (this.M1.Raised())
            {
                this.OnLoweringM1();
                this.M1.Lower();
                this.OnLoweredM1();
            }
        }

        public virtual void RaiseMREQ()
        {
            if (this.MREQ.Lowered())
            {
                this.OnRaisingMREQ();
                this.MREQ.Raise();
                this.OnRaisedMREQ();
            }
        }

        public virtual void LowerMREQ()
        {
            if (this.MREQ.Raised())
            {
                this.OnLoweringMREQ();
                this.MREQ.Lower();
                this.OnLoweredMREQ();
            }
        }

        public virtual void RaiseIORQ()
        {
            if (this.IORQ.Lowered())
            {
                this.OnRaisingIORQ();
                this.IORQ.Raise();
                this.OnRaisedIORQ();
            }
        }

        public virtual void LowerIORQ()
        {
            if (this.IORQ.Raised())
            {
                this.OnLoweringIORQ();
                this.IORQ.Lower();
                this.OnLoweredIORQ();
            }
        }

        public virtual void RaiseRD()
        {
            if (this.RD.Lowered())
            {
                this.OnRaisingRD();
                this.RD.Raise();
                this.OnRaisedRD();
            }
        }

        public virtual void LowerRD()
        {
            if (this.RD.Raised())
            {
                this.OnLoweringRD();
                this.RD.Lower();
                this.OnLoweredRD();
            }
        }

        public virtual void RaiseWR()
        {
            if (this.WR.Lowered())
            {
                this.OnRaisingWR();
                this.WR.Raise();
                this.OnRaisedWR();
            }
        }

        public virtual void LowerWR()
        {
            if (this.WR.Raised())
            {
                this.OnLoweringWR();
                this.WR.Lower();
                this.OnLoweredWR();
            }
        }

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
            else if (this.prefixED)
            {
                this.ExecuteED(x, y, z, p, q);
            }
            else
            {
                this.ExecuteOther(x, y, z, p, q);
            }

            return this.Cycles;
        }

        public override int Step()
        {
            this.ResetCycles();
            this.OnExecutingInstruction();
            if (this.Powered)
            {
                this.displaced = this.prefixCB = this.prefixDD = this.prefixED = false;
                var handled = false;
                if (this.RESET.Lowered())
                {
                    this.HandleRESET();
                    handled = true;
                }
                else if (this.NMI.Lowered())
                {
                    this.HandleNMI();
                    handled = true;
                }
                else if (this.INT.Lowered())
                {
                    this.RaiseHALT();
                    if (this.IFF1)
                    {
                        this.HandleINT();
                        handled = true;
                    }
                }
                else if (this.HALT.Lowered())
                {
                    this.Execute(0); // NOP
                    handled = true;
                }

                if (!handled)
                {
                    this.Execute(this.FetchInitialOpCode());
                }
            }

            this.OnExecutedInstruction();
            return this.Cycles;
        }

        protected override void OnRaisedPOWER()
        {
            this.RaiseM1();
            this.RaiseMREQ();
            this.RaiseIORQ();
            this.RaiseRD();
            this.RaiseWR();

            this.DisableInterrupts();
            this.IM = 0;

            this.REFRESH = new RefreshRegister(0);
            this.IV = (byte)Mask.Mask8;

            this.ExxAF();
            this.Exx();

            this.AF.Word = this.IX.Word = this.IY.Word = this.BC.Word = this.DE.Word = this.HL.Word = (ushort)Mask.Mask16;

            this.prefixCB = this.prefixDD = this.prefixED = false;

            base.OnRaisedPOWER();
        }

        protected virtual void OnExecutingInstruction() => this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnExecutedInstruction() => this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingNMI() => this.RaisingNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedNMI() => this.RaisedNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringNMI() => this.LoweringNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredNMI() => this.LoweredNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingM1() => this.RaisingM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedM1() => this.RaisedM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringM1() => this.LoweringM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredM1()
        {
            ++this.REFRESH;
            this.LoweredM1?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLoweringMREQ() => this.LoweringMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredMREQ() => this.LoweredMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingMREQ() => this.RaisingMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedMREQ() => this.RaisedMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringIORQ() => this.LoweringIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredIORQ() => this.LoweredIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingIORQ() => this.RaisingIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedIORQ() => this.RaisedIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRD() => this.LoweringRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRD() => this.LoweredRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRD() => this.RaisingRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRD() => this.RaisedRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringWR() => this.LoweringWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredWR() => this.LoweredWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingWR() => this.RaisingWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedWR() => this.RaisedWR?.Invoke(this, EventArgs.Empty);

        protected override void BusWrite()
        {
            this.LowerMREQ();
            this.LowerWR();
            base.BusWrite();
            this.RaiseWR();
            this.RaiseMREQ();
        }

        protected override byte BusRead()
        {
            this.LowerMREQ();
            this.LowerRD();
            var returned = base.BusRead();
            this.RaiseRD();
            this.RaiseMREQ();
            return returned;
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.DisableInterrupts();
            this.Tick(3);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.LowerM1();
            this.LowerIORQ();
            var data = this.Bus.Data;
            this.RaiseIORQ();
            this.RaiseM1();
            this.DisableInterrupts();
            switch (this.IM)
            {
                case 0: // i8080 equivalent
                    this.Execute(data);
                    break;
                case 1:
                    this.Restart(7 << 3);
                    this.Tick(13);
                    break;
                case 2:
                    this.Call(this.MEMPTR.Word = new Register16(data, this.IV).Word);
                    this.Tick(19);
                    break;
                default:
                    throw new NotSupportedException("Invalid interrupt mode");
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

        private static byte AdjustXY(byte input, byte value)
        {
            input = SetBit(input, StatusBits.XF, value & (byte)StatusBits.XF);
            return SetBit(input, StatusBits.YF, value & (byte)StatusBits.YF);
        }

        private static byte AdjustSZPXY(byte input, byte value)
        {
            input = AdjustSZP(input, value);
            return AdjustXY(input, value);
        }

        private static byte AdjustSZXY(byte input, byte value)
        {
            input = AdjustSZ(input, value);
            return AdjustXY(input, value);
        }

        private static byte AdjustHalfCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarryAdd(before, value, calculation));

        private static byte AdjustHalfCarrySub(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarrySub(before, value, calculation));

        private static byte AdjustOverflowAdd(byte input, int beforeNegative, int valueNegative, int afterNegative)
        {
            var overflow = (beforeNegative == valueNegative) && (beforeNegative != afterNegative);
            return SetBit(input, StatusBits.VF, overflow);
        }

        private static byte AdjustOverflowAdd(byte input, byte before, byte value, byte calculation) => AdjustOverflowAdd(input, before & (byte)StatusBits.SF, value & (byte)StatusBits.SF, calculation & (byte)StatusBits.SF);

        private static byte AdjustOverflowSub(byte input, int beforeNegative, int valueNegative, int afterNegative)
        {
            var overflow = (beforeNegative != valueNegative) && (beforeNegative != afterNegative);
            return SetBit(input, StatusBits.VF, overflow);
        }

        private static byte AdjustOverflowSub(byte input, byte before, byte value, byte calculation) => AdjustOverflowSub(input, before & (byte)StatusBits.SF, value & (byte)StatusBits.SF, calculation & (byte)StatusBits.SF);

        private static byte RES(int n, byte operand) => ClearBit(operand, Bit(n));

        private static byte SET(int n, byte operand) => SetBit(operand, Bit(n));

        private void DisableInterrupts() => this.IFF1 = this.IFF2 = false;

        private void EnableInterrupts() => this.IFF1 = this.IFF2 = true;

        private Register16 HL2()
        {
            if (!this.displaced)
            {
                return this.HL;
            }

            if (this.prefixDD)
            {
                return this.IX;
            }

            // Must be FD prefix
            return this.IY;
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
                    return this.HL2();
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
                    return this.HL2();
                case 3:
                    return this.AF;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rp));
            }
        }

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
                    return this.HL2().High;
                case 5:
                    return this.HL2().Low;
                case 6:
                    return this.BusRead(this.displaced ? this.DisplacedAddress : this.HL.Word);
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
                    this.HL2().High = value;
                    break;
                case 5:
                    this.HL2().Low = value;
                    break;
                case 6:
                    this.BusWrite(this.displaced ? this.DisplacedAddress : this.HL.Word, value);
                    break;
                case 7:
                    this.A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void R2(int r, byte value)
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
                    this.BusWrite(this.HL, value);
                    break;
                case 7:
                    this.A = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void ExecuteCB(int x, int y, int z)
        {
            var memoryZ = z == 6;
            var indirect = (!this.displaced && memoryZ) || this.displaced;
            var direct = !indirect;
            var operand = !this.displaced ? this.R(z) : this.BusRead(this.DisplacedAddress);
            var update = x != 1; // BIT does not update
            switch (x)
            {
                case 0: // rot[y] r[z]
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
                        case 6:
                            operand = this.SLL(operand);
                            break;
                        case 7:
                            operand = this.SRL(operand);
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    this.F = AdjustSZP(this.F, operand);
                    this.Tick(8);
                    break;
                case 1: // BIT y, r[z]
                    this.Tick(8);
                    this.BIT(y, operand);
                    if (direct)
                    {
                        this.F = AdjustXY(this.F, operand);
                    }
                    else
                    {
                        this.F = AdjustXY(this.F, this.MEMPTR.High);
                        this.Tick(4);
                    }

                    break;
                case 2: // RES y, r[z]
                    this.Tick(8);
                    operand = RES(y, operand);
                    break;
                case 3: // SET y, r[z]
                    this.Tick(8);
                    operand = SET(y, operand);
                    break;
                default:
                    throw new NotSupportedException("Invalid operation mode");
            }

            if (update)
            {
                if (!this.displaced)
                {
                    this.R(z, operand);
                    if (memoryZ)
                    {
                        this.Tick(7);
                    }
                }
                else
                {
                    this.BusWrite(operand);
                    if (!memoryZ)
                    {
                        this.R2(z, operand);
                    }

                    this.Tick(15);
                }
            }
        }

        private void ExecuteED(int x, int y, int z, int p, int q)
        {
            switch (x)
            {
                case 0:
                case 3: // Invalid instruction, equivalent to NONI followed by NOP
                    this.Tick(8);
                    break;
                case 1:
                    switch (z)
                    {
                        case 0: // Input from port with 16-bit address
                            this.MEMPTR.Word = this.Bus.Address.Word = this.BC.Word;
                            this.MEMPTR.Word++;
                            this.ReadPort();
                            if (y != 6)
                            {
                                this.R(y, this.Bus.Data); // IN r[y],(C)
                            }

                            this.F = AdjustSZPXY(this.F, this.Bus.Data);
                            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
                            this.Tick(12);
                            break;
                        case 1: // Output to port with 16-bit address
                            this.MEMPTR.Word = this.Bus.Address.Word = this.BC.Word;
                            this.MEMPTR.Word++;
                            this.Bus.Data = y != 6 ? this.R(y) : (byte)0;

                            this.WritePort();
                            this.Tick(12);
                            break;
                        case 2: // 16-bit add/subtract with carry
                            switch (q)
                            {
                                case 0: // SBC HL, rp[p]
                                    this.HL2().Word = this.SBC(this.HL2(), this.RP(p));
                                    break;
                                case 1: // ADC HL, rp[p]
                                    this.HL2().Word = this.ADC(this.HL2(), this.RP(p));
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(15);
                            break;
                        case 3: // Retrieve/store register pair from/to immediate address
                            this.Bus.Address.Word = this.FetchWord().Word;
                            switch (q)
                            {
                                case 0: // LD (nn), rp[p]
                                    this.SetWord(this.RP(p));
                                    break;
                                case 1: // LD rp[p], (nn)
                                    this.RP(p).Word = this.GetWord().Word;
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(20);
                            break;
                        case 4: // Negate accumulator
                            this.NEG();
                            this.Tick(8);
                            break;
                        case 5: // Return from interrupt
                            switch (y)
                            {
                                case 1:
                                    this.RetI(); // RETI
                                    break;
                                default:
                                    this.RetN(); // RETN
                                    break;
                            }

                            this.Tick(14);
                            break;
                        case 6: // Set interrupt mode
                            switch (y)
                            {
                                case 0:
                                case 1:
                                case 4:
                                case 5:
                                    this.IM = 0;
                                    break;
                                case 2:
                                case 6:
                                    this.IM = 1;
                                    break;
                                case 3:
                                case 7:
                                    this.IM = 2;
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(8);
                            break;
                        case 7: // Assorted ops
                            switch (y)
                            {
                                case 0: // LD I,A
                                    this.IV = this.A;
                                    this.Tick(9);
                                    break;
                                case 1: // LD R,A
                                    this.REFRESH = this.A;
                                    this.Tick(9);
                                    break;
                                case 2: // LD A,I
                                    this.F = AdjustSZXY(this.F, this.A = this.IV);
                                    this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
                                    this.F = SetBit(this.F, StatusBits.PF, this.IFF2);
                                    this.Tick(9);
                                    break;
                                case 3: // LD A,R
                                    this.F = AdjustSZXY(this.F, this.A = this.REFRESH);
                                    this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
                                    this.F = SetBit(this.F, StatusBits.PF, this.IFF2);
                                    this.Tick(9);
                                    break;
                                case 4: // RRD
                                    this.RRD();
                                    this.Tick(18);
                                    break;
                                case 5: // RLD
                                    this.RLD();
                                    this.Tick(18);
                                    break;
                                case 6: // NOP
                                case 7: // NOP
                                    this.Tick(4);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 2:
                    switch (z)
                    {
                        case 0: // LD
                            switch (y)
                            {
                                case 4: // LDI
                                    this.LDI();
                                    break;
                                case 5: // LDD
                                    this.LDD();
                                    break;
                                case 6: // LDIR
                                    if (this.LDIR())
                                    {
                                        this.MEMPTR.Word = --this.PC.Word;
                                        --this.PC.Word;
                                        this.Tick(5);
                                    }

                                    break;
                                case 7: // LDDR
                                    if (this.LDDR())
                                    {
                                        this.MEMPTR.Word = --this.PC.Word;
                                        --this.PC.Word;
                                        this.Tick(5);
                                    }

                                    break;
                            }

                            break;
                        case 1: // CP
                            switch (y)
                            {
                                case 4: // CPI
                                    this.CPI();
                                    break;
                                case 5: // CPD
                                    this.CPD();
                                    break;
                                case 6: // CPIR
                                    if (this.CPIR())
                                    {
                                        this.MEMPTR.Word = --this.PC.Word;
                                        --this.PC.Word;
                                        this.Tick(5);
                                    }

                                    break;
                                case 7: // CPDR
                                    if (this.CPDR())
                                    {
                                        this.MEMPTR.Word = --this.PC.Word;
                                        --this.PC.Word;
                                        this.Tick(5);
                                    }
                                    else
                                    {
                                        this.MEMPTR.Word = (ushort)(this.PC.Word - 2);
                                    }

                                    break;
                            }

                            break;
                        case 2: // IN
                            switch (y)
                            {
                                case 4: // INI
                                    this.INI();
                                    break;
                                case 5: // IND
                                    this.IND();
                                    break;
                                case 6: // INIR
                                    if (this.INIR())
                                    {
                                        this.PC.Word -= 2;
                                        this.Tick(5);
                                    }

                                    break;
                                case 7: // INDR
                                    if (this.INDR())
                                    {
                                        this.PC.Word -= 2;
                                        this.Tick(5);
                                    }

                                    break;
                            }

                            break;
                        case 3: // OUT
                            switch (y)
                            {
                                case 4: // OUTI
                                    this.OUTI();
                                    break;
                                case 5: // OUTD
                                    this.OUTD();
                                    break;
                                case 6: // OTIR
                                    if (this.OTIR())
                                    {
                                        this.PC.Word -= 2;
                                        this.Tick(5);
                                    }

                                    break;
                                case 7: // OTDR
                                    if (this.OTDR())
                                    {
                                        this.PC.Word -= 2;
                                        this.Tick(5);
                                    }

                                    break;
                            }

                            break;
                    }

                    this.Tick(16);
                    break;
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
                                    if (this.prefixDD)
                                    {
                                        this.Tick(4);
                                    }

                                    this.Tick(4);
                                    break;
                                case 1: // EX AF AF'
                                    this.ExxAF();
                                    this.Tick(4);
                                    break;
                                case 2: // DJNZ d
                                    if (this.JumpRelativeConditional(--this.B != 0))
                                    {
                                        this.Tick(5);
                                    }

                                    this.Tick(8);
                                    break;
                                case 3: // JR d
                                    this.JumpRelative((sbyte)this.FetchByte());
                                    this.Tick(12);
                                    break;
                                case 4: // JR cc,d
                                case 5:
                                case 6:
                                case 7:
                                    if (this.JumpRelativeConditionalFlag(y - 4))
                                    {
                                        this.Tick(5);
                                    }

                                    this.Tick(5);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
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
                                    this.HL2().Word = this.Add(this.HL2(), this.RP(p));
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
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.BC.Word;
                                            ++this.MEMPTR.Word;
                                            this.MEMPTR.High = this.Bus.Data = this.A;
                                            this.BusWrite();
                                            this.Tick(7);
                                            break;
                                        case 1: // LD (DE),A
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.DE.Word;
                                            ++this.MEMPTR.Word;
                                            this.MEMPTR.High = this.Bus.Data = this.A;
                                            this.BusWrite();
                                            this.Tick(7);
                                            break;
                                        case 2: // LD (nn),HL
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.SetWord(this.HL2());
                                            this.Tick(16);
                                            break;
                                        case 3: // LD (nn),A
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.FetchWord().Word;
                                            ++this.MEMPTR.Word;
                                            this.MEMPTR.High = this.Bus.Data = this.A;
                                            this.BusWrite();
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
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.BC.Word;
                                            ++this.MEMPTR.Word;
                                            this.A = this.BusRead();
                                            this.Tick(7);
                                            break;
                                        case 1: // LD A,(DE)
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.DE.Word;
                                            ++this.MEMPTR.Word;
                                            this.A = this.BusRead();
                                            this.Tick(7);
                                            break;
                                        case 2: // LD HL,(nn)
                                            this.Bus.Address.Word = this.FetchWord().Word;
                                            this.HL2().Word = this.GetWord().Word;
                                            this.Tick(16);
                                            break;
                                        case 3: // LD A,(nn)
                                            this.MEMPTR.Word = this.Bus.Address.Word = this.FetchWord().Word;
                                            ++this.MEMPTR.Word;
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
                            if (this.displaced && memoryY)
                            {
                                this.FetchDisplacement();
                            }

                            this.R(y, this.Increment(this.R(y)));
                            this.Tick(4);
                            break;
                        case 5: // 8-bit DEC
                            if (memoryY)
                            {
                                this.Tick(7);
                                if (this.displaced)
                                {
                                    this.FetchDisplacement();
                                }
                            }

                            this.R(y, this.Decrement(this.R(y)));
                            this.Tick(4);
                            break;
                        case 6: // 8-bit load immediate
                            if (memoryY)
                            {
                                this.Tick(3);
                                if (this.displaced)
                                {
                                    this.FetchDisplacement();
                                }
                            }

                            this.R(y, this.FetchByte());  // LD r,n
                            this.Tick(7);
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
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            this.Tick(4);
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 1: // 8-bit loading
                    if (!(memoryZ && memoryY))
                    {
                        var normal = true;
                        if (this.displaced)
                        {
                            if (memoryZ || memoryY)
                            {
                                this.FetchDisplacement();
                            }

                            if (memoryZ)
                            {
                                switch (y)
                                {
                                    case 4:
                                        this.H = this.R(z);
                                        normal = false;
                                        break;
                                    case 5:
                                        this.L = this.R(z);
                                        normal = false;
                                        break;
                                }
                            }

                            if (memoryY)
                            {
                                switch (z)
                                {
                                    case 4:
                                        this.R(y, this.H);
                                        normal = false;
                                        break;
                                    case 5:
                                        this.R(y, this.L);
                                        normal = false;
                                        break;
                                }
                            }
                        }

                        if (normal)
                        {
                            this.R(y, this.R(z));
                        }

                        // M operations
                        if (memoryY || memoryZ)
                        {
                            this.Tick(3);
                        }
                    }
                    else
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                    }

                    this.Tick(4);
                    break;
                case 2:
                    { // Operate on accumulator and register/memory location
                        if (memoryZ)
                        {
                            this.Tick(3);
                            if (this.displaced)
                            {
                                this.FetchDisplacement();
                            }
                        }

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

                        this.Tick(4);
                        break;
                    }

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
                                        case 1: // EXX
                                            this.Exx();
                                            this.Tick(4);
                                            break;
                                        case 2: // JP HL
                                            this.Jump(this.HL2().Word);
                                            this.Tick(4);
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Word = this.HL2().Word;
                                            this.Tick(4);
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
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
                                    this.Jump(this.MEMPTR.Word = this.FetchWord().Word);
                                    this.Tick(10);
                                    break;
                                case 1: // CB prefix
                                    this.prefixCB = true;
                                    if (this.displaced)
                                    {
                                        this.FetchDisplacement();
                                        this.Execute(this.FetchByte());
                                    }
                                    else
                                    {
                                        this.Execute(this.FetchInitialOpCode());
                                    }

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
                                    this.XHTL(this.HL2());
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
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
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
                                            this.Call(this.MEMPTR.Word = this.FetchWord().Word);
                                            this.Tick(17);
                                            break;
                                        case 1: // DD prefix
                                            this.displaced = this.prefixDD = true;
                                            this.Execute(this.FetchInitialOpCode());
                                            break;
                                        case 2: // ED prefix
                                            this.prefixED = true;
                                            this.Execute(this.FetchInitialOpCode());
                                            break;
                                        case 3: // FD prefix
                                            this.displaced = true;
                                            this.Execute(this.FetchInitialOpCode());
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
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

                                this.Tick(7);
                                break;
                            }

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

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.RaiseHALT();
            this.IFF1 = false;
            this.LowerM1();
            var discarded = this.Bus.Data;
            this.RaiseM1();
            this.Restart(0x66);
            this.Tick(13);
        }

        private void FetchDisplacement() => this.displacement = (sbyte)this.FetchByte();

        private byte FetchInitialOpCode()
        {
            this.LowerM1();
            var returned = this.FetchByte();
            this.RaiseM1();
            return returned;
        }

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.intermediate.Word = (ushort)(operand - value - carry);
            var result = this.intermediate.Low;

            this.F = AdjustHalfCarrySub(this.F, operand, value, result);
            this.F = AdjustOverflowSub(this.F, operand, value, result);

            this.F = SetBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, this.intermediate.High & (byte)StatusBits.CF);
            this.F = AdjustSZ(this.F, result);

            return result;
        }

        private byte Increment(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF);
            var result = ++operand;
            this.F = AdjustSZXY(this.F, result);
            this.F = SetBit(this.F, StatusBits.VF, result == (byte)Bits.Bit7);
            this.F = ClearBit(this.F, StatusBits.HC, LowNibble(result));
            return result;
        }

        private byte Decrement(byte operand)
        {
            this.F = SetBit(this.F, StatusBits.NF);
            this.F = ClearBit(this.F, StatusBits.HC, LowNibble(operand));
            var result = --operand;
            this.F = AdjustSZXY(this.F, result);
            this.F = SetBit(this.F, StatusBits.VF, result == (byte)Mask.Mask7);
            return result;
        }

        private void RetN()
        {
            this.Return();
            this.IFF1 = this.IFF2;
        }

        private void RetI() => this.RetN();

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

        private ushort SBC(Register16 operand, Register16 value)
        {
            var subtraction = operand.Word - value.Word - (this.F & (byte)StatusBits.CF);
            this.intermediate.Word = (ushort)subtraction;

            this.F = SetBit(this.F, StatusBits.NF);
            this.F = ClearBit(this.F, StatusBits.ZF, this.intermediate.Word);
            this.F = SetBit(this.F, StatusBits.CF, subtraction & (int)Bits.Bit16);
            this.F = AdjustHalfCarrySub(this.F, operand.High, value.High, this.intermediate.High);
            this.F = AdjustXY(this.F, this.intermediate.High);

            var beforeNegative = operand.High & (byte)StatusBits.SF;
            var valueNegative = value.High & (byte)StatusBits.SF;
            var afterNegative = this.intermediate.High & (byte)StatusBits.SF;

            this.F = SetBit(this.F, StatusBits.SF, afterNegative);
            this.F = AdjustOverflowSub(this.F, beforeNegative, valueNegative, afterNegative);

            this.MEMPTR.Word = (ushort)(operand.Word + 1);

            return this.intermediate.Word;
        }

        private ushort ADC(Register16 operand ,Register16 value)
        {
            this.Add(operand, value, this.F & (byte)StatusBits.CF); // Leaves result in intermediate anyway
            this.F = ClearBit(this.F, StatusBits.ZF, this.intermediate.Word);

            var beforeNegative = operand.High & (byte)StatusBits.SF;
            var valueNegative = value.High & (byte)StatusBits.SF;
            var afterNegative = this.intermediate.High & (byte)StatusBits.SF;

            this.F = SetBit(this.F, StatusBits.SF, afterNegative);
            this.F = AdjustOverflowAdd(this.F, beforeNegative, valueNegative, afterNegative);

            return this.intermediate.Word;
        }

        private ushort Add(Register16 operand, Register16 value, int carry = 0)
        {
            var addition = operand.Word + value.Word + carry;
            this.intermediate.Word = (ushort)addition;

            this.F = ClearBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, addition & (int)Bits.Bit16);
            this.F = AdjustHalfCarryAdd(this.F, operand.High, value.High, this.intermediate.High);
            this.F = AdjustXY(this.F, this.intermediate.High);

            this.MEMPTR.Word = (ushort)(operand.Word + 1);

            return this.intermediate.Word;
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            this.intermediate.Word = (ushort)(operand + value + carry);
            var result = this.intermediate.Low;

            this.F = AdjustHalfCarryAdd(this.F, operand, value, result);
            this.F = AdjustOverflowAdd(this.F, operand, value, result);

            this.F = ClearBit(this.F, StatusBits.NF);
            this.F = SetBit(this.F, StatusBits.CF, this.intermediate.High & (byte)StatusBits.CF);
            this.F = AdjustSZXY(this.F, result);

            return result;
        }

        private byte ADC(byte operand, byte value) => this.Add(operand, value, this.F & (byte)StatusBits.CF);

        private byte SUB(byte operand, byte value, int carry = 0)
        {
            var subtraction = this.Subtract(operand, value, carry);
            this.F = AdjustXY(this.F, subtraction);
            return subtraction;
        }

        private byte SBC(byte operand, byte value) => this.SUB(operand, value, this.F & (byte)StatusBits.CF);

        private void AndR(byte value)
        {
            this.F = SetBit(this.F, StatusBits.HC);
            this.F = ClearBit(this.F, StatusBits.CF | StatusBits.NF);
            this.F = AdjustSZPXY(this.F, this.A &= value);
        }

        private void XorR(byte value)
        {
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.F = AdjustSZPXY(this.F, this.A ^= value);
        }

        private void OrR(byte value)
        {
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.F = AdjustSZPXY(this.F, this.A |= value);
        }

        private void Compare(byte value)
        {
            this.Subtract(this.A, value);
            this.F = AdjustXY(this.F, value);
        }

        private byte RLC(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit7;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            var result = (byte)((operand << 1) | (carry >> 7));
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte RRC(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit0;
            this.F = SetBit(this.F, StatusBits.CF, carry);
            var result = (byte)((operand >> 1) | (carry << 7));
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte RL(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | carry);
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte RR(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (carry << 7));
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte SLA(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)(operand << 1);
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte SRA(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (operand & (byte)Bits.Bit7));
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte SLL(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | (byte)Bits.Bit0);
            this.F = AdjustXY(this.F, result);
            return result;
        }

        private byte SRL(byte operand)
        {
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = SetBit(this.F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) & ~(byte)Bits.Bit7);
            this.F = AdjustXY(this.F, result);
            this.F = SetBit(this.F, StatusBits.ZF, result);
            return result;
        }

        private void BIT(int n, byte operand)
        {
            this.F = SetBit(this.F, StatusBits.HC);
            this.F = ClearBit(this.F, StatusBits.NF);
            var discarded = (byte)(operand & Bit(n));
            this.F = AdjustSZ(this.F, discarded);
            this.F = ClearBit(this.F, StatusBits.PF, discarded);
        }

        private void DAA()
        {
            var updated = this.A;

            var lowAdjust = ((this.F & (byte)StatusBits.HC) != 0) || (LowNibble(this.A) > 9);
            var highAdjust = ((this.F & (byte)StatusBits.CF) != 0) || (this.A > 0x99);

            if ((this.F & (byte)StatusBits.NF) != 0)
            {
                if (lowAdjust)
                {
                    updated -= 6;
                }

                if (highAdjust)
                {
                    updated -= 0x60;
                }
            }
            else
            {
                if (lowAdjust)
                {
                    updated += 6;
                }

                if (highAdjust)
                {
                    updated += 0x60;
                }
            }

            this.F = (byte)((this.F & (byte)(StatusBits.CF | StatusBits.NF)) | (this.A > 0x99 ? (byte)StatusBits.CF : 0) | ((this.A ^ updated) & (byte)StatusBits.HC));

            this.F = AdjustSZPXY(this.F, this.A = updated);
        }

        private void SCF()
        {
            this.F = SetBit(this.F, StatusBits.CF);
            this.F = ClearBit(this.F, StatusBits.HC | StatusBits.NF);
            this.F = AdjustXY(this.F, this.A);
        }

        private void CCF()
        {
            this.F = ClearBit(this.F, StatusBits.NF);
            var carry = this.F & (byte)StatusBits.CF;
            this.F = SetBit(this.F, StatusBits.HC, carry);
            this.F = ClearBit(this.F, StatusBits.CF, carry);
            this.F = AdjustXY(this.F, this.A);
        }

        private void CPL()
        {
            this.F = SetBit(this.F, StatusBits.HC | StatusBits.NF);
            this.F = AdjustXY(this.F, this.A = (byte)~this.A);
        }

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

        private void BlockCompare(ushort source, ushort counter)
        {
            var value = this.BusRead(source);
            var result = (byte)(this.A - value);

            this.F = SetBit(this.F, StatusBits.PF, counter);

            this.F = AdjustSZ(this.F, result);
            this.F = AdjustHalfCarrySub(this.F, this.A, value, result);
            this.F = SetBit(this.F, StatusBits.NF);

            result -= (byte)((this.F & (byte)StatusBits.HC) >> 4);

            this.F = SetBit(this.F, StatusBits.YF, result & (byte)Bits.Bit1);
            this.F = SetBit(this.F, StatusBits.XF, result & (byte)Bits.Bit3);
        }

        private void CPI()
        {
            this.BlockCompare(this.HL.Word++, --this.BC.Word);
            ++this.MEMPTR.Word;
        }

        private bool CPIR()
        {
            this.CPI();
            return ((this.F & (byte)StatusBits.PF) != 0) && ((this.F & (byte)StatusBits.ZF) == 0); // See CPI
        }

        private void CPD()
        {
            this.BlockCompare(this.HL.Word--, --this.BC.Word);
            --this.MEMPTR.Word;
        }

        private bool CPDR()
        {
            this.CPD();
            return ((this.F & (byte)StatusBits.PF) != 0) && ((this.F & (byte)StatusBits.ZF) == 0); // See CPD
        }

        private void BlockLoad(ushort source, ushort destination, ushort counter)
        {
            var value = this.BusRead(source);
            this.BusWrite(destination, value);
            var xy = this.A + value;
            this.F = SetBit(this.F, StatusBits.XF, xy & (int)Bits.Bit3);
            this.F = SetBit(this.F, StatusBits.YF, xy & (int)Bits.Bit1);
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
            this.F = SetBit(this.F, StatusBits.PF, counter);
        }

        private void LDI() => this.BlockLoad(this.HL.Word++, this.DE.Word++, --this.BC.Word);

        private bool LDIR()
        {
            this.LDI();
            return (this.F & (byte)StatusBits.PF) != 0; // See LDI
        }

        private void LDD() => this.BlockLoad(this.HL.Word--, this.DE.Word--, --this.BC.Word);

        private bool LDDR()
        {
            this.LDD();
            return (this.F & (byte)StatusBits.PF) != 0; // See LDD
        }

        private void BlockIn(Register16 source, ushort destination)
        {
            this.MEMPTR.Word = this.Bus.Address.Word = source.Word;
            var value = this.ReadPort();
            this.BusWrite(destination, value);
            source.High = this.Decrement(source.High);
            this.F = SetBit(this.F, StatusBits.NF);
        }

        private void INI()
        {
            this.BlockIn(this.BC, this.HL.Word++);
            ++this.MEMPTR.Word;
        }

        private bool INIR()
        {
            this.INI();
            return (this.F & (byte)StatusBits.ZF) == 0; // See INI
        }

        private void IND()
        {
            this.BlockIn(this.BC, this.HL.Word--);
            --this.MEMPTR.Word;
        }

        private bool INDR()
        {
            this.IND();
            return (this.F & (byte)StatusBits.ZF) == 0; // See IND
        }

        private void BlockOut(ushort source, Register16 destination)
        {
            var value = this.BusRead(source);
            destination.High = this.Decrement(destination.High);
            this.Bus.Address.Word = destination.Word;
            this.WritePort();
            this.MEMPTR.Word = destination.Word;
            this.F = SetBit(this.F, StatusBits.NF, value & (byte)Bits.Bit7);
            this.F = SetBit(this.F, StatusBits.HC | StatusBits.CF, (this.L + value) > 0xff);
            this.F = AdjustParity(this.F, (byte)(((value + this.L) & (int)Mask.Mask3) ^ this.B));
        }

        private void OUTI()
        {
            this.BlockOut(this.HL.Word++, this.BC);
            ++this.MEMPTR.Word;
        }

        private bool OTIR()
        {
            this.OUTI();
            return (this.F & (byte)StatusBits.ZF) == 0; // See OUTI
        }

        private void OUTD()
        {
            this.BlockOut(this.HL.Word--, this.BC);
            --this.MEMPTR.Word;
        }

        private bool OTDR()
        {
            this.OUTD();
            return (this.F & (byte)StatusBits.ZF) == 0; // See OUTD
        }

        private void NEG()
        {
            this.F = SetBit(this.F, StatusBits.PF, this.A == (byte)Bits.Bit7);
            this.F = SetBit(this.F, StatusBits.CF, this.A);
            this.F = SetBit(this.F, StatusBits.NF);

            var original = this.A;

            this.A = (byte)(~this.A + 1);   // two's complement

            this.F = AdjustHalfCarrySub(this.F, (byte)0, original, this.A);
            this.F = AdjustOverflowSub(this.F, (byte)0, original, this.A);

            this.F = AdjustSZXY(this.F, this.A);
        }

        private void RRD()
        {
            this.MEMPTR.Word = this.Bus.Address.Word = this.HL.Word;
            ++this.MEMPTR.Word;
            var memory = this.BusRead();
            this.BusWrite((byte)(PromoteNibble(this.A) | HighNibble(memory)));
            this.A = (byte)(HigherNibble(this.A) | LowerNibble(memory));
            this.F = AdjustSZPXY(this.F, this.A);
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
        }

        private void RLD()
        {
            this.MEMPTR.Word = this.Bus.Address.Word = this.HL.Word;
            ++this.MEMPTR.Word;
            var memory = this.BusRead();
            this.BusWrite((byte)(PromoteNibble(memory) | LowNibble(this.A)));
            this.A = (byte)(HigherNibble(this.A) | HighNibble(memory));
            this.F = AdjustSZPXY(this.F, this.A);
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
        }

        private void WritePort(byte port)
        {
            this.MEMPTR.Word = this.Bus.Address.Word = new Register16(port, this.A).Word;
            this.Bus.Data = this.A;
            this.WritePort();
            ++this.MEMPTR.Low;
        }

        private void WritePort()
        {
            this.LowerIORQ();
            this.LowerWR();
            this.ports.Write(this.Bus.Address.Low, this.Bus.Data);
            this.RaiseWR();
            this.RaiseIORQ();
        }

        private byte ReadPort(byte port)
        {
            this.MEMPTR.Word = this.Bus.Address.Word = new Register16(port, this.A).Word;
            ++this.MEMPTR.Low;
            return this.ReadPort();
        }

        private byte ReadPort()
        {
            this.LowerIORQ();
            this.LowerRD();
            var returned = this.Bus.Data = this.ports.Read(this.Bus.Address.Low);
            this.RaiseRD();
            this.RaiseIORQ();
            return returned;
        }
    }
}
