// <copyright file="Core.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

//using EightBit;

namespace M6502
{
    using EightBit;

    public abstract class Core(Bus bus) : LittleEndianProcessor(bus)
    {
        #region Pin controls

        #region NMI pin

        public ref PinLevel NMI => ref nmiLine;
        private PinLevel nmiLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingNMI;
        public event EventHandler<EventArgs>? RaisedNMI;
        public event EventHandler<EventArgs>? LoweringNMI;
        public event EventHandler<EventArgs>? LoweredNMI;
        protected virtual void OnRaisingNMI() => RaisingNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedNMI() => RaisedNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringNMI() => LoweringNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredNMI() => LoweredNMI?.Invoke(this, EventArgs.Empty);


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
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

        #endregion

        #region SO pin

        public ref PinLevel SO => ref soLine;
        private PinLevel soLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingSO;
        public event EventHandler<EventArgs>? RaisedSO;
        public event EventHandler<EventArgs>? LoweringSO;
        public event EventHandler<EventArgs>? LoweredSO;

        protected virtual void OnRaisingSO() => RaisingSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSO() => RaisedSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringSO() => LoweringSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSO() => LoweredSO?.Invoke(this, EventArgs.Empty);


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseSO()
        {
            if (SO.Lowered())
            {
                OnRaisingSO();
                SO.Raise();
                OnRaisedSO();
            }
        }

        public virtual void LowerSO()
        {
            if (SO.Raised())
            {
                OnLoweringSO();
                SO.Lower();
                OnLoweredSO();
            }
        }

        #endregion

        #region SYNC pin

        public ref PinLevel SYNC => ref syncLine;
        private PinLevel syncLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingSYNC;
        public event EventHandler<EventArgs>? RaisedSYNC;
        public event EventHandler<EventArgs>? LoweringSYNC;
        public event EventHandler<EventArgs>? LoweredSYNC;

        protected virtual void OnRaisingSYNC() => RaisingSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSYNC() => RaisedSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringSYNC() => LoweringSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSYNC() => LoweredSYNC?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        protected virtual void RaiseSYNC()
        {
            OnRaisingSYNC();
            SYNC.Raise();
            OnRaisedSYNC();
        }

        protected virtual void LowerSYNC()
        {
            OnLoweringSYNC();
            SYNC.Lower();
            OnLoweredSYNC();
        }

        #endregion

        #region RDY pin

        public ref PinLevel RDY => ref rdyLine;
        private PinLevel rdyLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingRDY;
        public event EventHandler<EventArgs>? RaisedRDY;
        public event EventHandler<EventArgs>? LoweringRDY;
        public event EventHandler<EventArgs>? LoweredRDY;
        protected virtual void OnRaisingRDY() => RaisingRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedRDY() => RaisedRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringRDY() => LoweringRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredRDY() => LoweredRDY?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRDY()
        {
            if (RDY.Lowered())
            {
                OnRaisingRDY();
                RDY.Raise();
                OnRaisedRDY();
            }
        }

        public virtual void LowerRDY()
        {
            if (RDY.Raised())
            {
                OnLoweringRDY();
                RDY.Lower();
                OnLoweredRDY();
            }
        }

        #endregion

        #region RW pin

        public ref PinLevel RW => ref rwLine;
        private PinLevel rwLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingRW;
        public event EventHandler<EventArgs>? RaisedRW;
        public event EventHandler<EventArgs>? LoweringRW;
        public event EventHandler<EventArgs>? LoweredRW;
        protected virtual void OnRaisingRW() => RaisingRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedRW() => RaisedRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringRW() => LoweringRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredRW() => LoweredRW?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRW()
        {
            if (RW.Lowered())
            {
                OnRaisingRW();
                RW.Raise();
                OnRaisedRW();
            }
        }

        public virtual void LowerRW()
        {
            if (RW.Raised())
            {
                OnLoweringRW();
                RW.Lower();
                OnLoweredRW();
            }
        }

        #endregion

        protected override void OnRaisedPOWER()
        {
            X = (byte)Bits.Bit7;
            Y = 0;
            A = 0;
            P = (byte)StatusBits.RF;
            S = (byte)Mask.Eight;
            LowerSYNC();
            LowerRW();
            base.OnRaisedPOWER();
        }

        #endregion

        #region Interrupts

        private const byte IRQvector = 0xfe;  // IRQ vector
        private const byte RSTvector = 0xfc;  // RST vector
        private const byte NMIvector = 0xfa;  // NMI vector

