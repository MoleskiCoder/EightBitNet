// <copyright file="Core.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502
{
    using EightBit;

    public abstract class Core : LittleEndianProcessor
    {
        protected Core(Bus bus)
        : base(bus)
        {
            this.RaisedPOWER += this.Core_RaisedPOWER;
        }

        #region Pin controls

        #region NMI pin

        public ref PinLevel NMI => ref this.nmiLine;
        private PinLevel nmiLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingNMI;
        public event EventHandler<EventArgs>? RaisedNMI;
        public event EventHandler<EventArgs>? LoweringNMI;
        public event EventHandler<EventArgs>? LoweredNMI;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseNMI()
        {
            if (this.NMI.Lowered())
            {
                RaisingNMI?.Invoke(this, EventArgs.Empty);
                this.NMI.Raise();
                RaisedNMI?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerNMI()
        {
            if (this.NMI.Raised())
            {
                LoweringNMI?.Invoke(this, EventArgs.Empty);
                this.NMI.Lower();
                LoweredNMI?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region SO pin

        public ref PinLevel SO => ref this.soLine;
        private PinLevel soLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingSO;
        public event EventHandler<EventArgs>? RaisedSO;
        public event EventHandler<EventArgs>? LoweringSO;
        public event EventHandler<EventArgs>? LoweredSO;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseSO()
        {
            if (this.SO.Lowered())
            {
                RaisingSO?.Invoke(this, EventArgs.Empty);
                this.SO.Raise();
                RaisedSO?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerSO()
        {
            if (this.SO.Raised())
            {
                LoweringSO?.Invoke(this, EventArgs.Empty);
                this.SO.Lower();
                LoweredSO?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region SYNC pin

        public ref PinLevel SYNC => ref this.syncLine;
        private PinLevel syncLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingSYNC;
        public event EventHandler<EventArgs>? RaisedSYNC;
        public event EventHandler<EventArgs>? LoweringSYNC;
        public event EventHandler<EventArgs>? LoweredSYNC;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        protected virtual void RaiseSYNC()
        {
            if (this.SYNC.Lowered())
            {
                RaisingSYNC?.Invoke(this, EventArgs.Empty);
                this.SYNC.Raise();
                RaisedSYNC?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void LowerSYNC()
        {
            if (this.SYNC.Raised())
            {
                LoweringSYNC?.Invoke(this, EventArgs.Empty);
                this.SYNC.Lower();
                LoweredSYNC?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region RDY pin

        public ref PinLevel RDY => ref this.rdyLine;
        private PinLevel rdyLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingRDY;
        public event EventHandler<EventArgs>? RaisedRDY;
        public event EventHandler<EventArgs>? LoweringRDY;
        public event EventHandler<EventArgs>? LoweredRDY;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRDY()
        {
            if (this.RDY.Lowered())
            {
                RaisingRDY?.Invoke(this, EventArgs.Empty);
                this.RDY.Raise();
                RaisedRDY?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerRDY()
        {
            if (this.RDY.Raised())
            {
                LoweringRDY?.Invoke(this, EventArgs.Empty);
                this.RDY.Lower();
                LoweredRDY?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region RW pin

        public ref PinLevel RW => ref this.rwLine;
        private PinLevel rwLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingRW;
        public event EventHandler<EventArgs>? RaisedRW;
        public event EventHandler<EventArgs>? LoweringRW;
        public event EventHandler<EventArgs>? LoweredRW;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRW()
        {
            if (this.RW.Lowered())
            {
                RaisingRW?.Invoke(this, EventArgs.Empty);
                this.RW.Raise();
                RaisedRW?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerRW()
        {
            if (this.RW.Raised())
            {
                LoweringRW?.Invoke(this, EventArgs.Empty);
                this.RW.Lower();
                LoweredRW?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        private void Core_RaisedPOWER(object? sender, EventArgs e)
        {
            this.X = (byte)Bits.Bit7;
            this.Y = 0;
            this.A = 0;
            this.P = (byte)StatusBits.RF;
            this.S = (byte)Mask.Eight;
            this.LowerSYNC();
            this.LowerRW();
        }

        #endregion

        #region Interrupts

        private const byte IRQ_vector = 0xfe;  // IRQ vector
        private const byte RST_vector = 0xfc;  // RST vector
        private const byte NMI_vector = 0xfa;  // NMI vector

        protected enum InterruptSource { hardware, software };

        protected enum InterruptType { reset, nonReset };

        protected virtual void Interrupt(byte vector, InterruptSource source = InterruptSource.hardware, InterruptType type = InterruptType.nonReset)
        {
            if (type == InterruptType.reset)
            {
                this.DummyPush();
                this.DummyPush();
                this.DummyPush();
            }
            else
            {
                this.PushWord(this.PC);
                this.Push((byte)(this.P | (source == InterruptSource.hardware ? 0 : (byte)StatusBits.BF)));
            }
            this.SetFlag(StatusBits.IF);   // Disable IRQ
            this.GetWordPaged(0xff, vector);
            this.Jump(this.Intermediate);
        }

        #region Interrupt etc. handlers

        protected sealed override void HandleRESET()
        {
            this.RaiseRESET();
            this.Interrupt(RST_vector, InterruptSource.hardware, InterruptType.reset);
        }

        protected sealed override void HandleINT()
        {
            this.RaiseINT();
            this.Interrupt(IRQ_vector);
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.Interrupt(NMI_vector);
        }

        private void HandleSO()
        {
            this.RaiseSO();
            this.SetFlag(StatusBits.VF);
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

        protected int InterruptMasked => this.P & (byte)StatusBits.IF;
        protected int DecimalMasked => this.P & (byte)StatusBits.DF;
        protected int Negative => NegativeTest(this.P);
        protected int Zero => ZeroTest(this.P);
        protected int Overflow => OverflowTest(this.P);
        protected int Carry => CarryTest(this.P);

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

        protected void SetFlag(StatusBits flag) => this.P = SetBit(this.P, flag);

        protected void SetFlag(StatusBits which, int condition) => this.P = SetBit(this.P, which, condition);

        protected void SetFlag(StatusBits which, bool condition) => this.P = SetBit(this.P, which, condition);

        protected void ResetFlag(StatusBits which) => this.P = ClearBit(this.P, which);

        protected void ResetFlag(StatusBits which, int condition) => this.P = ClearBit(this.P, which, condition);

        #endregion

        #endregion

        #region Cycle wastage

        protected void SwallowRead() => _ = this.MemoryRead(this.PC);

        protected void SwallowPop() => _ = this.MemoryRead(this.S, 1);

        protected void SwallowFetch() => _ = this.FetchByte();

        #endregion

        #region Core instruction dispatching

        public override void Execute() => this.MaybeExecute();

        protected virtual bool MaybeExecute()
        {
            var cycles = this.Cycles;
            switch (this.OpCode)
            {
                case 0x00: this.BRK(); break;                                               // BRK (implied)
                case 0x01: this.IndexedIndirectX(); this.ORA(); break;                      // ORA (indexed indirect X)
                case 0x05: this.ZeroPage(); this.ORA(); break;                              // ORA (zero page)
                case 0x06: this.ZeroPage(); this.ASL(); break;                              // ASL (zero page)
                case 0x08: this.PHP(); break;                                               // PHP (implied)
                case 0x09: this.Immediate(); this.ORA(); break;                             // ORA (immediate)
                case 0x0a: this.ASLA(); break;                                              // ASL A (implied)
                case 0x0d: this.Absolute(); this.ORA(); break;                              // ORA (absolute)
                case 0x0e: this.Absolute(); this.ASL(); break;                              // ASL (absolute)

                case 0x10: this.BPL(); break;                                               // BPL (relative)
                case 0x11: this.IndirectIndexedY(); this.ORA(); break;                      // ORA (indirect indexed Y)
                case 0x15: this.ZeroPageX(); this.ORA(); break;                             // ORA (zero page, X)
                case 0x16: this.ZeroPageX(); this.ASL(); break;                             // ASL (zero page, X)
                case 0x18: this.CLC(); break;                                               // CLC (implied)
                case 0x19: this.AbsoluteY(); this.ORA(); break;                             // ORA (absolute, Y)
                case 0x1d: this.AbsoluteX(); this.ORA(); break;                             // ORA (absolute, X)
                case 0x1e: this.AbsoluteXAddress(); this.FixupRead(); this.ASL(); break;    // ASL (absolute, X)

                case 0x20: this.JSR(); break;                                               // JSR (absolute)
                case 0x21: this.IndexedIndirectX(); this.AND(); break;                      // AND (indexed indirect X)
                case 0x24: this.ZeroPage(); this.BIT(); break;                              // BIT (zero page)
                case 0x25: this.ZeroPage(); this.AND(); break;                              // AND (zero page)
                case 0x26: this.ZeroPage(); this.ROL(); break;                              // ROL (zero page)
                case 0x28: this.PLP(); break;                                               // PLP (implied)
                case 0x29: this.FetchByte(); this.AND(); break;                             // AND (immediate)
                case 0x2a: this.ROLA(); break;                                              // ROL A (implied)
                case 0x2c: this.Absolute(); this.BIT(); break;                              // BIT (absolute)
                case 0x2d: this.Absolute(); this.AND(); break;                              // AND (absolute)
                case 0x2e: this.Absolute(); this.ROL(); break;                              // ROL (absolute)

                case 0x30: this.BMI(); break;                                               // BMI (relative)
                case 0x31: this.IndirectIndexedY(); this.AND(); break;                      // AND (indirect indexed Y)
                case 0x35: this.ZeroPageX(); this.AND(); break;                             // AND (zero page, X)
                case 0x36: this.ZeroPageX(); this.ROL(); break;                             // ROL (zero page, X)
                case 0x38: this.SEC(); break;                                               // SEC (implied)
                case 0x39: this.AbsoluteY(); this.AND(); break;                             // AND (absolute, Y)
                case 0x3d: this.AbsoluteX(); this.AND(); break;                             // AND (absolute, X)
                case 0x3e: this.AbsoluteXAddress(); this.FixupRead(); this.ROL(); break;    // ROL (absolute, X)

                case 0x40: this.RTI(); break;                                               // RTI (implied)
                case 0x41: this.IndexedIndirectX(); this.EOR(); break;                      // EOR (indexed indirect X)
                case 0x44: this.ZeroPage(); break;                                          // *NOP (zero page)
                case 0x45: this.ZeroPage(); this.EOR(); break;                              // EOR (zero page)
                case 0x46: this.ZeroPage(); this.LSR(); break;                              // LSR (zero page)
                case 0x48: this.PHA(); break;                                               // PHA (implied)
                case 0x49: this.Immediate(); this.EOR(); break;                             // EOR (immediate)
                case 0x4a: this.LSRA(); break;                                              // LSR A (implied)
                case 0x4c: this.AbsoluteAddress(); this.JMP(); break;                       // JMP (absolute)
                case 0x4d: this.Absolute(); this.EOR(); break;                              // EOR (absolute)
                case 0x4e: this.Absolute(); this.LSR(); break;                              // LSR (absolute)

                case 0x50: this.BVC(); break;                                               // BVC (relative)
                case 0x51: this.IndirectIndexedY(); this.EOR(); break;                      // EOR (indirect indexed Y)
                case 0x54: this.ZeroPageX(); break;                                         // *NOP (zero page, X)
                case 0x55: this.ZeroPageX(); this.EOR(); break;                             // EOR (zero page, X)
                case 0x56: this.ZeroPageX(); this.LSR(); break;                             // LSR (zero page, X)
                case 0x58: this.CLI(); break;                                               // CLI (implied)
                case 0x59: this.AbsoluteY(); this.EOR(); break;                             // EOR (absolute, Y)
                case 0x5d: this.AbsoluteX(); this.EOR(); break;                             // EOR (absolute, X)
                case 0x5e: this.AbsoluteXAddress(); this.FixupRead(); this.LSR(); break;    // LSR (absolute, X)

                case 0x60: this.RTS(); break;                                               // RTS (implied)
                case 0x61: this.IndexedIndirectX(); this.ADC(); break;                      // ADC (indexed indirect X)
                case 0x65: this.ZeroPage(); this.ADC(); break;                              // ADC (zero page)
                case 0x66: this.ZeroPage(); this.ROR(); break;                              // ROR (zero page)
                case 0x68: this.PLA(); break;                                               // PLA (implied)
                case 0x69: this.Immediate(); this.ADC(); break;                             // ADC (immediate)
                case 0x6a: this.RORA(); break;                                              // ROR A (implied)
                case 0x6c: this.IndirectAddress(); this.JMP(); break;                       // JMP (indirect)
                case 0x6d: this.Absolute(); this.ADC(); break;                              // ADC (absolute)
                case 0x6e: this.Absolute(); this.ROR(); break;                              // ROR (absolute)

                case 0x70: this.BVS(); break;                                               // BVS (relative)
                case 0x71: this.IndirectIndexedY(); this.ADC(); break;                      // ADC (indirect indexed Y)
                case 0x75: this.ZeroPageX(); this.ADC(); break;                             // ADC (zero page, X)
                case 0x76: this.ZeroPageX(); this.ROR(); break;                             // ROR (zero page, X)
                case 0x78: this.SEI(); break;                                               // SEI (implied)
                case 0x79: this.AbsoluteY(); this.ADC(); break;                             // ADC (absolute, Y)
                case 0x7d: this.AbsoluteX(); this.ADC(); break;                             // ADC (absolute, X)
                case 0x7e: this.AbsoluteXAddress(); this.FixupRead(); this.ROR(); break;	// ROR (absolute, X)

                case 0x81: this.IndexedIndirectXAddress(); this.STA(); break;               // STA (indexed indirect X)
                case 0x82: this.Immediate(); break;                                         // *NOP (immediate)
                case 0x84: this.ZeroPageAddress(); this.STY(); break;                       // STY (zero page)
                case 0x85: this.ZeroPageAddress(); this.STA(); break;	                    // STA (zero page)
                case 0x86: this.ZeroPageAddress(); this.STX(); break;	                    // STX (zero page)
                case 0x88: this.DEY(); break;	                                            // DEY (implied)
                case 0x8a: this.TXA(); break;	                                            // TXA (implied)
                case 0x8c: this.AbsoluteAddress(); this.STY(); break;	                    // STY (absolute)
                case 0x8d: this.AbsoluteAddress(); this.STA(); break;	                    // STA (absolute)
                case 0x8e: this.AbsoluteAddress(); this.STX(); break;	                    // STX (absolute)

                case 0x90: this.BCC(); break;                                               // BCC (relative)
                case 0x91: this.IndirectIndexedYAddress(); this.Fixup(); this.STA(); break; // STA (indirect indexed Y)
                case 0x94: this.ZeroPageXAddress(); this.STY(); break;                      // STY (zero page, X)
                case 0x95: this.ZeroPageXAddress(); this.STA(); break;                      // STA (zero page, X)
                case 0x96: this.ZeroPageYAddress(); this.STX(); break;                      // STX (zero page, Y)
                case 0x98: this.TYA(); break;                                               // TYA (implied)
                case 0x99: this.AbsoluteYAddress(); this.Fixup(); this.STA(); break;        // STA (absolute, Y)
                case 0x9a: this.TXS(); break;                                               // TXS (implied)
                case 0x9d: this.AbsoluteXAddress(); this.Fixup(); this.STA(); break;        // STA (absolute, X)

                case 0xa0: this.Immediate(); this.LDY(); break;                             // LDY (immediate)
                case 0xa1: this.IndexedIndirectX(); this.LDA(); break;                      // LDA (indexed indirect X)
                case 0xa2: this.Immediate(); this.LDX(); break;                             // LDX (immediate)
                case 0xa4: this.ZeroPage(); this.LDY(); break;                              // LDY (zero page)
                case 0xa5: this.ZeroPage(); this.LDA(); break;                              // LDA (zero page)
                case 0xa6: this.ZeroPage(); this.LDX(); break;                              // LDX (zero page)
                case 0xa8: this.TAY(); break;                                               // TAY (implied)
                case 0xa9: this.Immediate(); this.LDA(); break;                             // LDA (immediate)
                case 0xaa: this.TAX(); break;                                               // TAX (implied)
                case 0xac: this.Absolute(); this.LDY(); break;                              // LDY (absolute)
                case 0xad: this.Absolute(); this.LDA(); break;                              // LDA (absolute)
                case 0xae: this.Absolute(); this.LDX(); break;                              // LDX (absolute)

                case 0xb0: this.BCS(); break;                                               // BCS (relative)
                case 0xb1: this.IndirectIndexedY(); this.LDA(); break;                      // LDA (indirect indexed Y)
                case 0xb4: this.ZeroPageX(); this.LDY(); break;                             // LDY (zero page, X)
                case 0xb5: this.ZeroPageX(); this.LDA(); break;                             // LDA (zero page, X)
                case 0xb6: this.ZeroPageY(); this.LDX(); break;                             // LDX (zero page, Y)
                case 0xb8: this.CLV(); break;                                               // CLV (implied)
                case 0xb9: this.AbsoluteY(); this.LDA(); break;                             // LDA (absolute, Y)
                case 0xba: this.TSX(); break;                                               // TSX (implied)
                case 0xbc: this.AbsoluteX(); this.LDY(); break;                             // LDY (absolute, X)
                case 0xbd: this.AbsoluteX(); this.LDA(); break;                             // LDA (absolute, X)
                case 0xbe: this.AbsoluteY(); this.LDX(); break;                             // LDX (absolute, Y)

                case 0xc0: this.Immediate(); this.CPY(); break;                             // CPY (immediate)
                case 0xc1: this.IndexedIndirectX(); this.CMP(); break;                      // CMP (indexed indirect X)
                case 0xc2: this.Immediate(); break;                                         // *NOP (immediate)
                case 0xc4: this.ZeroPage(); this.CPY(); break;                              // CPY (zero page)
                case 0xc5: this.ZeroPage(); this.CMP(); break;                              // CMP (zero page)
                case 0xc6: this.ZeroPage(); this.DEC(); break;                              // DEC (zero page)
                case 0xc8: this.INY(); break;                                               // INY (implied)
                case 0xc9: this.Immediate(); this.CMP(); break;                             // CMP (immediate)
                case 0xca: this.DEX(); break;                                               // DEX (implied)
                case 0xcc: this.Absolute(); this.CPY(); break;                              // CPY (absolute)
                case 0xcd: this.Absolute(); this.CMP(); break;                              // CMP (absolute)
                case 0xce: this.Absolute(); this.DEC(); break;                              // DEC (absolute)

                case 0xd0: this.BNE(); break;                                               // BNE (relative)
                case 0xd1: this.IndirectIndexedY(); this.CMP(); break;                      // CMP (indirect indexed Y)
                case 0xd4: this.ZeroPageX(); break;                                         // *NOP (zero page, X)
                case 0xd5: this.ZeroPageX(); this.CMP(); break;                             // CMP (zero page, X)
                case 0xd6: this.ZeroPageX(); this.DEC(); break;                             // DEC (zero page, X)
                case 0xd8: this.CLD(); break;                                               // CLD (implied)
                case 0xd9: this.AbsoluteY(); this.CMP(); break;                             // CMP (absolute, Y)
                case 0xdd: this.AbsoluteX(); this.CMP(); break;                             // CMP (absolute, X)
                case 0xde: this.AbsoluteXAddress(); this.FixupRead(); this.DEC(); break;    // DEC (absolute, X)

                case 0xe0: this.Immediate(); this.CPX(); break;                             // CPX (immediate)
                case 0xe1: this.IndexedIndirectX(); this.SBC(); break;                      // SBC (indexed indirect X)
                case 0xe2: this.Immediate(); break;                                         // *NOP (immediate)
                case 0xe4: this.ZeroPage(); this.CPX(); break;                              // CPX (zero page)
                case 0xe5: this.ZeroPage(); this.SBC(); break;                              // SBC (zero page)
                case 0xe6: this.ZeroPage(); this.INC(); break;                              // INC (zero page)
                case 0xe8: this.INX(); break;                                               // INX (implied)
                case 0xe9: this.Immediate(); this.SBC(); break;                             // SBC (immediate)
                case 0xea: this.NOP(); break;                                               // NOP (implied)
                case 0xec: this.Absolute(); this.CPX(); break;                              // CPX (absolute)
                case 0xed: this.Absolute(); this.SBC(); break;                              // SBC (absolute)
                case 0xee: this.Absolute(); this.INC(); break;                              // INC (absolute)

                case 0xf0: this.BEQ(); break;                                               // BEQ (relative)
                case 0xf1: this.IndirectIndexedY(); this.SBC(); break;                      // SBC (indirect indexed Y)
                case 0xf4: this.ZeroPageX(); break;                                         // *NOP (zero page, X)
                case 0xf5: this.ZeroPageX(); this.SBC(); break;                             // SBC (zero page, X)
                case 0xf6: this.ZeroPageX(); this.INC(); break;                             // INC (zero page, X)
                case 0xf8: this.SED(); break;                                               // SED (implied)
                case 0xf9: this.AbsoluteY(); this.SBC(); break;                             // SBC (absolute, Y)
                case 0xfd: this.AbsoluteX(); this.SBC(); break;                             // SBC (absolute, X)
                case 0xfe: this.AbsoluteXAddress(); this.FixupRead(); this.INC(); break;    // INC (absolute, X)

                default:
                    break;
            }

            return cycles != this.Cycles;
        }

        public override void PoweredStep()
        {
            this.Tick();
            if (this.SO.Lowered())
            {
                this.HandleSO();
            }

            if (this.RDY.Raised())
            {
                this.OpCode = this.FetchInstruction();
                if (this.RESET.Lowered())
                {
                    this.HandleRESET();
                }
                else if (this.NMI.Lowered())
                {
                    this.HandleNMI();
                }
                else if (this.INT.Lowered() && this.InterruptMasked == 0)
                {
                    this.HandleINT();
                }
                else
                {
                    this.Execute();
                }
            }
        }

        protected override byte FetchInstruction()
        {
            this.LowerSYNC();
            System.Diagnostics.Debug.Assert(this.Cycles == 1, "An extra cycle has occurred");

            // Can't use "FetchByte", since that would add an extra tick.
            this.ImmediateAddress();
            this.ReadFromBus();

            System.Diagnostics.Debug.Assert(this.Cycles == 1, "BUS read has introduced stray cycles");
            this.RaiseSYNC();

            return this.Bus.Data;
        }

        #endregion

        #region Bus/Memory access

        protected sealed override void BusWrite()
        {
            this.Tick();
            this.WriteToBus();
        }

        protected sealed override byte BusRead()
        {
            this.Tick();
            return this.ReadFromBus();
        }

        private byte ReadFromBus()
        {
            this.RaiseRW();
            return base.BusRead();
        }

        private void WriteToBus()
        {
            this.LowerRW();
            base.BusWrite();
        }

        protected abstract void ModifyWrite(byte data);

        #endregion

        #region Stack access

        protected override byte Pop()
        {
            this.RaiseStack();
            return this.MemoryRead();
        }

        protected override void Push(byte value)
        {
            this.LowerStack();
            this.MemoryWrite(value);
        }

        private void UpdateStack(byte position) => this.Bus.Address.Assign(position, 1);

        private void LowerStack() => this.UpdateStack(this.S--);

        private void RaiseStack() => this.UpdateStack(++this.S);

        private void DummyPush()
        {
            this.LowerStack();
            this.Tick();    // In place of the memory write
        }

        #endregion

        #region Addressing modes

        #region Address page fixup

        public byte FixedPage { get; protected set; }

        public byte UnfixedPage { get; protected set; }

        public bool Fixed => this.FixedPage != this.UnfixedPage;

        protected void MaybeFixup()
        {
            if (this.Bus.Address.High != this.FixedPage)
            {
                this.Fixup();
            }
        }

        protected abstract void Fixup();

        protected byte MaybeFixupRead()
        {
            this.MaybeFixup();
            return this.MemoryRead();
        }

        protected byte FixupRead()
        {
            this.Fixup();
            return this.MemoryRead();
        }

        #endregion

        #region Address resolution

        protected void NoteFixedAddress(int address) => this.NoteFixedAddress((ushort)address);

        protected void NoteFixedAddress(ushort address)
        {
            this.UnfixedPage = this.Bus.Address.High;
            this.Intermediate.Word = address;
            this.FixedPage = this.Intermediate.High;
            this.Bus.Address.Low = this.Intermediate.Low;
        }

        protected void GetAddressPaged()
        {
            this.GetWordPaged();
            this.Bus.Address.Assign(this.Intermediate);
        }

        protected void AbsoluteAddress() => this.FetchWordAddress();

        protected void ZeroPageAddress() => this.Bus.Address.Assign(this.FetchByte());

        protected void ZeroPageIndirectAddress()
        {
            this.ZeroPageAddress();
            this.GetAddressPaged();
        }

        protected abstract void IndirectAddress();

        protected void ZeroPageWithIndexAddress(byte index)
        {
            this.ZeroPage();
            this.Bus.Address.Low += index;
        }

        protected void ZeroPageXAddress() => this.ZeroPageWithIndexAddress(this.X);

        protected void ZeroPageYAddress() => this.ZeroPageWithIndexAddress(this.Y);

        private void AbsoluteWithIndexAddress(byte index)
        {
            this.AbsoluteAddress();
            this.NoteFixedAddress(this.Bus.Address.Word + index);
        }

        protected void AbsoluteXAddress() => this.AbsoluteWithIndexAddress(this.X);

        protected void AbsoluteYAddress() => this.AbsoluteWithIndexAddress(this.Y);

        protected void IndexedIndirectXAddress()
        {
            this.ZeroPageXAddress();
            this.GetAddressPaged();
        }

        protected void IndirectIndexedYAddress()
        {
            this.ZeroPageIndirectAddress();
            this.NoteFixedAddress(this.Bus.Address.Word + this.Y);
        }

        #endregion

        #region Address and read

        protected byte Immediate() => this.FetchByte();

        protected byte Absolute()
        {
            this.AbsoluteAddress();
            return this.MemoryRead();
        }

        protected byte ZeroPage()
        {
            this.ZeroPageAddress();
            return this.MemoryRead();
        }

        protected byte ZeroPageX()
        {
            this.ZeroPageXAddress();
            return this.MemoryRead();
        }

        protected byte ZeroPageY()
        {
            this.ZeroPageYAddress();
            return this.MemoryRead();
        }

        protected byte IndexedIndirectX()
        {
            this.IndexedIndirectXAddress();
            return this.MemoryRead();
        }

        protected byte AbsoluteX()
        {
            this.AbsoluteXAddress();
            return this.MaybeFixupRead();
        }

        protected byte AbsoluteY()
        {
            this.AbsoluteYAddress();
            return this.MaybeFixupRead();
        }

        protected byte IndirectIndexedY()
        {
            this.IndirectIndexedYAddress();
            return this.MaybeFixupRead();
        }

        #endregion

        #endregion

        #region Branching

        protected void BranchNot(int condition) => this.Branch(condition == 0);

        protected void Branch(int condition) => this.Branch(condition != 0);

        protected void Branch(bool condition)
        {
            _ = this.FetchByte();
            if (condition)
            {
                var relative = (sbyte)this.Bus.Data;
                this.SwallowRead();
                this.FixupBranch(relative);
                this.Jump(this.Bus.Address);
            }
        }

        protected abstract void FixupBranch(sbyte relative);

        #endregion

        #region Data flag adjustment

        protected void AdjustZero(byte datum) => this.ResetFlag(StatusBits.ZF, datum);

        protected void AdjustNegative(byte datum) => this.SetFlag(StatusBits.NF, NegativeTest(datum));

        protected void AdjustNZ(byte datum)
        {
            this.AdjustZero(datum);
            this.AdjustNegative(datum);
        }

        protected byte Through() => this.Through(this.Bus.Data);

        protected byte Through(int data) => this.Through((byte)data);

        protected byte Through(byte data)
        {
            this.AdjustNZ(data);
            return data;
        }

        #endregion

        #region Instruction implementations

        #region Miscellaneous

        private void NOP() => this.SwallowRead();

        protected void JMP() => this.Jump(this.Bus.Address);

        private void BRK()
        {
            this.SwallowFetch();
            this.Interrupt(IRQ_vector, InterruptSource.software);
        }

        #endregion

        #region Register to register transfer

        private void TAX()
        {
            this.SwallowRead();
            this.X = this.Through(this.A);
        }

        private void TXA()
        {
            this.SwallowRead();
            this.A = this.Through(this.X);
        }

        private void TAY()
        {
            this.SwallowRead();
            this.Y = this.Through(this.A);
        }

        private void TYA()
        {
            this.SwallowRead();
            this.A = this.Through(this.Y);
        }

        private void TXS()
        {
            this.SwallowRead();
            this.S = this.X;
        }

        private void TSX()
        {
            this.SwallowRead();
            this.X = this.Through(this.S);
        }

        #endregion

        #region Load and store

        protected void LDA() => this.A = this.Through();

        protected void STA() => this.MemoryWrite(this.A);

        protected void LDX() => this.X = this.Through();

        private void STX() => this.MemoryWrite(this.X);

        private void LDY() => this.Y = this.Through();

        private void STY() => this.MemoryWrite(this.Y);

        #endregion

        #region Branching

        private void BCS() => this.Branch(this.Carry);

        private void BCC() => this.BranchNot(this.Carry);

        private void BVC() => this.BranchNot(this.Overflow);

        private void BMI() => this.Branch(this.Negative);

        private void BPL() => this.BranchNot(this.Negative);

        private void BVS() => this.Branch(this.Overflow);

        private void BEQ() => this.Branch(this.Zero);

        private void BNE() => this.BranchNot(this.Zero);

        #endregion

        #region Status flag operations

        private void SEI()
        {
            this.SwallowRead();
            this.SetFlag(StatusBits.IF);
        }

        private void CLI()
        {
            this.SwallowRead();
            this.ResetFlag(StatusBits.IF);
        }

        private void CLV()
        {
            this.SwallowRead();
            this.ResetFlag(StatusBits.VF);
        }

        protected void SetCarry() => this.SetFlag(StatusBits.CF);

        private void SEC()
        {
            this.SwallowRead();
            this.SetCarry();
        }

        protected void ClearCarry() => this.ResetFlag(StatusBits.CF);

        private void CLC()
        {
            this.SwallowRead();
            this.ClearCarry();
        }

        protected void SetDecimal() => this.SetFlag(StatusBits.DF);

        private void SED()
        {
            this.SwallowRead();
            this.SetDecimal();
        }

        protected void ClearDecimal() => this.ResetFlag(StatusBits.DF);

        private void CLD()
        {
            this.SwallowRead();
            this.ClearDecimal();
        }

        #endregion

        #region Instructions with BCD effects

        #region Addition/subtraction

        #region Subtraction

        protected void AdjustOverflowSubtract(byte operand)
        {
            var data = this.Bus.Data;
            var intermediate = this.Intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)((operand ^ data) & (operand ^ intermediate))));
        }

        protected void SBC()
        {
            var operand = this.A;
            this.A = this.SUB(operand, CarryTest((byte)~this.P));

            this.AdjustOverflowSubtract(operand);
            this.AdjustNZ(this.Intermediate.Low);
            this.ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        private byte SUB(byte operand, int borrow) => this.DecimalMasked != 0 ? this.DecimalSUB(operand, borrow) : this.BinarySUB(operand, borrow);

        protected byte BinarySUB(byte operand, int borrow = 0)
        {
            var data = this.Bus.Data;
            this.Intermediate.Word = (ushort)(operand - data - borrow);
            return this.Intermediate.Low;
        }

        private byte DecimalSUB(byte operand, int borrow)
        {
            _ = this.BinarySUB(operand, borrow);

            var data = this.Bus.Data;
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
            var data = this.Bus.Data;
            var intermediate = this.Intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)(~(operand ^ data) & (operand ^ intermediate))));
        }

        protected void ADC() => this.ADC(this.Bus.Data);

        protected void ADC(byte data) => this.A = this.DecimalMasked != 0 ? this.DecimalADC(data) : this.BinaryADC(data);

        private byte BinaryADC(byte data)
        {
            var operand = this.A;
            this.Intermediate.Word = (ushort)(operand + data + this.Carry);

            this.AdjustOverflowAdd(operand);
            this.SetFlag(StatusBits.CF, CarryTest(this.Intermediate.High));

            this.AdjustNZ(this.Intermediate.Low);

            return this.Intermediate.Low;
        }

        private byte DecimalADC(byte data)
        {
            var operand = this.A;

            var low = (ushort)(LowerNibble(operand) + LowerNibble(data) + this.Carry);
            this.Intermediate.Word = (ushort)(HigherNibble(operand) + HigherNibble(data));

            this.AdjustZero(LowByte((ushort)(low + this.Intermediate.Word)));

            if (low > 0x09)
            {
                this.Intermediate.Word += 0x10;
                low += 0x06;
            }

            this.AdjustNegative(this.Intermediate.Low);
            this.AdjustOverflowAdd(operand);

            if (this.Intermediate.Word > 0x90)
            {
                this.Intermediate.Word += 0x60;
            }

            this.SetFlag(StatusBits.CF, this.Intermediate.High);

            return (byte)(LowerNibble(LowByte(low)) | HigherNibble(this.Intermediate.Low));
        }

        #endregion

        #endregion

        #endregion

        #region Bitwise operations

        protected void ORA() => this.ORA(this.Bus.Data);

        protected void ORA(byte data) => this.A = this.Through(this.A | data);

        protected void AND() => this.AND(this.Bus.Data);

        protected void AND(byte data) => this.A = this.Through(this.A & data);

        protected void EOR() => this.EOR(this.Bus.Data);

        protected void EOR(byte data) => this.A = this.Through(this.A ^ data);

        protected void BIT()
        {
            var data = this.Bus.Data;
            this.SetFlag(StatusBits.VF, OverflowTest(data));
            this.AdjustZero((byte)(this.A & data));
            this.AdjustNegative(data);
        }

        #endregion

        #region Comparison operations

        protected void CMP() => this.CMP(this.A, this.Bus.Data);

        private void CPX() => this.CMP(this.X, this.Bus.Data);

        private void CPY() => this.CMP(this.Y, this.Bus.Data);

        protected void CMP(byte first, byte second)
        {
            this.Intermediate.Word = (ushort)(first - second);
            this.AdjustNZ(this.Intermediate.Low);
            this.ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        #endregion

        #region Increment/decrement

        private void DEX()
        {
            this.SwallowRead();
            this.X = this.DEC(this.X);
        }

        private void DEY()
        {
            this.SwallowRead();
            this.Y = this.DEC(this.Y);
        }

        protected void DEC() => this.ModifyWrite(this.DEC(this.Bus.Data));

        protected byte DEC(byte value) => this.Through(value - 1);

        private void INX()
        {
            this.SwallowRead();
            this.X = this.INC(this.X);
        }

        private void INY()
        {
            this.SwallowRead();
            this.Y = this.INC(this.Y);
        }

        protected void INC() => this.ModifyWrite(this.INC(this.Bus.Data));

        protected byte INC(byte value) => this.Through(value + 1);

        #endregion

        #region Stack operations

        private void JSR()
        {
            var low = this.FetchByte();
            this.SwallowPop();
            this.PushWord(this.PC);
            var high = this.FetchByte();
            this.PC.Assign(low, high);
        }

        private void PHA()
        {
            this.SwallowRead();
            this.Push(this.A);
        }

        private void PLA()
        {
            this.SwallowRead();
            this.SwallowPop();
            this.A = this.Through(this.Pop());
        }

        private void PHP()
        {
            this.SwallowRead();
            this.Push(SetBit(this.P, StatusBits.BF));
        }

        private void PLP()
        {
            this.SwallowRead();
            this.SwallowPop();
            this.Pop();
            this.P = ClearBit(SetBit(this.Bus.Data, StatusBits.RF), StatusBits.BF);
        }

        private void RTI()
        {
            this.PLP();
            base.Return();
        }

        protected void RTS()
        {
            this.SwallowRead();
            this.Return();
        }

        protected override void Return()
        {
            this.SwallowPop();
            base.Return();
            this.SwallowFetch();
        }

        #endregion

        #region Shift/rotate operations

        #region Shift

        private void ASLA()
        {
            this.SwallowRead();
            this.A = this.ASL(this.A);
        }

        protected void ASL() => this.ModifyWrite(this.ASL(this.Bus.Data));

        protected byte ASL(byte value)
        {
            this.SetFlag(StatusBits.CF, NegativeTest(value));
            return this.Through(value << 1);
        }

        protected void ImplementLSRA() => this.A = this.LSR(this.A);

        private void LSRA()
        {
            this.SwallowRead();
            ImplementLSRA();
        }

        protected void LSR() => this.ModifyWrite(this.LSR(this.Bus.Data));

        protected byte LSR(byte value)
        {
            this.SetFlag(StatusBits.CF, CarryTest(value));
            return this.Through(value >> 1);
        }

        #endregion

        #region Rotate

        private void ROLA()
        {
            this.SwallowRead();
            this.A = this.ROL(this.A);
        }

        protected void ROL() => this.ModifyWrite(this.ROL(this.Bus.Data));

        protected byte ROL(byte value)
        {
            var carryIn = this.Carry;
            return this.Through(this.ASL(value) | carryIn);
        }

        private void RORA()
        {
            this.SwallowRead();
            this.A = this.ROR(this.A);
        }

        protected void ROR() => this.ModifyWrite(this.ROR(this.Bus.Data));

        protected byte ROR(byte value)
        {
            var carryIn = this.Carry;
            return this.Through(this.LSR(value) | (carryIn << 7));
        }

        #endregion

        #endregion

        #endregion
    }
}