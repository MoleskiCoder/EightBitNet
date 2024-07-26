// <copyright file="M6502Core.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class M6502Core(Bus bus) : LittleEndianProcessor(bus)
    {
        #region Pin controls

        #region NMI pin

        public ref PinLevel NMI => ref this.nmiLine;
        private PinLevel nmiLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingNMI;
        public event EventHandler<EventArgs>? RaisedNMI;
        public event EventHandler<EventArgs>? LoweringNMI;
        public event EventHandler<EventArgs>? LoweredNMI;
        protected virtual void OnRaisingNMI() => this.RaisingNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedNMI() => this.RaisedNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringNMI() => this.LoweringNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredNMI() => this.LoweredNMI?.Invoke(this, EventArgs.Empty);


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
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

        #endregion

        #region SO pin

        public ref PinLevel SO => ref this.soLine;
        private PinLevel soLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingSO;
        public event EventHandler<EventArgs>? RaisedSO;
        public event EventHandler<EventArgs>? LoweringSO;
        public event EventHandler<EventArgs>? LoweredSO;

        protected virtual void OnRaisingSO() => this.RaisingSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSO() => this.RaisedSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringSO() => this.LoweringSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSO() => this.LoweredSO?.Invoke(this, EventArgs.Empty);


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseSO()
        {
            if (this.SO.Lowered())
            {
                this.OnRaisingSO();
                this.SO.Raise();
                this.OnRaisedSO();
            }
        }

        public virtual void LowerSO()
        {
            if (this.SO.Raised())
            {
                this.OnLoweringSO();
                this.SO.Lower();
                this.OnLoweredSO();
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

        protected virtual void OnRaisingSYNC() => this.RaisingSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSYNC() => this.RaisedSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringSYNC() => this.LoweringSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSYNC() => this.LoweredSYNC?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        protected virtual void RaiseSYNC()
        {
            this.OnRaisingSYNC();
            this.SYNC.Raise();
            this.OnRaisedSYNC();
        }

        protected virtual void LowerSYNC()
        {
            this.OnLoweringSYNC();
            this.SYNC.Lower();
            this.OnLoweredSYNC();
        }

        #endregion

        #region RDY pin

        public ref PinLevel RDY => ref this.rdyLine;
        private PinLevel rdyLine = PinLevel.Low;
        public event EventHandler<EventArgs>? RaisingRDY;
        public event EventHandler<EventArgs>? RaisedRDY;
        public event EventHandler<EventArgs>? LoweringRDY;
        public event EventHandler<EventArgs>? LoweredRDY;
        protected virtual void OnRaisingRDY() => this.RaisingRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedRDY() => this.RaisedRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringRDY() => this.LoweringRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredRDY() => this.LoweredRDY?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRDY()
        {
            if (this.RDY.Lowered())
            {
                this.OnRaisingRDY();
                this.RDY.Raise();
                this.OnRaisedRDY();
            }
        }

        public virtual void LowerRDY()
        {
            if (this.RDY.Raised())
            {
                this.OnLoweringRDY();
                this.RDY.Lower();
                this.OnLoweredRDY();
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
        protected virtual void OnRaisingRW() => this.RaisingRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedRW() => this.RaisedRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweringRW() => this.LoweringRW?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredRW() => this.LoweredRW?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRW()
        {
            if (this.RW.Lowered())
            {
                this.OnRaisingRW();
                this.RW.Raise();
                this.OnRaisedRW();
            }
        }

        public virtual void LowerRW()
        {
            if (this.RW.Raised())
            {
                this.OnLoweringRW();
                this.RW.Lower();
                this.OnLoweredRW();
            }
        }

        #endregion

        protected override void OnRaisedPOWER()
        {
            this.X = (byte)Bits.Bit7;
            this.Y = 0;
            this.A = 0;
            this.P = (byte)StatusBits.RF;
            this.S = (byte)Mask.Eight;
            this.LowerSYNC();
            this.LowerRW();
            base.OnRaisedPOWER();
        }

        #endregion

        #region Interrupts

        private const byte IRQvector = 0xfe;  // IRQ vector
        private const byte RSTvector = 0xfc;  // RST vector
        private const byte NMIvector = 0xfa;  // NMI vector

        protected enum InterruptSource { hardware, software };

        protected enum InterruptType { reset, non_reset };

        protected virtual void Interrupt(byte vector, InterruptSource source = InterruptSource.hardware, InterruptType type = InterruptType.non_reset)
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
            this.Jump(this.GetWordPaged(0xff, vector));
        }

        #region Interrupt etc. handlers

        protected override sealed void HandleRESET()
        {
            this.RaiseRESET();
            this.Interrupt(RSTvector, InterruptSource.hardware, InterruptType.reset);
        }

        protected override sealed void HandleINT()
        {
            this.RaiseINT();
            this.Interrupt(IRQvector);
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.Interrupt(NMIvector);
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

        protected void SetFlag(StatusBits flag)
        {
            this.P = SetBit(this.P, flag);
        }

        protected void SetFlag(StatusBits which, int condition)
        {
            this.P = SetBit(this.P, which, condition);
        }

        protected void SetFlag(StatusBits which, bool condition)
        {
            this.P = SetBit(this.P, which, condition);
        }

        protected void ResetFlag(StatusBits which)
        {
            this.P = ClearBit(this.P, which);
        }

        protected void ResetFlag(StatusBits which, int condition)
        {
            this.P = ClearBit(this.P, which, condition);
        }

        #endregion

        #endregion

        #region Cycle wastage

        protected void SwallowRead() => this.MemoryRead(this.PC);

        protected void SwallowPop() => this.MemoryRead(this.S, 1);

        protected void SwallowFetch() => this.FetchByte();

        #endregion

        #region Core instruction dispatching

        public override void Execute()
        {
            this.MaybeExecute();
        }
        
        protected virtual bool MaybeExecute()
        {
            var cycles = this.Cycles;
            switch (this.OpCode)
            {
                case 0x00: this.SwallowFetch(); this.Interrupt(IRQvector, InterruptSource.software); break; // BRK (implied)
                case 0x01: this.IndexedIndirectXRead(); this.OrR(); break;                                  // ORA (indexed indirect X)
                case 0x05: this.ZeroPageRead(); this.OrR(); break;                                          // ORA (zero page)
                case 0x06: this.ZeroPageRead(); this.ModifyWrite(this.ASL()); break;                        // ASL (zero page)
                case 0x08: this.SwallowRead(); this.PHP(); break;                                           // PHP (implied)
                case 0x09: this.ImmediateRead(); this.OrR(); break;                                         // ORA (immediate)
                case 0x0a: this.SwallowRead(); A = this.ASL(A); break;                                      // ASL A (implied)
                case 0x0d: this.AbsoluteRead(); this.OrR(); break;                                          // ORA (absolute)
                case 0x0e: this.AbsoluteRead(); this.ModifyWrite(this.ASL()); break;                        // ASL (absolute)

                case 0x10: this.BranchNot(this.Negative); break;                                            // BPL (relative)
                case 0x11: this.IndirectIndexedYRead(); this.OrR(); break;                                  // ORA (indirect indexed Y)
                case 0x15: this.ZeroPageXRead(); this.OrR(); break;                                         // ORA (zero page, X)
                case 0x16: this.ZeroPageXRead(); this.ModifyWrite(this.ASL()); break;                       // ASL (zero page, X)
                case 0x18: this.SwallowRead(); this.ResetFlag(StatusBits.CF); break;                        // CLC (implied)
                case 0x19: this.AbsoluteYRead(); this.OrR(); break;                                         // ORA (absolute, Y)
                case 0x1d: this.AbsoluteXRead(); this.OrR(); break;                                         // ORA (absolute, X)
                case 0x1e: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.ASL()); break;  // ASL (absolute, X)

                case 0x20: this.JSR(); break;                                                               // JSR (absolute)
                case 0x21: this.IndexedIndirectXRead(); this.AndR(); break;                                 // AND (indexed indirect X)
                case 0x24: this.ZeroPageRead(); this.BIT(); break;                                          // BIT (zero page)
                case 0x25: this.ZeroPageRead(); this.AndR(); break;                                         // AND (zero page)
                case 0x26: this.ZeroPageRead(); this.ModifyWrite(this.ROL()); break;                        // ROL (zero page)
                case 0x28: this.SwallowRead(); this.PLP(); break;                                           // PLP (implied)
                case 0x29: this.ImmediateRead(); this.AndR(); break;                                        // AND (immediate)
                case 0x2a: this.SwallowRead(); this.A = this.ROL(this.A); break;                            // ROL A (implied)
                case 0x2c: this.AbsoluteRead(); this.BIT(); break;                                          // BIT (absolute)
                case 0x2d: this.AbsoluteRead(); this.AndR(); break;                                         // AND (absolute)
                case 0x2e: this.AbsoluteRead(); this.ModifyWrite(this.ROL()); break;                        // ROL (absolute)

                case 0x30: this.Branch(this.Negative); break;                                               // BMI (relative)
                case 0x31: this.IndirectIndexedYRead(); this.AndR(); break;                                 // AND (indirect indexed Y)
                case 0x35: this.ZeroPageXRead(); this.AndR(); break;                                        // AND (zero page, X)
                case 0x36: this.ZeroPageXRead(); this.ModifyWrite(this.ROL()); break;                       // ROL (zero page, X)
                case 0x38: this.SwallowRead(); this.SetFlag(StatusBits.CF); break;                          // SEC (implied)
                case 0x39: this.AbsoluteYRead(); this.AndR(); break;                                        // AND (absolute, Y)
                case 0x3d: this.AbsoluteXRead(); this.AndR(); break;                                        // AND (absolute, X)
                case 0x3e: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.ROL()); break;  // ROL (absolute, X)

                case 0x40: this.SwallowRead(); this.RTI(); break;                                           // RTI (implied)
                case 0x41: this.IndexedIndirectXRead(); this.EorR(); break;                                 // EOR (indexed indirect X)
                case 0x44: this.ZeroPageRead(); break;                                                          // *NOP (zero page)
                case 0x45: this.ZeroPageRead(); this.EorR(); break;                                         // EOR (zero page)
                case 0x46: this.ZeroPageRead(); this.ModifyWrite(this.LSR()); break;                        // LSR (zero page)
                case 0x48: this.SwallowRead(); this.Push(this.A); break;                                    // PHA (implied)
                case 0x49: this.ImmediateRead(); this.EorR(); break;                                        // EOR (immediate)
                case 0x4a: this.SwallowRead(); this.A = this.LSR(this.A); break;                            // LSR A (implied)
                case 0x4c: this.AbsoluteAddress(); this.Jump(this.Bus.Address); break;                      // JMP (absolute)
                case 0x4d: this.AbsoluteRead(); this.EorR(); break;                                         // EOR (absolute)
                case 0x4e: this.AbsoluteRead(); this.ModifyWrite(this.LSR()); break;                        // LSR (absolute)

                case 0x50: this.BranchNot(this.Overflow); break;                                            // BVC (relative)
                case 0x51: this.IndirectIndexedYRead(); this.EorR(); break;                                 // EOR (indirect indexed Y)
                case 0x54: this.ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0x55: this.ZeroPageXRead(); this.EorR(); break;                                        // EOR (zero page, X)
                case 0x56: this.ZeroPageXRead(); this.ModifyWrite(this.LSR()); break;                       // LSR (zero page, X)
                case 0x58: this.SwallowRead(); this.ResetFlag(StatusBits.IF); break;                        // CLI (implied)
                case 0x59: this.AbsoluteYRead(); this.EorR(); break;                                        // EOR (absolute, Y)
                case 0x5d: this.AbsoluteXRead(); this.EorR(); break;                                        // EOR (absolute, X)
                case 0x5e: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.LSR()); break;  // LSR (absolute, X)

                case 0x60: this.SwallowRead(); this.Return(); break;                                        // RTS (implied)
                case 0x61: this.IndexedIndirectXRead(); this.ADC(); break;                                  // ADC (indexed indirect X)
                case 0x65: this.ZeroPageRead(); this.ADC(); break;                                          // ADC (zero page)
                case 0x66: this.ZeroPageRead(); this.ModifyWrite(this.ROR()); break;                        // ROR (zero page)
                case 0x68: this.SwallowRead(); this.SwallowPop(); this.A = this.Through(this.Pop()); break; // PLA (implied)
                case 0x69: this.ImmediateRead(); this.ADC(); break;                                         // ADC (immediate)
                case 0x6a: this.SwallowRead(); this.A = this.ROR(this.A); break;                            // ROR A (implied)
                case 0x6c: this.IndirectAddress(); this.Jump(this.Bus.Address); break;                      // JMP (indirect)
                case 0x6d: this.AbsoluteRead(); this.ADC(); break;                                          // ADC (absolute)
                case 0x6e: this.AbsoluteRead(); this.ModifyWrite(this.ROR()); break;                        // ROR (absolute)

                case 0x70: this.Branch(this.Overflow); break;                                               // BVS (relative)
                case 0x71: this.IndirectIndexedYRead(); this.ADC(); break;                                  // ADC (indirect indexed Y)
                case 0x75: this.ZeroPageXRead(); this.ADC(); break;                                         // ADC (zero page, X)
                case 0x76: this.ZeroPageXRead(); this.ModifyWrite(this.ROR()); break;                       // ROR (zero page, X)
                case 0x78: this.SwallowRead(); this.SetFlag(StatusBits.IF); break;                          // SEI (implied)
                case 0x79: this.AbsoluteYRead(); this.ADC(); break;                                         // ADC (absolute, Y)
                case 0x7d: this.AbsoluteXRead(); this.ADC(); break;                                         // ADC (absolute, X)
                case 0x7e: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.ROR()); break;	// ROR (absolute, X)

                case 0x81: this.IndexedIndirectXAddress(); this.MemoryWrite(this.A); break;                 // STA (indexed indirect X)
                case 0x82: this.ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0x84: this.ZeroPageAddress(); this.MemoryWrite(this.Y); break;                         // STY (zero page)
                case 0x85: this.ZeroPageAddress(); this.MemoryWrite(this.A); break;	                        // STA (zero page)
                case 0x86: this.ZeroPageAddress(); this.MemoryWrite(this.X); break;	                        // STX (zero page)
                case 0x88: this.SwallowRead(); this.Y = this.DEC(this.Y); break;	                        // DEY (implied)
                case 0x8a: this.SwallowRead(); this.A = this.Through(this.X); break;	                    // TXA (implied)
                case 0x8c: this.AbsoluteAddress(); this.MemoryWrite(this.Y); break;	                        // STY (absolute)
                case 0x8d: this.AbsoluteAddress(); this.MemoryWrite(this.A); break;	                        // STA (absolute)
                case 0x8e: this.AbsoluteAddress(); this.MemoryWrite(this.X); break;	                        // STX (absolute)

                case 0x90: this.BranchNot(this.Carry); break;                                               // BCC (relative)
                case 0x91: this.IndirectIndexedYAddress(); this.Fixup(); this.MemoryWrite(this.A); break;   // STA (indirect indexed Y)
                case 0x94: this.ZeroPageXAddress(); this.MemoryWrite(this.Y); break;                        // STY (zero page, X)
                case 0x95: this.ZeroPageXAddress(); this.MemoryWrite(this.A); break;                        // STA (zero page, X)
                case 0x96: this.ZeroPageYAddress(); this.MemoryWrite(this.X); break;                        // STX (zero page, Y)
                case 0x98: this.SwallowRead(); this.A = this.Through(this.Y); break;                        // TYA (implied)
                case 0x99: this.AbsoluteYAddress(); this.Fixup(); this.MemoryWrite(this.A); break;          // STA (absolute, Y)
                case 0x9a: this.SwallowRead(); this.S = this.X; break;                                      // TXS (implied)
                case 0x9d: this.AbsoluteXAddress(); this.Fixup(); this.MemoryWrite(this.A); break;          // STA (absolute, X)

                case 0xa0: this.ImmediateRead(); this.Y = this.Through(); break;                            // LDY (immediate)
                case 0xa1: this.IndexedIndirectXRead(); this.A = this.Through(); break;                     // LDA (indexed indirect X)
                case 0xa2: this.ImmediateRead(); this.X = this.Through(); break;                            // LDX (immediate)
                case 0xa4: this.ZeroPageRead(); this.Y = this.Through(); break;                             // LDY (zero page)
                case 0xa5: this.ZeroPageRead(); this.A = this.Through(); break;                             // LDA (zero page)
                case 0xa6: this.ZeroPageRead(); this.X = this.Through(); break;                             // LDX (zero page)
                case 0xa8: this.SwallowRead(); this.Y = Through(this.A); break;                             // TAY (implied)
                case 0xa9: this.ImmediateRead(); this.A = this.Through(); break;                            // LDA (immediate)
                case 0xaa: this.SwallowRead(); this.X = this.Through(this.A); break;                        // TAX (implied)
                case 0xac: this.AbsoluteRead(); this.Y = this.Through(); break;                             // LDY (absolute)
                case 0xad: this.AbsoluteRead(); this.A = this.Through(); break;                             // LDA (absolute)
                case 0xae: this.AbsoluteRead(); this.X = this.Through(); break;                             // LDX (absolute)

                case 0xb0: this.Branch(this.Carry); break;                                                  // BCS (relative)
                case 0xb1: this.IndirectIndexedYRead(); this.A = this.Through(); break;                     // LDA (indirect indexed Y)
                case 0xb4: this.ZeroPageXRead(); this.Y = this.Through(); break;                            // LDY (zero page, X)
                case 0xb5: this.ZeroPageXRead(); this.A = this.Through(); break;                            // LDA (zero page, X)
                case 0xb6: this.ZeroPageYRead(); this.X = this.Through(); break;                            // LDX (zero page, Y)
                case 0xb8: this.SwallowRead(); this.ResetFlag(StatusBits.VF); break;                        // CLV (implied)
                case 0xb9: this.AbsoluteYRead(); this.A = this.Through(); break;                            // LDA (absolute, Y)
                case 0xba: this.SwallowRead(); this.X = this.Through(this.S); break;                        // TSX (implied)
                case 0xbc: this.AbsoluteXRead(); this.Y = this.Through(); break;                            // LDY (absolute, X)
                case 0xbd: this.AbsoluteXRead(); this.A = this.Through(); break;                            // LDA (absolute, X)
                case 0xbe: this.AbsoluteYRead(); this.X = this.Through(); break;                            // LDX (absolute, Y)

                case 0xc0: this.ImmediateRead(); this.CMP(this.Y); break;                                   // CPY (immediate)
                case 0xc1: this.IndexedIndirectXRead(); this.CMP(this.A); break;                            // CMP (indexed indirect X)
                case 0xc2: this.ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0xc4: this.ZeroPageRead(); this.CMP(this.Y); break;                                    // CPY (zero page)
                case 0xc5: this.ZeroPageRead(); this.CMP(this.A); break;                                    // CMP (zero page)
                case 0xc6: this.ZeroPageRead(); this.ModifyWrite(this.DEC()); break;                        // DEC (zero page)
                case 0xc8: this.SwallowRead(); this.Y = this.INC(this.Y); break;                            // INY (implied)
                case 0xc9: this.ImmediateRead(); this.CMP(this.A); break;                                   // CMP (immediate)
                case 0xca: this.SwallowRead(); this.X = this.DEC(this.X); break;                            // DEX (implied)
                case 0xcc: this.AbsoluteRead(); this.CMP(this.Y); break;                                    // CPY (absolute)
                case 0xcd: this.AbsoluteRead(); this.CMP(this.A); break;                                    // CMP (absolute)
                case 0xce: this.AbsoluteRead(); this.ModifyWrite(this.DEC()); break;                        // DEC (absolute)

                case 0xd0: this.BranchNot(this.Zero); break;                                                // BNE (relative)
                case 0xd1: this.IndirectIndexedYRead(); this.CMP(this.A); break;                            // CMP (indirect indexed Y)
                case 0xd4: this.ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0xd5: this.ZeroPageXRead(); this.CMP(this.A); break;                                   // CMP (zero page, X)
                case 0xd6: this.ZeroPageXRead(); this.ModifyWrite(this.DEC()); break;                       // DEC (zero page, X)
                case 0xd8: this.SwallowRead(); this.ResetFlag(StatusBits.DF); break;                        // CLD (implied)
                case 0xd9: this.AbsoluteYRead(); this.CMP(this.A); break;                                   // CMP (absolute, Y)
                case 0xdd: this.AbsoluteXRead(); this.CMP(this.A); break;                                   // CMP (absolute, X)
                case 0xde: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.DEC()); break;  // DEC (absolute, X)

                case 0xe0: this.ImmediateRead(); this.CMP(this.X); break;                                   // CPX (immediate)
                case 0xe1: this.IndexedIndirectXRead(); this.SBC(); break;                                  // SBC (indexed indirect X)
                case 0xe2: this.ImmediateRead(); break;                                                     // *NOP (immediate)
                case 0xe4: this.ZeroPageRead(); this.CMP(this.X); break;                                    // CPX (zero page)
                case 0xe5: this.ZeroPageRead(); this.SBC(); break;                                          // SBC (zero page)
                case 0xe6: this.ZeroPageRead(); this.ModifyWrite(INC()); break;                             // INC (zero page)
                case 0xe8: this.SwallowRead(); this.X = this.INC(this.X); break;                            // INX (implied)
                case 0xe9: this.ImmediateRead(); this.SBC(); break;                                         // SBC (immediate)
                case 0xea: this.SwallowRead(); break;                                                       // NOP (implied)
                case 0xec: this.AbsoluteRead(); this.CMP(this.X); break;                                    // CPX (absolute)
                case 0xed: this.AbsoluteRead(); this.SBC(); break;                                          // SBC (absolute)
                case 0xee: this.AbsoluteRead(); this.ModifyWrite(this.INC()); break;                        // INC (absolute)

                case 0xf0: this.Branch(this.Zero); break;                                                   // BEQ (relative)
                case 0xf1: this.IndirectIndexedYRead(); this.SBC(); break;                                  // SBC (indirect indexed Y)
                case 0xf4: this.ZeroPageXRead(); break;                                                     // *NOP (zero page, X)
                case 0xf5: this.ZeroPageXRead(); this.SBC(); break;                                         // SBC (zero page, X)
                case 0xf6: this.ZeroPageXRead(); this.ModifyWrite(this.INC()); break;                       // INC (zero page, X)
                case 0xf8: this.SwallowRead(); this.SetFlag(StatusBits.DF); break;                          // SED (implied)
                case 0xf9: this.AbsoluteYRead(); this.SBC(); break;                                         // SBC (absolute, Y)
                case 0xfd: this.AbsoluteXRead(); this.SBC(); break;                                         // SBC (absolute, X)
                case 0xfe: this.AbsoluteXAddress(); this.FixupRead(); this.ModifyWrite(this.INC()); break;  // INC (absolute, X)
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
                this.FetchInstruction();
                if (this.RESET.Lowered())
                {
                    this.HandleRESET();
                }
                else if (this.NMI.Lowered())
                {
                    this.HandleNMI();
                }
                else if (this.INT.Lowered() && (this.InterruptMasked == 0))
                {
                    this.HandleINT();
                }
                else
                {
                    this.Execute();
                }
            }
        }

        private void FetchInstruction()
        {
            // Instruction fetch beginning
            this.LowerSYNC();

            System.Diagnostics.Debug.Assert(this.Cycles == 1, "An extra cycle has occurred");

            // Can't use fetchByte, since that would add an extra tick.
            this.ImmediateAddress();
            this.OpCode = this.ReadFromBus();

            System.Diagnostics.Debug.Assert(this.Cycles == 1, "BUS read has introduced stray cycles");

            // Instruction fetch has now completed
            this.RaiseSYNC();
        }

        #endregion

        #region Bus/Memory access

        protected override sealed void BusWrite()
        {
            this.Tick();
            this.WriteToBus();
        }

        protected override sealed byte BusRead()
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

        private void UpdateStack(byte position)
        {
            this.Bus.Address.Assign(position, 1);
        }

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

        private byte fixedPage;

        public byte FixedPage
        {
            get => this.fixedPage;
            protected set => this.fixedPage = value;
        }

        protected void MaybeFixup()
        {
            if (this.Bus.Address.High != this.FixedPage)
            {
                this.Fixup();
            }
        }

        protected abstract void Fixup();

        protected void MaybeFixupRead()
        {
            this.MaybeFixup();
            this.MemoryRead();
        }

        protected void FixupRead()
        {
            this.Fixup();
            this.MemoryRead();
        }

        #endregion

        #region Address resolution

        protected void NoteFixedAddress(int address)
        {
            this.NoteFixedAddress((ushort)address);
        }

        protected void NoteFixedAddress(ushort address)
        {
            this.Intermediate.Word = address;
            this.FixedPage = this.Intermediate.High;
            this.Bus.Address.Low = this.Intermediate.Low;
        }

        protected void GetAddressPaged()
        {
            this.GetWordPaged();
            this.Bus.Address.Assign(this.Intermediate);
        }

        protected void ImmediateAddress()
        {
            this.Bus.Address.Assign(this.PC);
            ++this.PC.Word;
        }

        protected void AbsoluteAddress() => this.FetchWordAddress();

        protected void ZeroPageAddress()
        {
            this.Bus.Address.Assign(this.FetchByte(), 0);
        }

        protected void ZeroPageIndirectAddress()
        {
            this.ZeroPageAddress();
            this.GetAddressPaged();
        }

        protected abstract void IndirectAddress();

        protected void ZeroPageWithIndexAddress(byte index)
        {
            this.ZeroPageRead();
            this.Bus.Address.Low += index;
        }

        protected void ZeroPageXAddress() => this.ZeroPageWithIndexAddress(this.X);

        protected void ZeroPageYAddress() => this.ZeroPageWithIndexAddress(this.Y);

        private void AbsoluteWithIndexAddress(byte index)
        {
            this.AbsoluteAddress();
            this.NoteFixedAddress(this.Bus.Address.Word + index);
        }

        protected void AbsoluteXAddress() => this.AbsoluteWithIndexAddress(X);

        protected void AbsoluteYAddress() => this.AbsoluteWithIndexAddress(Y);

        protected void IndexedIndirectXAddress()
        {
            this.ZeroPageXAddress();
            this.GetAddressPaged();
        }

        protected void IndirectIndexedYAddress()
        {
            this.ZeroPageIndirectAddress();
            this.NoteFixedAddress(this.Bus.Address.Word + Y);
        }

        #endregion

        #region Address and read

        protected void ImmediateRead()
        {
            this.ImmediateAddress();
            this.MemoryRead();
        }

        protected void AbsoluteRead()
        {
            this.AbsoluteAddress();
            this.MemoryRead();
        }

        protected void ZeroPageRead()
        {
            this.ZeroPageAddress();
            this.MemoryRead();
        }

        protected void ZeroPageXRead()
        {
            this.ZeroPageXAddress();
            this.MemoryRead();
        }

        protected void ZeroPageYRead()
        {
            this.ZeroPageYAddress();
            this.MemoryRead();
        }

        protected void IndexedIndirectXRead()
        {
            this.IndexedIndirectXAddress();
            this.MemoryRead();
        }

        protected void AbsoluteXRead()
        {
            this.AbsoluteXAddress();
            this.MaybeFixupRead();
        }

        protected void AbsoluteYRead()
        {
            this.AbsoluteYAddress();
            this.MaybeFixupRead();
        }

        protected void IndirectIndexedYRead()
        {
            this.IndirectIndexedYAddress();
            this.MaybeFixupRead();
        }

        #endregion

        #endregion

        #region Branching

        private void BranchNot(int condition) => this.Branch(condition == 0);

        private void Branch(int condition) => this.Branch(condition != 0);

        private void Branch(bool condition)
        {
            this.ImmediateRead();
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

        #region Instructions with BCD effects

        #region Addition/subtraction

        #region Subtraction

        protected void AdjustOverflowSubtract(byte operand)
        {
            var data = Bus.Data;
            var intermediate = this.Intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)((operand ^ data) & (operand ^ intermediate))));
        }

        protected void SBC()
        {
            var operand = this.A;
            A = this.SUB(operand, CarryTest((byte)~this.P));

            this.AdjustOverflowSubtract(operand);
            this.AdjustNZ(this.Intermediate.Low);
            this.ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        private byte SUB(byte operand, int borrow) => this.DecimalMasked != 0 ? DecimalSUB(operand, borrow) : BinarySUB(operand, borrow);

        protected byte BinarySUB(byte operand, int borrow = 0)
        {
            var data = Bus.Data;
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
            var data = Bus.Data;
            var intermediate = this.Intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)(~(operand ^ data) & (operand ^ intermediate))));
        }

        protected void ADC()
        {
            this.A = this.DecimalMasked != 0 ? this.DecimalADC() : this.BinaryADC();
        }

        private byte BinaryADC()
        {
            var operand = A;
            var data = Bus.Data;
            this.Intermediate.Word = (ushort)(operand + data + this.Carry);

            this.AdjustOverflowAdd(operand);
            this.SetFlag(StatusBits.CF, CarryTest(this.Intermediate.High));

            this.AdjustNZ(this.Intermediate.Low);

            return this.Intermediate.Low;
        }

        private byte DecimalADC()
        {
            var operand = this.A;
            var data = this.Bus.Data;

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
                this.Intermediate.Word += 0x60;

            this.SetFlag(StatusBits.CF, this.Intermediate.High);

            return (byte)(LowerNibble(LowByte(low)) | HigherNibble(this.Intermediate.Low));
        }

        #endregion

        #endregion

        #endregion

        #region Bitwise operations

        protected void OrR() => this.A = this.Through(this.A | this.Bus.Data);

        protected void AndR() => this.A = this.Through(this.A & this.Bus.Data);

        protected void EorR() => this.A = this.Through(this.A ^ this.Bus.Data);

        private void BIT()
        {
            var data = this.Bus.Data;
            this.SetFlag(StatusBits.VF, OverflowTest(data));
            this.AdjustZero((byte)(this.A & data));
            this.AdjustNegative(data);
        }

        #endregion

        protected void CMP(byte first)
        {
            var second = Bus.Data;
            this.Intermediate.Word = (ushort)(first - second);
            AdjustNZ(this.Intermediate.Low);
            ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        #region Increment/decrement

        protected byte DEC() => this.DEC(this.Bus.Data);

        protected byte DEC(byte value) => this.Through(value - 1);

        protected byte INC() => this.INC(this.Bus.Data);

        protected byte INC(byte value) => this.Through(value + 1);

        #endregion

        #region Stack operations

        private void JSR()
        {
            this.Intermediate.Low = this.FetchByte();
            this.SwallowPop();
            this.PushWord(this.PC);
            this.PC.High = this.FetchByte();
            this.PC.Low = this.Intermediate.Low;
        }

        private void PHP() => this.Push(SetBit(this.P, StatusBits.BF));

        private void PLP()
        {
            this.SwallowPop();
            this.P = ClearBit(SetBit(this.Pop(), StatusBits.RF), StatusBits.BF);
        }

        private void RTI()
        {
            this.PLP();
            base.Return();
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

        protected byte ASL() => this.ASL(this.Bus.Data);

        protected byte ASL(byte value)
        {
            this.SetFlag(StatusBits.CF, NegativeTest(value));
            return this.Through(value << 1);
        }

        protected byte LSR() => this.LSR(this.Bus.Data);

        protected byte LSR(byte value)
        {
            this.SetFlag(StatusBits.CF, CarryTest(value));
            return this.Through(value >> 1);
        }

        #endregion

        #region Rotate

        protected byte ROL() => this.ROL(this.Bus.Data);

        protected byte ROL(byte value)
        {
            var carryIn = this.Carry;
            return this.Through(this.ASL(value) | carryIn);
        }

        protected byte ROR() => this.ROR(this.Bus.Data);

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