        protected enum InterruptSource { hardware, software };

        protected enum InterruptType { reset, nonReset };

        protected virtual void Interrupt(byte vector, InterruptSource source = InterruptSource.hardware, InterruptType type = InterruptType.nonReset)
        {
            if (type == InterruptType.reset)
            {
                DummyPush();
                DummyPush();
                DummyPush();
            }
            else
            {
                PushWord(PC);
                Push((byte)(P | (source == InterruptSource.hardware ? 0 : (byte)StatusBits.BF)));
            }
            SetFlag(StatusBits.IF);   // Disable IRQ
            Jump(GetWordPaged(0xff, vector));
        }

        #region Interrupt etc. handlers

        protected override sealed void HandleRESET()
        {
            RaiseRESET();
            Interrupt(RSTvector, InterruptSource.hardware, InterruptType.reset);
        }

        protected override sealed void HandleINT()
        {
            RaiseINT();
            Interrupt(IRQvector);
        }

        private void HandleNMI()
        {
            RaiseNMI();
            Interrupt(NMIvector);
        }

        private void HandleSO()
        {
            RaiseSO();
            SetFlag(StatusBits.VF);
        }

        #endregion

        #endregion

        #region Registers

        public byte X { get; set; }
        public byte Y { get; set; }
        public byte A { get; set; }
        public byte S { get; set; }
        public byte P { get; set; }

        #endregion

        #region Processor state helpers

        protected int InterruptMasked => P & (byte)StatusBits.IF;
        protected int DecimalMasked => P & (byte)StatusBits.DF;
        protected int Negative => NegativeTest(P);
        protected int Zero => ZeroTest(P);
        protected int Overflow => OverflowTest(P);
        protected int Carry => CarryTest(P);

        protected static int NegativeTest(byte data) => data & (byte)StatusBits.NF;
        protected static int ZeroTest(byte data) => data & (byte)StatusBits.ZF;
        protected static int OverflowTest(byte data) => data & (byte)StatusBits.VF;
        protected static int CarryTest(byte data) => data & (byte)StatusBits.CF;

        #endregion

        #region Bit/state twiddling

        #region Bit twiddling

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        #endregion

        #region State flag twiddling

        protected void SetFlag(StatusBits flag)
        {
            P = SetBit(P, flag);
        }

        protected void SetFlag(StatusBits which, int condition)
        {
            P = SetBit(P, which, condition);
        }

        protected void SetFlag(StatusBits which, bool condition)
        {
            P = SetBit(P, which, condition);
        }

        protected void ResetFlag(StatusBits which)
        {
            P = ClearBit(P, which);
        }

        protected void ResetFlag(StatusBits which, int condition)
        {
            P = ClearBit(P, which, condition);
        }

        #endregion

        #endregion

        #region Cycle wastage

        protected void SwallowRead() => MemoryRead(PC);

        protected void SwallowPop() => MemoryRead(S, 1);

        protected void SwallowFetch() => FetchByte();

        #endregion

        #region Core instruction dispatching

        public override void Execute()
        {
            MaybeExecute();
        }

