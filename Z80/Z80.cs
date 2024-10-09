// <copyright file="Z80.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Z80(Bus bus, InputOutput ports) : IntelProcessor(bus)
    {
        private readonly InputOutput _ports = ports;

        private readonly Register16[] _accumulatorFlags = [new Register16(), new Register16()];
        private readonly Register16[,] _registers =
        {
            {
                new Register16(), new Register16(), new Register16(),
            },
            {
                new Register16(), new Register16(), new Register16(),
            },
        };

        private RefreshRegister _refresh = new(0x7f);

        private bool _prefixCB;
        private bool _prefixDD;
        private bool _prefixED;
        private bool _prefixFD;

        private PinLevel _nmiLine = PinLevel.Low;
        private PinLevel _m1Line = PinLevel.Low;
        private PinLevel _rfshLine = PinLevel.Low;
        private PinLevel _mreqLine = PinLevel.Low;
        private PinLevel _iorqLine = PinLevel.Low;
        private PinLevel _rdLine = PinLevel.Low;
        private PinLevel _wrLine = PinLevel.Low;

        private int _accumulatorFlagsSet;

        private int _registerSet;
        private sbyte _displacement;
        private bool _displaced;

        public event EventHandler<EventArgs>? RaisingNMI;

        public event EventHandler<EventArgs>? RaisedNMI;

        public event EventHandler<EventArgs>? LoweringNMI;

        public event EventHandler<EventArgs>? LoweredNMI;

        public event EventHandler<EventArgs>? RaisingM1;

        public event EventHandler<EventArgs>? RaisedM1;

        public event EventHandler<EventArgs>? LoweringM1;

        public event EventHandler<EventArgs>? LoweredM1;

        public event EventHandler<EventArgs>? RaisingRFSH;

        public event EventHandler<EventArgs>? RaisedRFSH;

        public event EventHandler<EventArgs>? LoweringRFSH;

        public event EventHandler<EventArgs>? LoweredRFSH;

        public event EventHandler<EventArgs>? RaisingMREQ;

        public event EventHandler<EventArgs>? RaisedMREQ;

        public event EventHandler<EventArgs>? LoweringMREQ;

        public event EventHandler<EventArgs>? LoweredMREQ;

        public event EventHandler<EventArgs>? RaisingIORQ;

        public event EventHandler<EventArgs>? RaisedIORQ;

        public event EventHandler<EventArgs>? LoweringIORQ;

        public event EventHandler<EventArgs>? LoweredIORQ;

        public event EventHandler<EventArgs>? RaisingRD;

        public event EventHandler<EventArgs>? RaisedRD;

        public event EventHandler<EventArgs>? LoweringRD;

        public event EventHandler<EventArgs>? LoweredRD;

        public event EventHandler<EventArgs>? RaisingWR;

        public event EventHandler<EventArgs>? RaisedWR;

        public event EventHandler<EventArgs>? LoweringWR;

        public event EventHandler<EventArgs>? LoweredWR;

        public byte IV { get; set; } = 0xff;

        public int IM { get; set; }

        public bool IFF1 { get; set; }

        public bool IFF2 { get; set; }

        public override Register16 AF => _accumulatorFlags[_accumulatorFlagsSet];

        public override Register16 BC => _registers[_registerSet, (int)RegisterIndex.IndexBC];

        public override Register16 DE => _registers[_registerSet, (int)RegisterIndex.IndexDE];

        public override Register16 HL => _registers[_registerSet, (int)RegisterIndex.IndexHL];

        public Register16 IX { get; } = new(0xffff);

        public byte IXH { get => IX.High; set => IX.High = value; }

        public byte IXL { get => IX.Low; set => IX.Low = value; }

        public Register16 IY { get; } = new(0xffff);

        public byte IYH { get => IY.High; set => IY.High = value; }

        public byte IYL { get => IY.Low; set => IY.Low = value; }

        // ** From the Z80 CPU User Manual
        // Memory Refresh(R) Register.The Z80 CPU contains a memory _refresh counter,
        // enabling dynamic memories to be used with the same ease as static memories.Seven bits
        // of this 8-bit register are automatically incremented after each instruction fetch.The eighth
        // bit remains as programmed, resulting from an LD R, A instruction. The data in the _refresh
        // counter is sent out on the lower portion of the address bus along with a _refresh control
        // signal while the CPU is decoding and executing the fetched instruction. This mode of _refresh
        // is transparent to the programmer and does not slow the CPU operation.The programmer
        // can load the R register for testing purposes, but this register is normally not used by the
        // programmer. During _refresh, the contents of the I Register are placed on the upper eight
        // bits of the address bus.
        public ref RefreshRegister REFRESH => ref _refresh;

        public ref PinLevel NMI => ref _nmiLine;

        public ref PinLevel M1 => ref _m1Line;

        // ** From the Z80 CPU User Manual
        // RFSH.Refresh(output, active Low). RFSH, together with MREQ, indicates that the lower
        // seven bits of the system’s address bus can be used as a _refresh address to the system’s
        // dynamic memories.
        public ref PinLevel RFSH => ref _rfshLine;

        public ref PinLevel MREQ => ref _mreqLine;

        public ref PinLevel IORQ => ref _iorqLine;

        public ref PinLevel RD => ref _rdLine;

        public ref PinLevel WR => ref _wrLine;

        private Register16 DisplacedAddress
        {
            get
            {
                var displacement = (_prefixDD ? IX : IY).Word + _displacement;
                MEMPTR.Word = (ushort)displacement;
                return MEMPTR;
            }
        }

        public void Exx() => _registerSet ^= 1;

        public void ExxAF() => _accumulatorFlagsSet ^= 1;

        public virtual void RaiseNMI()
        {
            if (NMI.Lowered())
            {
                OnRaisingNMI();
                NMI.Raise();
                OnRaisedNMI();
            }
        }

        public virtual void LowerNMI()
        {
            if (NMI.Raised())
            {
                OnLoweringNMI();
                NMI.Lower();
                OnLoweredNMI();
            }
        }

        public virtual void RaiseM1()
        {
            if (M1.Lowered())
            {
                OnRaisingM1();
                M1.Raise();
                OnRaisedM1();
            }
        }

        public virtual void LowerM1()
        {
            if (M1.Raised())
            {
                OnLoweringM1();
                M1.Lower();
                OnLoweredM1();
            }
        }

        public virtual void RaiseRFSH()
        {
            if (RFSH.Lowered())
            {
                OnRaisingRFSH();
                RFSH.Raise();
                OnRaisedRFSH();
            }
        }

        public virtual void LowerRFSH()
        {
            if (RFSH.Raised())
            {
                OnLoweringRFSH();
                RFSH.Lower();
                OnLoweredRFSH();
            }
        }

        public virtual void RaiseMREQ()
        {
            if (MREQ.Lowered())
            {
                OnRaisingMREQ();
                MREQ.Raise();
                OnRaisedMREQ();
            }
        }

        public virtual void LowerMREQ()
        {
            if (MREQ.Raised())
            {
                OnLoweringMREQ();
                MREQ.Lower();
                OnLoweredMREQ();
            }
        }

        public virtual void RaiseIORQ()
        {
            if (IORQ.Lowered())
            {
                OnRaisingIORQ();
                IORQ.Raise();
                OnRaisedIORQ();
            }
        }

        public virtual void LowerIORQ()
        {
            if (IORQ.Raised())
            {
                OnLoweringIORQ();
                IORQ.Lower();
                OnLoweredIORQ();
            }
        }

        public virtual void RaiseRD()
        {
            if (RD.Lowered())
            {
                OnRaisingRD();
                RD.Raise();
                OnRaisedRD();
            }
        }

        public virtual void LowerRD()
        {
            if (RD.Raised())
            {
                OnLoweringRD();
                RD.Lower();
                OnLoweredRD();
            }
        }

        public virtual void RaiseWR()
        {
            if (WR.Lowered())
            {
                OnRaisingWR();
                WR.Raise();
                OnRaisedWR();
            }
        }

        public virtual void LowerWR()
        {
            if (WR.Raised())
            {
                OnLoweringWR();
                WR.Lower();
                OnLoweredWR();
            }
        }

        public override void Execute()
        {
            var decoded = GetDecodedOpCode(OpCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            if (_prefixCB)
            {
                ExecuteCB(x, y, z);
            }
            else if (_prefixED)
            {
                ExecuteED(x, y, z, p, q);
            }
            else
            {
                ExecuteOther(x, y, z, p, q);
            }
        }

        public override void PoweredStep()
        {
            _displaced = _prefixCB = _prefixDD = _prefixED = _prefixFD = false;
            var handled = false;
            if (RESET.Lowered())
            {
                HandleRESET();
                handled = true;
            }
            else if (NMI.Lowered())
            {
                HandleNMI();
                handled = true;
            }
            else if (INT.Lowered())
            {
                RaiseINT();
                RaiseHALT();
                if (IFF1)
                {
                    HandleINT();
                    handled = true;
                }
            }
            else if (HALT.Lowered())
            {
                // ** From the Z80 CPU User Manual
                // When a software HALT instruction is executed, the CPU executes NOPs until an interrupt
                // is received(either a nonmaskable or a maskable interrupt while the interrupt flip-flop is
                // enabled). The two interrupt lines are sampled with the rising clock edge during each T4
                // state as depicted in Figure 11.If a nonmaskable interrupt is received or a maskable interrupt
                // is received and the interrupt enable flip-flop is set, then the HALT state is exited on
                // the next rising clock edge.The following cycle is an interrupt acknowledge cycle corresponding
                // to the type of interrupt that was received.If both are received at this time, then
                // the nonmaskable interrupt is acknowledged because it is the highest priority.The purpose
                // of executing NOP instructions while in the HALT state is to keep the memory _refresh signals
                // active.Each cycle in the HALT state is a normal M1(fetch) cycle except that the data
                // received from the memory is ignored and an NOP instruction is forced internally to the
                // CPU.The HALT acknowledge signal is active during this time indicating that the processor
                // is in the HALT state.
                _ = ReadInitialOpCode();
                Execute(0); // NOP
                handled = true;
            }

            if (!handled)
            {
                Execute(FetchInitialOpCode());
            }
        }

        protected override void OnRaisedPOWER()
        {
            RaiseM1();
            RaiseMREQ();
            RaiseIORQ();
            RaiseRD();
            RaiseWR();

            DisableInterrupts();
            IM = 0;

            REFRESH = new(0);
            IV = (byte)Mask.Eight;

            ExxAF();
            Exx();

            AF.Word = IX.Word = IY.Word = BC.Word = DE.Word = HL.Word = (ushort)Mask.Sixteen;

            _prefixCB = _prefixDD = _prefixED = _prefixFD = false;

            base.OnRaisedPOWER();
        }

        protected virtual void OnRaisingNMI() => RaisingNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedNMI() => RaisedNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringNMI() => LoweringNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredNMI() => LoweredNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingM1() => RaisingM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedM1()
        {
            ++REFRESH;
            RaisedM1?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLoweringM1() => LoweringM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredM1() => LoweredM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRFSH() => RaisingRFSH?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRFSH() => RaisedRFSH?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRFSH() => LoweringRFSH?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRFSH() => LoweredRFSH?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringMREQ() => LoweringMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredMREQ() => LoweredMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingMREQ() => RaisingMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedMREQ() => RaisedMREQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringIORQ() => LoweringIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredIORQ() => LoweredIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingIORQ() => RaisingIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedIORQ() => RaisedIORQ?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRD() => LoweringRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRD() => LoweredRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRD() => RaisingRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRD() => RaisedRD?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringWR() => LoweringWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredWR() => LoweredWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingWR() => RaisingWR?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedWR() => RaisedWR?.Invoke(this, EventArgs.Empty);

        protected override void MemoryWrite()
        {
            Tick(3);
            LowerMREQ();
            LowerWR();
            base.MemoryWrite();
            RaiseWR();
            RaiseMREQ();
        }

        protected override byte MemoryRead()
        {
            Tick(3);
            LowerMREQ();
            LowerRD();
            var returned = base.MemoryRead();
            RaiseRD();
            RaiseMREQ();
            return returned;
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            DisableInterrupts();
            IV = REFRESH = 0;
            SP.Word = AF.Word = (ushort)Mask.Sixteen;
            Tick(3);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            LowerM1();
            LowerIORQ();
            var data = Bus.Data;
            RaiseIORQ();
            RaiseM1();
            DisableInterrupts();
            Tick(5);
            switch (IM)
            {
                case 0: // i8080 equivalent
                    Execute(data);
                    break;
                case 1:
                    Tick();
                    Restart(7 << 3);   // 7 cycles
                    break;
                case 2:
                    Tick(7);
                    MEMPTR.Assign(data, IV);
                    Call(MEMPTR);
                    break;
                default:
                    throw new NotSupportedException("Invalid interrupt mode");
            }
        }

        protected override void Call(Register16 destination)
        {
            Tick();
            base.Call(destination);
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

        private void DisableInterrupts() => IFF1 = IFF2 = false;

        private void EnableInterrupts() => IFF1 = IFF2 = true;

        private Register16 HL2() => _prefixDD ? IX : _prefixFD ? IY : HL;

        private Register16 RP(int rp) => rp switch
        {
            0 => BC,
            1 => DE,
            2 => HL2(),
            3 => SP,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private Register16 RP2(int rp) => rp switch
        {
            0 => BC,
            1 => DE,
            2 => HL2(),
            3 => AF,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private ref byte R(int r, AccessLevel access = AccessLevel.ReadOnly)
        {
            switch (r)
            {
                case 0:
                    return ref B;
                case 1:
                    return ref C;
                case 2:
                    return ref D;
                case 3:
                    return ref E;
                case 4:
                    return ref HL2().High;
                case 5:
                    return ref HL2().Low;
                case 6:
                    Bus.Address.Assign(_displaced ? DisplacedAddress : HL);
                    if (access == AccessLevel.ReadOnly)
                    {
                        MemoryRead();
                    }
                    // Will need a post-MemoryWrite
                    return ref Bus.Data;
                case 7:
                    return ref A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void R(int r, byte value)
        {
            R(r, AccessLevel.WriteOnly) = value;
            if (r == 6)
                MemoryWrite();
        }

        private ref byte R2(int r)
        {
            switch (r)
            {
                case 0:
                    return ref B;
                case 1:
                    return ref C;
                case 2:
                    return ref D;
                case 3:
                    return ref E;
                case 4:
                    return ref H;
                case 5:
                    return ref L;
                case 6:
                    // N.B. Write not possible, when r == 6
                    MemoryRead(HL);
                    return ref Bus.Data;
                case 7:
                    return ref A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void ExecuteCB(int x, int y, int z)
        {
            var memoryZ = z == 6;
            var indirect = (!_displaced && memoryZ) || _displaced;
            var direct = !indirect;

            byte operand;
            if (_displaced)
            {
                Tick(2);
                operand = MemoryRead(DisplacedAddress);
            }
            else
            {
                operand = R(z);
            }

            var update = x != 1; // BIT does not update
            switch (x)
            {
                case 0: // rot[y] r[z]
                    operand = y switch
                    {
                        0 => RLC(operand),
                        1 => RRC(operand),
                        2 => RL(operand),
                        3 => RR(operand),
                        4 => SLA(operand),
                        5 => SRA(operand),
                        6 => SLL(operand),
                        7 => SRL(operand),
                        _ => throw new NotSupportedException("Invalid operation mode"),
                    };
                    F = AdjustSZP(F, operand);
                    break;
                case 1: // BIT y, r[z]
                    BIT(y, operand);
                    F = AdjustXY(F, direct ? operand : MEMPTR.High);
                    break;
                case 2: // RES y, r[z]
                    operand = RES(y, operand);
                    break;
                case 3: // SET y, r[z]
                    operand = SET(y, operand);
                    break;
                default:
                    throw new NotSupportedException("Invalid operation mode");
            }

            if (update)
            {
                Tick();
                if (_displaced)
                {
                    MemoryWrite(operand);
                    if (!memoryZ)
                    {
                        R2(z) = operand;
                    }
                }
                else
                {
                    R(z, operand);
                }
            }
        }

        private void ExecuteED(int x, int y, int z, int p, int q)
        {
            switch (x)
            {
                case 0:
                case 3: // Invalid instruction, equivalent to NONI followed by NOP
                    break;
                case 1:
                    switch (z)
                    {
                        case 0: // Input from port with 16-bit address
                            Bus.Address.Assign(BC);
                            MEMPTR.Assign(Bus.Address);
                            MEMPTR.Word++;
                            ReadPort();
                            if (y != 6)
                            {
                                R(y, AccessLevel.WriteOnly) = Bus.Data; // IN r[y],(C)
                            }

                            F = AdjustSZPXY(F, Bus.Data);
                            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
                            break;
                        case 1: // Output to port with 16-bit address
                            Bus.Address.Assign(BC);
                            MEMPTR.Assign(Bus.Address);
                            MEMPTR.Word++;
                            Bus.Data = y != 6 ? R(y) : (byte)0;
                            WritePort();
                            break;
                        case 2: // 16-bit add/subtract with carry
                            HL2().Assign(q switch
                            {
                                0 => SBC(HL2(), RP(p)), // SBC HL, rp[p]
                                1 => ADC(HL2(), RP(p)), // ADC HL, rp[p]
                                _ => throw new NotSupportedException("Invalid operation mode"),
                            });
                            break;
                        case 3: // Retrieve/store register pair from/to immediate address
                            FetchWordAddress();
                            switch (q)
                            {
                                case 0: // LD (nn), rp[p]
                                    SetWord(RP(p));
                                    break;
                                case 1: // LD rp[p], (nn)
                                    RP(p).Assign(GetWord());
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 4: // Negate accumulator
                            NEG();
                            break;
                        case 5: // Return from interrupt
                            switch (y)
                            {
                                case 1:
                                    RetI(); // RETI
                                    break;
                                default:
                                    RetN(); // RETN
                                    break;
                            }

                            break;
                        case 6: // Set interrupt mode
                            IM = y switch
                            {
                                0 or 1 or 4 or 5 => 0,
                                2 or 6 => 1,
                                3 or 7 => 2,
                                _ => throw new NotSupportedException("Invalid operation mode"),
                            };
                            break;
                        case 7: // Assorted ops
                            switch (y)
                            {
                                case 0: // LD I,A
                                    IV = A;
                                    break;
                                case 1: // LD R,A
                                    REFRESH = A;
                                    break;
                                case 2: // LD A,I
                                    F = AdjustSZXY(F, A = IV);
                                    F = ClearBit(F, StatusBits.NF | StatusBits.HC);
                                    F = SetBit(F, StatusBits.PF, IFF2);
                                    break;
                                case 3: // LD A,R
                                    F = AdjustSZXY(F, A = REFRESH);
                                    F = ClearBit(F, StatusBits.NF | StatusBits.HC);
                                    F = SetBit(F, StatusBits.PF, IFF2);
                                    break;
                                case 4: // RRD
                                    RRD();
                                    break;
                                case 5: // RLD
                                    RLD();
                                    break;
                                case 6: // NOP
                                case 7: // NOP
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
                                    LDI();
                                    break;
                                case 5: // LDD
                                    LDD();
                                    break;
                                case 6: // LDIR
                                    if (LDIR())
                                    {
                                        --PC.Word;
                                        MEMPTR.Assign(PC);
                                        --PC.Word;
                                    }

                                    Tick(7);
                                    break;
                                case 7: // LDDR
                                    if (LDDR())
                                    {
                                        --PC.Word;
                                        MEMPTR.Assign(PC);
                                        --PC.Word;
                                    }

                                    Tick(7);
                                    break;
                            }

                            break;
                        case 1: // CP
                            switch (y)
                            {
                                case 4: // CPI
                                    CPI();
                                    break;
                                case 5: // CPD
                                    CPD();
                                    break;
                                case 6: // CPIR
                                    if (CPIR())
                                    {
                                        --PC.Word;
                                        MEMPTR.Assign(PC);
                                        --PC.Word;
                                        Tick(5);
                                    }

                                    Tick(5);
                                    break;
                                case 7: // CPDR
                                    if (CPDR())
                                    {
                                        --PC.Word;
                                        MEMPTR.Assign(PC);
                                        --PC.Word;
                                        Tick(3);
                                    }
                                    else
                                    {
                                        MEMPTR.Word = (ushort)(PC.Word - 2);
                                    }

                                    Tick(7);
                                    break;
                            }

                            break;
                        case 2: // IN
                            switch (y)
                            {
                                case 4: // INI
                                    INI();
                                    break;
                                case 5: // IND
                                    IND();
                                    break;
                                case 6: // INIR
                                    if (INIR())
                                    {
                                        PC.Word -= 2;
                                        Tick(5);
                                    }

                                    break;
                                case 7: // INDR
                                    if (INDR())
                                    {
                                        PC.Word -= 2;
                                        Tick(5);
                                    }

                                    break;
                            }

                            break;
                        case 3: // OUT
                            switch (y)
                            {
                                case 4: // OUTI
                                    OUTI();
                                    break;
                                case 5: // OUTD
                                    OUTD();
                                    break;
                                case 6: // OTIR
                                    if (OTIR())
                                    {
                                        PC.Word -= 2;
                                        Tick(5);
                                    }

                                    Tick(3);
                                    break;
                                case 7: // OTDR
                                    if (OTDR())
                                    {
                                        PC.Word -= 2;
                                        Tick(5);
                                    }

                                    Tick(3);
                                    break;
                            }

                            break;
                    }

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
                                    break;
                                case 1: // EX AF AF'
                                    ExxAF();
                                    break;
                                case 2: // DJNZ d
                                    Tick();
                                    if (JumpRelativeConditional(--B != 0))
                                    {
                                        Tick(2);
                                    }

                                    Tick(3);
                                    break;
                                case 3: // JR d
                                    JumpRelative((sbyte)FetchByte());
                                    break;
                                case 4: // JR cc,d
                                case 5:
                                case 6:
                                case 7:
                                    JumpRelativeConditionalFlag(y - 4);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    RP(p).Assign(FetchWord());
                                    break;
                                case 1: // ADD HL,rp
                                    HL2().Assign(Add(HL2(), RP(p)));
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
                                            Bus.Address.Assign(BC);
                                            MEMPTR.Assign(Bus.Address);
                                            ++MEMPTR.Word;
                                            MEMPTR.High = Bus.Data = A;
                                            MemoryWrite();
                                            break;
                                        case 1: // LD (DE),A
                                            Bus.Address.Assign(DE);
                                            MEMPTR.Assign(Bus.Address);
                                            ++MEMPTR.Word;
                                            MEMPTR.High = Bus.Data = A;
                                            MemoryWrite();
                                            break;
                                        case 2: // LD (nn),HL
                                            FetchWordAddress();
                                            SetWord(HL2());
                                            break;
                                        case 3: // LD (nn),A
                                            FetchWordMEMPTR();
                                            Bus.Address.Assign(MEMPTR);
                                            ++MEMPTR.Word;
                                            MEMPTR.High = Bus.Data = A;
                                            MemoryWrite();
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            Bus.Address.Assign(BC);
                                            MEMPTR.Assign(Bus.Address);
                                            ++MEMPTR.Word;
                                            A = MemoryRead();
                                            break;
                                        case 1: // LD A,(DE)
                                            Bus.Address.Assign(DE);
                                            MEMPTR.Assign(Bus.Address);
                                            ++MEMPTR.Word;
                                            A = MemoryRead();
                                            break;
                                        case 2: // LD HL,(nn)
                                            FetchWordAddress();
                                            HL2().Assign(GetWord());
                                            break;
                                        case 3: // LD A,(nn)
                                            FetchWordMEMPTR();
                                            Bus.Address.Assign(MEMPTR);
                                            ++MEMPTR.Word;
                                            A = MemoryRead();
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
                                    ++RP(p).Word;
                                    break;
                                case 1: // DEC rp
                                    --RP(p).Word;
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 4: // 8-bit INC
                            {
                                if (memoryY && _displaced)
                                {
                                    FetchDisplacement();
                                    Tick(5);
                                }

                                var original = R(y);
                                Tick();
                                R(y, Increment(original));
                                break;
                            }

                        case 5: // 8-bit DEC
                            {
                                if (memoryY && _displaced)
                                {
                                    FetchDisplacement();
                                    Tick(5);
                                }

                                var original = R(y);
                                Tick();
                                R(y, Decrement(original));
                                break;
                            }

                        case 6: // 8-bit load immediate
                            {
                                if (memoryY && _displaced)
                                {
                                    FetchDisplacement();
                                }

                                var value = FetchByte();
                                if (_displaced)
                                {
                                    Tick(2);
                                }

                                R(y, value);  // LD r,n
                                break;
                            }

                        case 7: // Assorted operations on accumulator/flags
                            switch (y)
                            {
                                case 0:
                                    A = RLC(A);
                                    break;
                                case 1:
                                    A = RRC(A);
                                    break;
                                case 2:
                                    A = RL(A);
                                    break;
                                case 3:
                                    A = RR(A);
                                    break;
                                case 4:
                                    DAA();
                                    break;
                                case 5:
                                    CPL();
                                    break;
                                case 6:
                                    SCF();
                                    break;
                                case 7:
                                    CCF();
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
                    if (!(memoryZ && memoryY))
                    {
                        var normal = true;
                        if (_displaced)
                        {
                            if (memoryZ || memoryY)
                            {
                                FetchDisplacement();
                            }

                            if (memoryZ)
                            {
                                switch (y)
                                {
                                    case 4:
                                        if (_displaced)
                                        {
                                            Tick(5);
                                        }

                                        H = R(z);
                                        normal = false;
                                        break;
                                    case 5:
                                        if (_displaced)
                                        {
                                            Tick(5);
                                        }

                                        L = R(z);
                                        normal = false;
                                        break;
                                }
                            }

                            if (memoryY)
                            {
                                switch (z)
                                {
                                    case 4:
                                        if (_displaced)
                                        {
                                            Tick(5);
                                        }

                                        R(y, H);
                                        normal = false;
                                        break;
                                    case 5:
                                        if (_displaced)
                                        {
                                            Tick(5);
                                        }

                                        R(y, L);
                                        normal = false;
                                        break;
                                }
                            }
                        }

                        if (normal)
                        {
                            if (_displaced)
                            {
                                Tick(5);
                            }

                            R(y, R(z));
                        }
                    }
                    else
                    {
                        LowerHALT(); // Exception (replaces LD (HL), (HL))
                    }

                    break;
                case 2:
                    { // Operate on accumulator and register/memory location
                        if (memoryZ && _displaced)
                        {
                            FetchDisplacement();
                            Tick(5);
                        }

                        var value = R(z);
                        switch (y)
                        {
                            case 0: // ADD A,r
                                A = Add(A, value);
                                break;
                            case 1: // ADC A,r
                                A = ADC(A, value);
                                break;
                            case 2: // SUB r
                                A = SUB(A, value);
                                break;
                            case 3: // SBC A,r
                                A = SBC(A, value);
                                break;
                            case 4: // AND r
                                AndR(value);
                                break;
                            case 5: // XOR r
                                XorR(value);
                                break;
                            case 6: // OR r
                                OrR(value);
                                break;
                            case 7: // CP r
                                Compare(value);
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
                            ReturnConditionalFlag(y);
                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    RP2(p).Assign(PopWord());
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            Return();
                                            break;
                                        case 1: // EXX
                                            Exx();
                                            break;
                                        case 2: // JP HL
                                            Jump(HL2());
                                            break;
                                        case 3: // LD SP,HL
                                            SP.Assign(HL2());
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
                            JumpConditionalFlag(y);
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    JumpIndirect();
                                    break;
                                case 1: // CB prefix
                                    _prefixCB = true;
                                    if (_displaced)
                                    {
                                        FetchDisplacement();
                                        Execute(FetchByte());
                                    }
                                    else
                                    {
                                        Execute(FetchInitialOpCode());
                                    }

                                    break;
                                case 2: // OUT (n),A
                                    WritePort(FetchByte());
                                    break;
                                case 3: // IN A,(n)
                                    A = ReadPort(FetchByte());
                                    break;
                                case 4: // EX (SP),HL
                                    XHTL(HL2());
                                    break;
                                case 5: // EX DE,HL
                                    {
                                        Intermediate.Assign(DE);
                                        DE.Assign(HL);
                                        HL.Assign(Intermediate);
                                    }
                                    break;
                                case 6: // DI
                                    DisableInterrupts();
                                    break;
                                case 7: // EI
                                    EnableInterrupts();
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 4: // Conditional call: CALL cc[y], nn
                            CallConditionalFlag(y);
                            break;
                        case 5: // PUSH & various ops
                            switch (q)
                            {
                                case 0: // PUSH rp2[p]
                                    Tick();
                                    PushWord(RP2(p));
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            CallIndirect();
                                            break;
                                        case 1: // DD prefix
                                            _displaced = _prefixDD = true;
                                            Execute(FetchInitialOpCode());
                                            break;
                                        case 2: // ED prefix
                                            _prefixED = true;
                                            Execute(FetchInitialOpCode());
                                            break;
                                        case 3: // FD prefix
                                            _displaced = _prefixFD = true;
                                            Execute(FetchInitialOpCode());
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
                                var operand = FetchByte();
                                switch (y)
                                {
                                    case 0: // ADD A,n
                                        A = Add(A, operand);
                                        break;
                                    case 1: // ADC A,n
                                        A = ADC(A, operand);
                                        break;
                                    case 2: // SUB n
                                        A = SUB(A, operand);
                                        break;
                                    case 3: // SBC A,n
                                        A = SBC(A, operand);
                                        break;
                                    case 4: // AND n
                                        AndR(operand);
                                        break;
                                    case 5: // XOR n
                                        XorR(operand);
                                        break;
                                    case 6: // OR n
                                        OrR(operand);
                                        break;
                                    case 7: // CP n
                                        Compare(operand);
                                        break;
                                    default:
                                        throw new NotSupportedException("Invalid operation mode");
                                }

                                break;
                            }

                        case 7: // Restart: RST y * 8
                            Restart((byte)(y << 3));
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
            }
        }

        private void HandleNMI()
        {
            RaiseNMI();
            RaiseHALT();
            IFF2 = IFF1;
            IFF1 = false;
            LowerM1();
            _ = Bus.Data;
            RaiseM1();
            Restart(0x66);
        }

        private void FetchDisplacement() => _displacement = (sbyte)FetchByte();

        private byte FetchInitialOpCode()
        {
            var returned = ReadInitialOpCode();
            ++PC.Word;
            return returned;
        }

        // ** From the Z80 CPU User Manual
        // Figure 5 depicts the timing during an M1 (op code fetch) cycle. The Program Counter is
        // placed on the address bus at the beginning of the M1 cycle. One half clock cycle later, the
        // MREQ signal goes active. At this time, the address to memory has had time to stabilize so
        // that the falling edge of MREQ can be used directly as a chip enable clock to dynamic
        // memories. The RD line also goes active to indicate that the memory read data should be
        // enabled onto the CPU data bus. The CPU samples the data from the memory space on the
        // data bus with the rising edge of the clock of state T3, and this same edge is used by the
        // CPU to turn off the RD and MREQ signals. As a result, the data is sampled by the CPU
        // before the RD signal becomes inactive. Clock states T3 and T4 of a fetch cycle are used to
        // _refresh dynamic memories. The CPU uses this time to decode and execute the fetched
        // instruction so that no other concurrent operation can be performed.
        private byte ReadInitialOpCode()
        {
            Tick();
            LowerM1();
            var returned = MemoryRead(PC);
            RaiseM1();
            Bus.Address.Assign(REFRESH, IV);
            LowerRFSH();
            LowerMREQ();
            RaiseMREQ();
            RaiseRFSH();
            return returned;
        }

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            Intermediate.Word = (ushort)(operand - value - carry);
            var result = Intermediate.Low;

            F = AdjustHalfCarrySub(F, operand, value, result);
            F = AdjustOverflowSub(F, operand, value, result);

            F = SetBit(F, StatusBits.NF);
            F = SetBit(F, StatusBits.CF, Intermediate.High & (byte)StatusBits.CF);
            F = AdjustSZ(F, result);

            return result;
        }

        private byte Increment(byte operand)
        {
            F = ClearBit(F, StatusBits.NF);
            var result = ++operand;
            F = AdjustSZXY(F, result);
            F = SetBit(F, StatusBits.VF, result == (byte)Bits.Bit7);
            F = ClearBit(F, StatusBits.HC, LowNibble(result));
            return result;
        }

        private byte Decrement(byte operand)
        {
            F = SetBit(F, StatusBits.NF);
            F = ClearBit(F, StatusBits.HC, LowNibble(operand));
            var result = --operand;
            F = AdjustSZXY(F, result);
            F = SetBit(F, StatusBits.VF, result == (byte)Mask.Seven);
            return result;
        }

        private void RetN()
        {
            Return();
            IFF1 = IFF2;
        }

        private void RetI() => RetN();

        private bool ConvertCondition(int flag) => flag switch
        {
            0 => (F & (byte)StatusBits.ZF) == 0,
            1 => (F & (byte)StatusBits.ZF) != 0,
            2 => (F & (byte)StatusBits.CF) == 0,
            3 => (F & (byte)StatusBits.CF) != 0,
            4 => (F & (byte)StatusBits.PF) == 0,
            5 => (F & (byte)StatusBits.PF) != 0,
            6 => (F & (byte)StatusBits.SF) == 0,
            7 => (F & (byte)StatusBits.SF) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        private void ReturnConditionalFlag(int flag)
        {
            if (ConvertCondition(flag))
            {
                Tick();
                Return();
            }
        }

        private void JumpRelativeConditionalFlag(int flag) => JumpRelativeConditional(ConvertCondition(flag));

        private void JumpConditionalFlag(int flag) => JumpConditional(ConvertCondition(flag));

        private void CallConditionalFlag(int flag) => CallConditional(ConvertCondition(flag));

        private Register16 SBC(Register16 operand, Register16 value)
        {
            var subtraction = operand.Word - value.Word - (F & (byte)StatusBits.CF);
            Intermediate.Word = (ushort)subtraction;

            F = SetBit(F, StatusBits.NF);
            F = ClearBit(F, StatusBits.ZF, Intermediate.Word);
            F = SetBit(F, StatusBits.CF, subtraction & (int)Bits.Bit16);
            F = AdjustHalfCarrySub(F, operand.High, value.High, Intermediate.High);
            F = AdjustXY(F, Intermediate.High);

            var beforeNegative = operand.High & (byte)StatusBits.SF;
            var valueNegative = value.High & (byte)StatusBits.SF;
            var afterNegative = Intermediate.High & (byte)StatusBits.SF;

            F = SetBit(F, StatusBits.SF, afterNegative);
            F = AdjustOverflowSub(F, beforeNegative, valueNegative, afterNegative);

            MEMPTR.Word = (ushort)(operand.Word + 1);

            return Intermediate;
        }

        private Register16 ADC(Register16 operand, Register16 value)
        {
            Add(operand, value, F & (byte)StatusBits.CF); // Leaves result in intermediate anyway
            F = ClearBit(F, StatusBits.ZF, Intermediate.Word);

            var beforeNegative = operand.High & (byte)StatusBits.SF;
            var valueNegative = value.High & (byte)StatusBits.SF;
            var afterNegative = Intermediate.High & (byte)StatusBits.SF;

            F = SetBit(F, StatusBits.SF, afterNegative);
            F = AdjustOverflowAdd(F, beforeNegative, valueNegative, afterNegative);

            return Intermediate;
        }

        private Register16 Add(Register16 operand, Register16 value, int carry = 0)
        {
            var addition = operand.Word + value.Word + carry;
            Intermediate.Word = (ushort)addition;

            F = ClearBit(F, StatusBits.NF);
            F = SetBit(F, StatusBits.CF, addition & (int)Bits.Bit16);
            F = AdjustHalfCarryAdd(F, operand.High, value.High, Intermediate.High);
            F = AdjustXY(F, Intermediate.High);

            MEMPTR.Word = (ushort)(operand.Word + 1);

            return Intermediate;
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            Intermediate.Word = (ushort)(operand + value + carry);
            var result = Intermediate.Low;

            F = AdjustHalfCarryAdd(F, operand, value, result);
            F = AdjustOverflowAdd(F, operand, value, result);

            F = ClearBit(F, StatusBits.NF);
            F = SetBit(F, StatusBits.CF, Intermediate.High & (byte)StatusBits.CF);
            F = AdjustSZXY(F, result);

            return result;
        }

        private byte ADC(byte operand, byte value) => Add(operand, value, F & (byte)StatusBits.CF);

        private byte SUB(byte operand, byte value, int carry = 0)
        {
            var subtraction = Subtract(operand, value, carry);
            F = AdjustXY(F, subtraction);
            return subtraction;
        }

        private byte SBC(byte operand, byte value) => SUB(operand, value, F & (byte)StatusBits.CF);

        private void AndR(byte value)
        {
            F = SetBit(F, StatusBits.HC);
            F = ClearBit(F, StatusBits.CF | StatusBits.NF);
            F = AdjustSZPXY(F, A &= value);
        }

        private void XorR(byte value)
        {
            F = ClearBit(F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            F = AdjustSZPXY(F, A ^= value);
        }

        private void OrR(byte value)
        {
            F = ClearBit(F, StatusBits.HC | StatusBits.CF | StatusBits.NF);
            F = AdjustSZPXY(F, A |= value);
        }

        private void Compare(byte value)
        {
            Subtract(A, value);
            F = AdjustXY(F, value);
        }

        private byte RLC(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit7;
            F = SetBit(F, StatusBits.CF, carry);
            var result = (byte)((operand << 1) | (carry >> 7));
            F = AdjustXY(F, result);
            return result;
        }

        private byte RRC(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit0;
            F = SetBit(F, StatusBits.CF, carry);
            var result = (byte)((operand >> 1) | (carry << 7));
            F = AdjustXY(F, result);
            return result;
        }

        private byte RL(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            var carry = F & (byte)StatusBits.CF;
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | carry);
            F = AdjustXY(F, result);
            return result;
        }

        private byte RR(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            var carry = F & (byte)StatusBits.CF;
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (carry << 7));
            F = AdjustXY(F, result);
            return result;
        }

        private byte SLA(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)(operand << 1);
            F = AdjustXY(F, result);
            return result;
        }

        private byte SRA(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (operand & (byte)Bits.Bit7));
            F = AdjustXY(F, result);
            return result;
        }

        private byte SLL(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | (byte)Bits.Bit0);
            F = AdjustXY(F, result);
            return result;
        }

        private byte SRL(byte operand)
        {
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            F = SetBit(F, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) & ~(byte)Bits.Bit7);
            F = AdjustXY(F, result);
            F = SetBit(F, StatusBits.ZF, result);
            return result;
        }

        private void BIT(int n, byte operand)
        {
            F = SetBit(F, StatusBits.HC);
            F = ClearBit(F, StatusBits.NF);
            var discarded = (byte)(operand & Bit(n));
            F = AdjustSZ(F, discarded);
            F = ClearBit(F, StatusBits.PF, discarded);
        }

        private void DAA()
        {
            var updated = A;

            var lowAdjust = ((F & (byte)StatusBits.HC) != 0) || (LowNibble(A) > 9);
            var highAdjust = ((F & (byte)StatusBits.CF) != 0) || (A > 0x99);

            if ((F & (byte)StatusBits.NF) != 0)
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

            F = (byte)((F & (byte)(StatusBits.CF | StatusBits.NF)) | (A > 0x99 ? (byte)StatusBits.CF : 0) | ((A ^ updated) & (byte)StatusBits.HC));

            F = AdjustSZPXY(F, A = updated);
        }

        private void SCF()
        {
            F = SetBit(F, StatusBits.CF);
            F = ClearBit(F, StatusBits.HC | StatusBits.NF);
            F = AdjustXY(F, A);
        }

        private void CCF()
        {
            F = ClearBit(F, StatusBits.NF);
            var carry = F & (byte)StatusBits.CF;
            F = SetBit(F, StatusBits.HC, carry);
            F = ClearBit(F, StatusBits.CF, carry);
            F = AdjustXY(F, A);
        }

        private void CPL()
        {
            F = SetBit(F, StatusBits.HC | StatusBits.NF);
            F = AdjustXY(F, A = (byte)~A);
        }

        private void XHTL(Register16 exchange)
        {
            MEMPTR.Low = MemoryRead(SP);
            ++Bus.Address.Word;
            MEMPTR.High = MemoryRead();
            Tick();
            MemoryWrite(exchange.High);
            exchange.High = MEMPTR.High;
            --Bus.Address.Word;
            MemoryWrite(exchange.Low);
            exchange.Low = MEMPTR.Low;
        }

        private void BlockCompare(Register16 source, ushort counter)
        {
            var value = MemoryRead(source);
            var result = (byte)(A - value);

            F = SetBit(F, StatusBits.PF, counter);

            F = AdjustSZ(F, result);
            F = AdjustHalfCarrySub(F, A, value, result);
            F = SetBit(F, StatusBits.NF);

            result -= (byte)((F & (byte)StatusBits.HC) >> 4);

            F = SetBit(F, StatusBits.YF, result & (byte)Bits.Bit1);
            F = SetBit(F, StatusBits.XF, result & (byte)Bits.Bit3);
        }

        private void CPI()
        {
            BlockCompare(HL, --BC.Word);
            ++HL.Word;
            ++MEMPTR.Word;
        }

        private bool CPIR()
        {
            CPI();
            return ((F & (byte)StatusBits.PF) != 0) && ((F & (byte)StatusBits.ZF) == 0); // See CPI
        }

        private void CPD()
        {
            BlockCompare(HL, --BC.Word);
            --HL.Word;
            --MEMPTR.Word;
        }

        private bool CPDR()
        {
            CPD();
            return ((F & (byte)StatusBits.PF) != 0) && ((F & (byte)StatusBits.ZF) == 0); // See CPD
        }

        private void BlockLoad(Register16 source, Register16 destination, ushort counter)
        {
            var value = MemoryRead(source);
            MemoryWrite(destination);
            var xy = A + value;
            F = SetBit(F, StatusBits.XF, xy & (int)Bits.Bit3);
            F = SetBit(F, StatusBits.YF, xy & (int)Bits.Bit1);
            F = ClearBit(F, StatusBits.NF | StatusBits.HC);
            F = SetBit(F, StatusBits.PF, counter);
        }

        private void LDI()
        {
            BlockLoad(HL, DE, --BC.Word);
            ++HL.Word;
            ++DE.Word;
        }

        private bool LDIR()
        {
            this.LDI();
            return (this.F & (byte)StatusBits.PF) != 0; // See LDI
        }

        private void LDD()
        {
            this.BlockLoad(this.HL, this.DE, --this.BC.Word);
            --this.HL.Word;
            --this.DE.Word;
        }

        private bool LDDR()
        {
            this.LDD();
            return (this.F & (byte)StatusBits.PF) != 0; // See LDD
        }

        private void BlockIn(Register16 source, Register16 destination)
        {
            this.Bus.Address.Assign(source);
            this.MEMPTR.Assign(Bus.Address);
            this.Tick();
            this.ReadPort();
            this.Tick(3);
            this.MemoryWrite(destination);
            source.High = this.Decrement(source.High);
            this.F = SetBit(this.F, StatusBits.NF);
        }

        private void INI()
        {
            this.BlockIn(this.BC, this.HL);
            ++this.HL.Word;
            ++this.MEMPTR.Word;
        }

        private bool INIR()
        {
            this.INI();
            return (this.F & (byte)StatusBits.ZF) == 0; // See INI
        }

        private void IND()
        {
            this.BlockIn(this.BC, this.HL);
            --this.HL.Word;
            --this.MEMPTR.Word;
        }

        private bool INDR()
        {
            this.IND();
            return (this.F & (byte)StatusBits.ZF) == 0; // See IND
        }

        private void BlockOut(Register16 source, Register16 destination)
        {
            this.Tick();
            this.MemoryRead(source);
            destination.High = this.Decrement(destination.High);
            this.Bus.Address.Assign(destination);
            this.WritePort();
            this.MEMPTR.Assign(destination);
        }

        private void AdjustBlockOutFlags()
        {
            // HL needs to have been incremented or decremented prior to this call
            var value = this.Bus.Data;
            this.F = SetBit(this.F, StatusBits.NF, value & (byte)Bits.Bit7);
            this.F = SetBit(this.F, StatusBits.HC | StatusBits.CF, (this.L + value) > 0xff);
            this.F = AdjustParity(this.F, (byte)(((value + this.L) & (int)Mask.Three) ^ this.B));
        }

        private void OUTI()
        {
            this.BlockOut(this.HL, this.BC);
            ++this.HL.Word;
            this.AdjustBlockOutFlags();
            ++this.MEMPTR.Word;
        }

        private bool OTIR()
        {
            this.OUTI();
            return (this.F & (byte)StatusBits.ZF) == 0; // See OUTI
        }

        private void OUTD()
        {
            this.BlockOut(this.HL, this.BC);
            --this.HL.Word;
            this.AdjustBlockOutFlags();
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
            this.Bus.Address.Assign(this.HL);
            this.MEMPTR.Assign(this.Bus.Address);
            ++this.MEMPTR.Word;
            var memory = this.MemoryRead();
            this.Tick(4);
            this.MemoryWrite((byte)(PromoteNibble(this.A) | HighNibble(memory)));
            this.A = (byte)(HigherNibble(this.A) | LowerNibble(memory));
            this.F = AdjustSZPXY(this.F, this.A);
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
        }

        private void RLD()
        {
            this.Bus.Address.Assign(this.HL);
            this.MEMPTR.Assign(this.Bus.Address);
            ++this.MEMPTR.Word;
            var memory = this.MemoryRead();
            this.Tick(4);
            this.MemoryWrite((byte)(PromoteNibble(memory) | LowNibble(this.A)));
            this.A = (byte)(HigherNibble(this.A) | HighNibble(memory));
            this.F = AdjustSZPXY(this.F, this.A);
            this.F = ClearBit(this.F, StatusBits.NF | StatusBits.HC);
        }

        private void WritePort(byte port)
        {
            this.Bus.Address.Assign(port, this.Bus.Data = this.A);
            this.MEMPTR.Assign(this.Bus.Address);
            this.WritePort();
            ++this.MEMPTR.Low;
        }

        private void WritePort()
        {
            this.Tick();
            this.LowerIORQ();
            this.LowerWR();
            this._ports.Write(this.Bus.Address.Low, this.Bus.Data);
            this.RaiseWR();
            this.RaiseIORQ();
        }

        private byte ReadPort(byte port)
        {
            this.Bus.Address.Assign(port, this.Bus.Data = this.A);
            this.MEMPTR.Assign(this.Bus.Address);
            ++this.MEMPTR.Low;
            return this.ReadPort();
        }

        private byte ReadPort()
        {
            this.Tick();
            this.LowerIORQ();
            this.LowerRD();
            var returned = this.Bus.Data = this._ports.Read(this.Bus.Address.Low);
            this.RaiseRD();
            this.RaiseIORQ();
            return returned;
        }
    }
}