        protected virtual bool MaybeExecute()
        {
            var cycles = Cycles;
            switch (OpCode)
            {
                case 0x00: SwallowFetch(); Interrupt(IRQvector, InterruptSource.software); break; // BRK (implied)
                case 0x01: IndexedIndirectXRead(); OrR(); break;                                  // ORA (indexed indirect X)
                case 0x05: ZeroPageRead(); OrR(); break;                                          // ORA (zero page)
                case 0x06: ZeroPageRead(); ModifyWrite(ASL()); break;                        // ASL (zero page)
                case 0x08: SwallowRead(); PHP(); break;                                           // PHP (implied)
                case 0x09: ImmediateRead(); OrR(); break;                                         // ORA (immediate)
                case 0x0a: SwallowRead(); A = ASL(A); break;                                      // ASL A (implied)
                case 0x0d: AbsoluteRead(); OrR(); break;                                          // ORA (absolute)
                case 0x0e: AbsoluteRead(); ModifyWrite(ASL()); break;                        // ASL (absolute)

                case 0x10: BranchNot(Negative); break;                                            // BPL (relative)
                case 0x11: IndirectIndexedYRead(); OrR(); break;                                  // ORA (indirect indexed Y)
                case 0x15: ZeroPageXRead(); OrR(); break;                                         // ORA (zero page, X)
                case 0x16: ZeroPageXRead(); ModifyWrite(ASL()); break;                       // ASL (zero page, X)
                case 0x18: SwallowRead(); ResetFlag(StatusBits.CF); break;                        // CLC (implied)
                case 0x19: AbsoluteYRead(); OrR(); break;                                         // ORA (absolute, Y)
                case 0x1d: AbsoluteXRead(); OrR(); break;                                         // ORA (absolute, X)
                case 0x1e: AbsoluteXAddress(); FixupRead(); ModifyWrite(ASL()); break;  // ASL (absolute, X)

                case 0x20: JSR(); break;                                                               // JSR (absolute)
                case 0x21: IndexedIndirectXRead(); AndR(); break;                                 // AND (indexed indirect X)
                case 0x24: ZeroPageRead(); BIT(); break;                                          // BIT (zero page)
                case 0x25: ZeroPageRead(); AndR(); break;                                         // AND (zero page)
                case 0x26: ZeroPageRead(); ModifyWrite(ROL()); break;                        // ROL (zero page)
                case 0x28: SwallowRead(); PLP(); break;                                           // PLP (implied)
                case 0x29: ImmediateRead(); AndR(); break;                                        // AND (immediate)
                case 0x2a: SwallowRead(); A = ROL(A); break;                            // ROL A (implied)
                case 0x2c: AbsoluteRead(); BIT(); break;                                          // BIT (absolute)
                case 0x2d: AbsoluteRead(); AndR(); break;                                         // AND (absolute)
                case 0x2e: AbsoluteRead(); ModifyWrite(ROL()); break;                        // ROL (absolute)

                case 0x30: Branch(Negative); break;                                               // BMI (relative)
                case 0x31: IndirectIndexedYRead(); AndR(); break;                                 // AND (indirect indexed Y)
                case 0x35: ZeroPageXRead(); AndR(); break;                                        // AND (zero page, X)
                case 0x36: ZeroPageXRead(); ModifyWrite(ROL()); break;                       // ROL (zero page, X)
                case 0x38: SwallowRead(); SetFlag(StatusBits.CF); break;                          // SEC (implied)
                case 0x39: AbsoluteYRead(); AndR(); break;                                        // AND (absolute, Y)
                case 0x3d: AbsoluteXRead(); AndR(); break;                                        // AND (absolute, X)
                case 0x3e: AbsoluteXAddress(); FixupRead(); ModifyWrite(ROL()); break;  // ROL (absolute, X)

                case 0x40: SwallowRead(); RTI(); break;                                           // RTI (implied)
                case 0x41: IndexedIndirectXRead(); EorR(); break;                                 // EOR (indexed indirect X)
                case 0x44: ZeroPageRead(); break;                                                      // *NOP (zero page)
                case 0x45: ZeroPageRead(); EorR(); break;                                         // EOR (zero page)
                case 0x46: ZeroPageRead(); ModifyWrite(LSR()); break;                        // LSR (zero page)
                case 0x48: SwallowRead(); Push(A); break;                                    // PHA (implied)
                case 0x49: ImmediateRead(); EorR(); break;                                        // EOR (immediate)
                case 0x4a: SwallowRead(); A = LSR(A); break;                            // LSR A (implied)
                case 0x4c: AbsoluteAddress(); Jump(Bus.Address); break;                      // JMP (absolute)
                case 0x4d: AbsoluteRead(); EorR(); break;                                         // EOR (absolute)
                case 0x4e: AbsoluteRead(); ModifyWrite(LSR()); break;                        // LSR (absolute)

                case 0x50: BranchNot(Overflow); break;                                            // BVC (relative)
                case 0x51: IndirectIndexedYRead(); EorR(); break;                                 // EOR (indirect indexed Y)
                case 0x54: ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0x55: ZeroPageXRead(); EorR(); break;                                        // EOR (zero page, X)
                case 0x56: ZeroPageXRead(); ModifyWrite(LSR()); break;                       // LSR (zero page, X)
                case 0x58: SwallowRead(); ResetFlag(StatusBits.IF); break;                        // CLI (implied)
                case 0x59: AbsoluteYRead(); EorR(); break;                                        // EOR (absolute, Y)
                case 0x5d: AbsoluteXRead(); EorR(); break;                                        // EOR (absolute, X)
                case 0x5e: AbsoluteXAddress(); FixupRead(); ModifyWrite(LSR()); break;  // LSR (absolute, X)

                case 0x60: SwallowRead(); Return(); break;                                        // RTS (implied)
                case 0x61: IndexedIndirectXRead(); ADC(); break;                                  // ADC (indexed indirect X)
                case 0x65: ZeroPageRead(); ADC(); break;                                          // ADC (zero page)
                case 0x66: ZeroPageRead(); ModifyWrite(ROR()); break;                        // ROR (zero page)
                case 0x68: SwallowRead(); SwallowPop(); A = Through(Pop()); break; // PLA (implied)
                case 0x69: ImmediateRead(); ADC(); break;                                         // ADC (immediate)
                case 0x6a: SwallowRead(); A = ROR(A); break;                            // ROR A (implied)
                case 0x6c: IndirectAddress(); Jump(Bus.Address); break;                      // JMP (indirect)
                case 0x6d: AbsoluteRead(); ADC(); break;                                          // ADC (absolute)
                case 0x6e: AbsoluteRead(); ModifyWrite(ROR()); break;                        // ROR (absolute)

                case 0x70: Branch(Overflow); break;                                               // BVS (relative)
                case 0x71: IndirectIndexedYRead(); ADC(); break;                                  // ADC (indirect indexed Y)
                case 0x75: ZeroPageXRead(); ADC(); break;                                         // ADC (zero page, X)
                case 0x76: ZeroPageXRead(); ModifyWrite(ROR()); break;                       // ROR (zero page, X)
                case 0x78: SwallowRead(); SetFlag(StatusBits.IF); break;                          // SEI (implied)
                case 0x79: AbsoluteYRead(); ADC(); break;                                         // ADC (absolute, Y)
                case 0x7d: AbsoluteXRead(); ADC(); break;                                         // ADC (absolute, X)
                case 0x7e: AbsoluteXAddress(); FixupRead(); ModifyWrite(ROR()); break;	// ROR (absolute, X)

                case 0x81: IndexedIndirectXAddress(); MemoryWrite(A); break;                 // STA (indexed indirect X)
                case 0x82: ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0x84: ZeroPageAddress(); MemoryWrite(Y); break;                         // STY (zero page)
                case 0x85: ZeroPageAddress(); MemoryWrite(A); break;	                        // STA (zero page)
                case 0x86: ZeroPageAddress(); MemoryWrite(X); break;	                        // STX (zero page)
                case 0x88: SwallowRead(); Y = DEC(Y); break;	                        // DEY (implied)
                case 0x8a: SwallowRead(); A = Through(X); break;	                    // TXA (implied)
                case 0x8c: AbsoluteAddress(); MemoryWrite(Y); break;	                        // STY (absolute)
                case 0x8d: AbsoluteAddress(); MemoryWrite(A); break;	                        // STA (absolute)
                case 0x8e: AbsoluteAddress(); MemoryWrite(X); break;	                        // STX (absolute)

                case 0x90: BranchNot(Carry); break;                                               // BCC (relative)
                case 0x91: IndirectIndexedYAddress(); Fixup(); MemoryWrite(A); break;   // STA (indirect indexed Y)
                case 0x94: ZeroPageXAddress(); MemoryWrite(Y); break;                        // STY (zero page, X)
                case 0x95: ZeroPageXAddress(); MemoryWrite(A); break;                        // STA (zero page, X)
                case 0x96: ZeroPageYAddress(); MemoryWrite(X); break;                        // STX (zero page, Y)
                case 0x98: SwallowRead(); A = Through(Y); break;                        // TYA (implied)
                case 0x99: AbsoluteYAddress(); Fixup(); MemoryWrite(A); break;          // STA (absolute, Y)
                case 0x9a: SwallowRead(); S = X; break;                                      // TXS (implied)
                case 0x9d: AbsoluteXAddress(); Fixup(); MemoryWrite(A); break;          // STA (absolute, X)

                case 0xa0: ImmediateRead(); Y = Through(); break;                            // LDY (immediate)
                case 0xa1: IndexedIndirectXRead(); A = Through(); break;                     // LDA (indexed indirect X)
                case 0xa2: ImmediateRead(); X = Through(); break;                            // LDX (immediate)
                case 0xa4: ZeroPageRead(); Y = Through(); break;                             // LDY (zero page)
                case 0xa5: ZeroPageRead(); A = Through(); break;                             // LDA (zero page)
                case 0xa6: ZeroPageRead(); X = Through(); break;                             // LDX (zero page)
                case 0xa8: SwallowRead(); Y = Through(A); break;                             // TAY (implied)
                case 0xa9: ImmediateRead(); A = Through(); break;                            // LDA (immediate)
                case 0xaa: SwallowRead(); X = Through(A); break;                        // TAX (implied)
                case 0xac: AbsoluteRead(); Y = Through(); break;                             // LDY (absolute)
                case 0xad: AbsoluteRead(); A = Through(); break;                             // LDA (absolute)
                case 0xae: AbsoluteRead(); X = Through(); break;                             // LDX (absolute)

                case 0xb0: Branch(Carry); break;                                                  // BCS (relative)
                case 0xb1: IndirectIndexedYRead(); A = Through(); break;                     // LDA (indirect indexed Y)
                case 0xb4: ZeroPageXRead(); Y = Through(); break;                            // LDY (zero page, X)
                case 0xb5: ZeroPageXRead(); A = Through(); break;                            // LDA (zero page, X)
                case 0xb6: ZeroPageYRead(); X = Through(); break;                            // LDX (zero page, Y)
                case 0xb8: SwallowRead(); ResetFlag(StatusBits.VF); break;                        // CLV (implied)
                case 0xb9: AbsoluteYRead(); A = Through(); break;                            // LDA (absolute, Y)
                case 0xba: SwallowRead(); X = Through(S); break;                        // TSX (implied)
                case 0xbc: AbsoluteXRead(); Y = Through(); break;                            // LDY (absolute, X)
                case 0xbd: AbsoluteXRead(); A = Through(); break;                            // LDA (absolute, X)
                case 0xbe: AbsoluteYRead(); X = Through(); break;                            // LDX (absolute, Y)

                case 0xc0: ImmediateRead(); CMP(Y); break;                                   // CPY (immediate)
                case 0xc1: IndexedIndirectXRead(); CMP(A); break;                            // CMP (indexed indirect X)
                case 0xc2: ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0xc4: ZeroPageRead(); CMP(Y); break;                                    // CPY (zero page)
                case 0xc5: ZeroPageRead(); CMP(A); break;                                    // CMP (zero page)
                case 0xc6: ZeroPageRead(); ModifyWrite(DEC()); break;                        // DEC (zero page)
                case 0xc8: SwallowRead(); Y = INC(Y); break;                            // INY (implied)
                case 0xc9: ImmediateRead(); CMP(A); break;                                   // CMP (immediate)
                case 0xca: SwallowRead(); X = DEC(X); break;                            // DEX (implied)
                case 0xcc: AbsoluteRead(); CMP(Y); break;                                    // CPY (absolute)
                case 0xcd: AbsoluteRead(); CMP(A); break;                                    // CMP (absolute)
                case 0xce: AbsoluteRead(); ModifyWrite(DEC()); break;                        // DEC (absolute)

                case 0xd0: BranchNot(Zero); break;                                                // BNE (relative)
                case 0xd1: IndirectIndexedYRead(); CMP(A); break;                            // CMP (indirect indexed Y)
                case 0xd4: ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0xd5: ZeroPageXRead(); CMP(A); break;                                   // CMP (zero page, X)
                case 0xd6: ZeroPageXRead(); ModifyWrite(DEC()); break;                       // DEC (zero page, X)
                case 0xd8: SwallowRead(); ResetFlag(StatusBits.DF); break;                        // CLD (implied)
                case 0xd9: AbsoluteYRead(); CMP(A); break;                                   // CMP (absolute, Y)
                case 0xdd: AbsoluteXRead(); CMP(A); break;                                   // CMP (absolute, X)
                case 0xde: AbsoluteXAddress(); FixupRead(); ModifyWrite(DEC()); break;  // DEC (absolute, X)

                case 0xe0: ImmediateRead(); CMP(X); break;                                   // CPX (immediate)
                case 0xe1: IndexedIndirectXRead(); SBC(); break;                                  // SBC (indexed indirect X)
                case 0xe2: ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0xe4: ZeroPageRead(); CMP(X); break;                                    // CPX (zero page)
                case 0xe5: ZeroPageRead(); SBC(); break;                                          // SBC (zero page)
                case 0xe6: ZeroPageRead(); ModifyWrite(INC()); break;                             // INC (zero page)
                case 0xe8: SwallowRead(); X = INC(X); break;                            // INX (implied)
                case 0xe9: ImmediateRead(); SBC(); break;                                         // SBC (immediate)
                case 0xea: SwallowRead(); break;                                                       // NOP (implied)
                case 0xec: AbsoluteRead(); CMP(X); break;                                    // CPX (absolute)
                case 0xed: AbsoluteRead(); SBC(); break;                                          // SBC (absolute)
                case 0xee: AbsoluteRead(); ModifyWrite(INC()); break;                        // INC (absolute)

                case 0xf0: Branch(Zero); break;                                                   // BEQ (relative)
                case 0xf1: IndirectIndexedYRead(); SBC(); break;                                  // SBC (indirect indexed Y)
                case 0xf4: ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0xf5: ZeroPageXRead(); SBC(); break;                                         // SBC (zero page, X)
                case 0xf6: ZeroPageXRead(); ModifyWrite(INC()); break;                       // INC (zero page, X)
                case 0xf8: SwallowRead(); SetFlag(StatusBits.DF); break;                          // SED (implied)
                case 0xf9: AbsoluteYRead(); SBC(); break;                                         // SBC (absolute, Y)
                case 0xfd: AbsoluteXRead(); SBC(); break;                                         // SBC (absolute, X)
                case 0xfe: AbsoluteXAddress(); FixupRead(); ModifyWrite(INC()); break;  // INC (absolute, X)
            }

            return cycles != Cycles;
        }

        public override void PoweredStep()
        {
            Tick();
            if (SO.Lowered())
            {
                HandleSO();
            }

            if (RDY.Raised())
            {
                FetchInstruction();
                if (RESET.Lowered())
                {
                    HandleRESET();
                }
                else if (NMI.Lowered())
                {
                    HandleNMI();
                }
                else if (INT.Lowered() && InterruptMasked == 0)
                {
                    HandleINT();
                }
                else
                {
                    Execute();
                }
            }
        }

        private void FetchInstruction()
        {
            // Instruction fetch beginning
            LowerSYNC();

            System.Diagnostics.Debug.Assert(Cycles == 1, "An extra cycle has occurred");

            // Can't use fetchByte, since that would add an extra tick.
            ImmediateAddress();
            OpCode = ReadFromBus();

            System.Diagnostics.Debug.Assert(Cycles == 1, "BUS read has introduced stray cycles");

            // Instruction fetch has now completed
            RaiseSYNC();
        }

        #endregion

        #region Bus/Memory access

        protected override sealed void BusWrite()
        {
            Tick();
            WriteToBus();
        }

        protected override sealed byte BusRead()
        {
            Tick();
            return ReadFromBus();
        }

        private byte ReadFromBus()
        {
            RaiseRW();
            return base.BusRead();
        }

        private void WriteToBus()
        {
            LowerRW();
            base.BusWrite();
        }

        protected abstract void ModifyWrite(byte data);

        #endregion

        #region Stack access

        protected override byte Pop()
        {
            RaiseStack();
            return MemoryRead();
        }

        protected override void Push(byte value)
        {
            LowerStack();
            MemoryWrite(value);
        }

        private void UpdateStack(byte position)
        {
            Bus.Address.Assign(position, 1);
        }

        private void LowerStack() => UpdateStack(S--);

        private void RaiseStack() => UpdateStack(++S);

        private void DummyPush()
        {
            LowerStack();
            Tick();    // In place of the memory write
        }

        #endregion

        #region Addressing modes

        #region Address page fixup

        private byte fixedPage;

        private byte unfixedPage;

        public byte FixedPage
        {
            get => fixedPage;
            protected set => fixedPage = value;
        }

        public byte UnfixedPage
        {
            get => unfixedPage;
            protected set => unfixedPage = value;
        }

        public bool Fixed => FixedPage != UnfixedPage;

        protected void MaybeFixup()
        {
            if (Bus.Address.High != FixedPage)
            {
                Fixup();
            }
        }

        protected abstract void Fixup();

        protected void MaybeFixupRead()
        {
            MaybeFixup();
            MemoryRead();
        }

        protected void FixupRead()
        {
            Fixup();
            MemoryRead();
        }

        #endregion

        #region Address resolution

        protected void NoteFixedAddress(int address)
        {
            NoteFixedAddress((ushort)address);
        }

        protected void NoteFixedAddress(ushort address)
        {
            UnfixedPage = Bus.Address.High;
            Intermediate.Word = address;
            FixedPage = Intermediate.High;
            Bus.Address.Low = Intermediate.Low;
        }

        protected void GetAddressPaged()
        {
            GetWordPaged();
            Bus.Address.Assign(Intermediate);
        }

        protected void ImmediateAddress()
        {
            Bus.Address.Assign(PC);
            ++PC.Word;
        }

        protected void AbsoluteAddress() => FetchWordAddress();

        protected void ZeroPageAddress()
        {
            Bus.Address.Assign(FetchByte(), 0);
        }

        protected void ZeroPageIndirectAddress()
        {
            ZeroPageAddress();
            GetAddressPaged();
        }

        protected abstract void IndirectAddress();

        protected void ZeroPageWithIndexAddress(byte index)
        {
            ZeroPageRead();
            Bus.Address.Low += index;
        }

        protected void ZeroPageXAddress() => ZeroPageWithIndexAddress(X);

        protected void ZeroPageYAddress() => ZeroPageWithIndexAddress(Y);

        private void AbsoluteWithIndexAddress(byte index)
        {
            AbsoluteAddress();
            NoteFixedAddress(Bus.Address.Word + index);
        }

        protected void AbsoluteXAddress() => AbsoluteWithIndexAddress(X);

        protected void AbsoluteYAddress() => AbsoluteWithIndexAddress(Y);

        protected void IndexedIndirectXAddress()
        {
            ZeroPageXAddress();
            GetAddressPaged();
        }

        protected void IndirectIndexedYAddress()
        {
            ZeroPageIndirectAddress();
            NoteFixedAddress(Bus.Address.Word + Y);
        }

        #endregion

        #region Address and read

        protected void ImmediateRead()
        {
            ImmediateAddress();
            MemoryRead();
        }

        protected void AbsoluteRead()
        {
            AbsoluteAddress();
            MemoryRead();
        }

        protected void ZeroPageRead()
        {
            ZeroPageAddress();
            MemoryRead();
        }

        protected void ZeroPageXRead()
        {
            ZeroPageXAddress();
            MemoryRead();
        }

        protected void ZeroPageYRead()
        {
            ZeroPageYAddress();
            MemoryRead();
        }

        protected void IndexedIndirectXRead()
        {
            IndexedIndirectXAddress();
            MemoryRead();
        }

        protected void AbsoluteXRead()
        {
            AbsoluteXAddress();
            MaybeFixupRead();
        }

        protected void AbsoluteYRead()
        {
            AbsoluteYAddress();
            MaybeFixupRead();
        }

        protected void IndirectIndexedYRead()
        {
            IndirectIndexedYAddress();
            MaybeFixupRead();
        }

        #endregion

        #endregion

        #region Branching

        protected void BranchNot(int condition) => Branch(condition == 0);

        protected void Branch(int condition) => Branch(condition != 0);

        protected void Branch(bool condition)
        {
            ImmediateRead();
            if (condition)
            {
                var relative = (sbyte)Bus.Data;
                SwallowRead();
                FixupBranch(relative);
                Jump(Bus.Address);
            }
        }

        protected abstract void FixupBranch(sbyte relative);

        #endregion

        #region Data flag adjustment

        protected void AdjustZero(byte datum) => ResetFlag(StatusBits.ZF, datum);

        protected void AdjustNegative(byte datum) => SetFlag(StatusBits.NF, NegativeTest(datum));

        protected void AdjustNZ(byte datum)
        {
            AdjustZero(datum);
            AdjustNegative(datum);
        }

        protected byte Through() => Through(Bus.Data);

        protected byte Through(int data) => Through((byte)data);

        protected byte Through(byte data)
        {
            AdjustNZ(data);
            return data;
        }

        #endregion

        #region Instruction implementations

        #region Instructions with BCD effects

        #region Addition/subtraction

        #region Subtraction

        protected void AdjustOverflowSubtract(byte operand)
        {
            var data = Bus.Data;
            var intermediate = Intermediate.Low;
            SetFlag(StatusBits.VF, NegativeTest((byte)((operand ^ data) & (operand ^ intermediate))));
        }

        protected void SBC()
        {
            var operand = A;
            A = SUB(operand, CarryTest((byte)~P));

            AdjustOverflowSubtract(operand);
            AdjustNZ(Intermediate.Low);
            ResetFlag(StatusBits.CF, Intermediate.High);
        }

        private byte SUB(byte operand, int borrow) => DecimalMasked != 0 ? DecimalSUB(operand, borrow) : BinarySUB(operand, borrow);

        protected byte BinarySUB(byte operand, int borrow = 0)
        {
            var data = Bus.Data;
            Intermediate.Word = (ushort)(operand - data - borrow);
            return Intermediate.Low;
        }

        private byte DecimalSUB(byte operand, int borrow)
        {
            _ = BinarySUB(operand, borrow);

            var data = Bus.Data;
            var low = (byte)(LowNibble(operand) - LowNibble(data) - borrow);
            var lowNegative = NegativeTest(low);
            if (lowNegative != 0)
            {
                low -= 6;
            }

            var high = (byte)(HighNibble(operand) - HighNibble(data) - (lowNegative >> 7));
            var highNegative = NegativeTest(high);
            if (highNegative != 0)
            {
                high -= 6;
            }

            return (byte)(PromoteNibble(high) | LowNibble(low));
        }

        #endregion

        #region Addition

        protected void AdjustOverflowAdd(byte operand)
        {
            var data = Bus.Data;
            var intermediate = Intermediate.Low;
            SetFlag(StatusBits.VF, NegativeTest((byte)(~(operand ^ data) & (operand ^ intermediate))));
        }

        protected void ADC()
        {
            A = DecimalMasked != 0 ? DecimalADC() : BinaryADC();
        }

        private byte BinaryADC()
        {
            var operand = A;
            var data = Bus.Data;
            Intermediate.Word = (ushort)(operand + data + Carry);

            AdjustOverflowAdd(operand);
            SetFlag(StatusBits.CF, CarryTest(Intermediate.High));

            AdjustNZ(Intermediate.Low);

            return Intermediate.Low;
        }

        private byte DecimalADC()
        {
            var operand = A;
            var data = Bus.Data;

            var low = (ushort)(LowerNibble(operand) + LowerNibble(data) + Carry);
            Intermediate.Word = (ushort)(HigherNibble(operand) + HigherNibble(data));

            AdjustZero(LowByte((ushort)(low + Intermediate.Word)));

            if (low > 0x09)
            {
                Intermediate.Word += 0x10;
                low += 0x06;
            }

            AdjustNegative(Intermediate.Low);
            AdjustOverflowAdd(operand);

            if (Intermediate.Word > 0x90)
                Intermediate.Word += 0x60;

            SetFlag(StatusBits.CF, Intermediate.High);

            return (byte)(LowerNibble(LowByte(low)) | HigherNibble(Intermediate.Low));
        }

        #endregion

        #endregion

        #endregion

        #region Bitwise operations

        protected void OrR() => A = Through(A | Bus.Data);

        protected void AndR() => A = Through(A & Bus.Data);

        protected void EorR() => A = Through(A ^ Bus.Data);

        protected void BIT()
        {
            var data = Bus.Data;
            SetFlag(StatusBits.VF, OverflowTest(data));
            AdjustZero((byte)(A & data));
            AdjustNegative(data);
        }

        #endregion

        protected void CMP(byte first)
        {
            var second = Bus.Data;
            Intermediate.Word = (ushort)(first - second);
            AdjustNZ(Intermediate.Low);
            ResetFlag(StatusBits.CF, Intermediate.High);
        }

        #region Increment/decrement

        protected byte DEC() => DEC(Bus.Data);

        protected byte DEC(byte value) => Through(value - 1);

        protected byte INC() => INC(Bus.Data);

        protected byte INC(byte value) => Through(value + 1);

        #endregion

        #region Stack operations

        private void JSR()
        {
            Intermediate.Low = FetchByte();
            SwallowPop();
            PushWord(PC);
            PC.High = FetchByte();
            PC.Low = Intermediate.Low;
        }

        private void PHP() => Push(SetBit(P, StatusBits.BF));

        private void PLP()
        {
            SwallowPop();
            P = ClearBit(SetBit(Pop(), StatusBits.RF), StatusBits.BF);
        }

        private void RTI()
        {
            PLP();
            base.Return();
        }

        protected override void Return()
        {
            SwallowPop();
            base.Return();
            SwallowFetch();
        }

        #endregion

        #region Shift/rotate operations

        #region Shift

        protected byte ASL() => ASL(Bus.Data);

        protected byte ASL(byte value)
        {
            SetFlag(StatusBits.CF, NegativeTest(value));
            return Through(value << 1);
        }

        protected byte LSR() => LSR(Bus.Data);

        protected byte LSR(byte value)
        {
            SetFlag(StatusBits.CF, CarryTest(value));
            return Through(value >> 1);
        }

        #endregion

        #region Rotate

        protected byte ROL() => ROL(Bus.Data);

        protected byte ROL(byte value)
        {
            var carryIn = Carry;
            return Through(ASL(value) | carryIn);
        }

        protected byte ROR() => ROR(Bus.Data);

        protected byte ROR(byte value)
        {
            var carryIn = Carry;
            return Through(LSR(value) | carryIn << 7);
        }

        #endregion

        #endregion

        #endregion
    }
}