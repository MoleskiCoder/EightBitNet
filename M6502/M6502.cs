namespace EightBit
{
    using System;

    public class M6502 : LittleEndianProcessor
    {
        private const byte IRQvector = 0xfe;  // IRQ vector
        private const byte RSTvector = 0xfc;  // RST vector
        private const byte NMIvector = 0xfa;  // NMI vector

        private byte x;
        private byte y;
        private byte a;
        private byte s;
        private byte p;

        private Register16 intermediate;

        private bool handlingRESET;
        private bool handlingNMI;
        private bool handlingINT;

        private PinLevel nmiLine;
        private PinLevel soLine;
        private PinLevel syncLine;
        private PinLevel rdyLine;

        public M6502(Bus bus)
        : base(bus)
        {
            intermediate = new Register16();
        }

        public event EventHandler<EventArgs> ExecutingInstruction;
        public event EventHandler<EventArgs> ExecutedInstruction;

        public event EventHandler<EventArgs> RaisingNMI;
        public event EventHandler<EventArgs> RaisedNMI;
        public event EventHandler<EventArgs> LoweringNMI;
        public event EventHandler<EventArgs> LoweredNMI;

        public event EventHandler<EventArgs> RaisingSO;
        public event EventHandler<EventArgs> RaisedSO;
        public event EventHandler<EventArgs> LoweringSO;
        public event EventHandler<EventArgs> LoweredSO;

        public event EventHandler<EventArgs> RaisingSYNC;
        public event EventHandler<EventArgs> RaisedSYNC;
        public event EventHandler<EventArgs> LoweringSYNC;
        public event EventHandler<EventArgs> LoweredSYNC;

        public event EventHandler<EventArgs> RaisingRDY;
        public event EventHandler<EventArgs> RaisedRDY;
        public event EventHandler<EventArgs> LoweringRDY;
        public event EventHandler<EventArgs> LoweredRDY;

        public byte X { get => x; set => x = value; }
        public byte Y { get => y; set { y = value; } }
        public byte A { get => a; set { a = value; } }
        public byte S { get => s; set { s = value; } }
        public byte P { get => p; set { p = value; } }

        private int InterruptMasked => P & (byte)StatusBits.IF;
        private int Decimal => P & (byte)StatusBits.DF;
        private int Negative => P & (byte)StatusBits.NF;
        private int Zero => P & (byte)StatusBits.ZF;
        private int Overflow => P & (byte)StatusBits.VF;
        private int Carry => P & (byte)StatusBits.CF;

        public ref PinLevel NMI() => ref nmiLine;
        public ref PinLevel SO() => ref soLine;
        public ref PinLevel SYNC() => ref syncLine;
        public ref PinLevel RDY() => ref rdyLine;

        public virtual void RaiseNMI()
        {
            OnRaisingNMI();
            NMI().Raise();
            OnRaisedNMI();
        }

        public virtual void LowerNMI()
        {
            OnLoweringNMI();
            NMI().Lower();
            OnLoweredNMI();
        }

        public virtual void RaiseSO()
        {
            OnRaisingSO();
            SO().Raise();
            OnRaisedSO();
        }

        public virtual void LowerSO()
        {
            OnLoweringSO();
            SO().Lower();
            OnLoweredSO();
        }

        protected virtual void RaiseSYNC()
        {
            OnRaisingSYNC();
            SYNC().Raise();
            OnRaisedSYNC();
        }

        protected virtual void LowerSYNC()
        {
            OnLoweringSYNC();
            SYNC().Lower();
            OnLoweredSYNC();
        }

        public virtual void RaiseRDY()
        {
            OnRaisingRDY();
            RDY().Raise();
            OnRaisedRDY();
        }

        public virtual void LowerRDY()
        {
            OnLoweringRDY();
            RDY().Lower();
            OnLoweredRDY();
        }

        protected virtual void OnExecutingInstruction() => ExecutingInstruction?.Invoke(this, EventArgs.Empty);
        protected virtual void OnExecutedInstruction() => ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingNMI() => RaisingNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedNMI() => RaisedNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringNMI() => LoweringNMI?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredNMI() => LoweredNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingSO() => RaisingSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSO() => RaisedSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringSO() => LoweringSO?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSO() => LoweredSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingSYNC() => RaisingSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedSYNC() => RaisedSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringSYNC() => LoweringSYNC?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredSYNC() => LoweredSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRDY() => RaisingRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedRDY() => RaisedRDY?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRDY() => LoweringRDY?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredRDY() => LoweredRDY?.Invoke(this, EventArgs.Empty);

        public override int Execute()
        {
            RaiseSYNC();    // Instruction fetch has now completed

            switch (OpCode)
            {
                case 0x00: FetchByte(); Interrupt(); break;                                                     // BRK (implied)
                case 0x01: A = OrR(A, AM_IndexedIndirectX()); break;                                            // ORA (indexed indirect X)
                case 0x02: break;
                case 0x03: SLO(AM_IndexedIndirectX()); break;                                                   // *SLO (indexed indirect X)
                case 0x04: AM_ZeroPage(); break;                                                                // *NOP (zero page)
                case 0x05: A = OrR(A, AM_ZeroPage()); break;                                                    // ORA (zero page)
                case 0x06: BusReadModifyWrite(ASL(AM_ZeroPage())); break;                                       // ASL (zero page)
                case 0x07: SLO(AM_ZeroPage()); break;                                                           // *SLO (zero page)
                case 0x08: BusRead(); PHP(); break;                                                             // PHP (implied)
                case 0x09: A = OrR(A, AM_Immediate()); break;                                                   // ORA (immediate)
                case 0x0a: BusRead(); A = ASL(A); break;                                                        // ASL A (implied)
                case 0x0b: ANC(AM_Immediate()); break;                                                          // *ANC (immediate)
                case 0x0c: AM_Absolute(); break;                                                                // *NOP (absolute)
                case 0x0d: A = OrR(A, AM_Absolute()); break;                                                    // ORA (absolute)
                case 0x0e: BusReadModifyWrite(ASL(AM_Absolute())); break;                                       // ASL (absolute)
                case 0x0f: SLO(AM_Absolute()); break;                                                           // *SLO (absolute)

                case 0x10: Branch(Negative == 0); break;                                                        // BPL (relative)
                case 0x11: A = OrR(A, AM_IndirectIndexedY()); break;                                            // ORA (indirect indexed Y)
                case 0x12: break;
                case 0x13: SLO(AM_IndirectIndexedY()); break;                                                   // *SLO (indirect indexed Y)
                case 0x14: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0x15: A = OrR(A, AM_ZeroPageX()); break;                                                   // ORA (zero page, X)
                case 0x16: BusReadModifyWrite(ASL(AM_ZeroPageX())); break;                                      // ASL (zero page, X)
                case 0x17: SLO(AM_ZeroPageX()); break;                                                          // *SLO (zero page, X)
                case 0x18: BusRead(); ClearFlag(ref p, StatusBits.CF); break;                                   // CLC (implied)
                case 0x19: A = OrR(A, AM_AbsoluteY()); break;                                                   // ORA (absolute, Y)
                case 0x1a: BusRead(); break;                                                                    // *NOP (implied)
                case 0x1b: SLO(AM_AbsoluteY()); break;                                                          // *SLO (absolute, Y)
                case 0x1c: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0x1d: A = OrR(A, AM_AbsoluteX()); break;                                                   // ORA (absolute, X)
                case 0x1e: BusReadModifyWrite(ASL(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // ASL (absolute, X)
                case 0x1f: SLO(AM_AbsoluteX()); break;                                                          // *SLO (absolute, X)

                case 0x20: JSR(); break;                                                                        // JSR (absolute)
                case 0x21: A = AndR(A, AM_IndexedIndirectX()); break;                                           // AND (indexed indirect X)
                case 0x22: break;
                case 0x23: RLA(AM_IndexedIndirectX()); break;                                                   // *RLA (indexed indirect X)
                case 0x24: BIT(A, AM_ZeroPage()); break;                                                        // BIT (zero page)
                case 0x25: A = AndR(A, AM_ZeroPage()); break;                                                   // AND (zero page)
                case 0x26: BusReadModifyWrite(ROL(AM_ZeroPage())); break;                                       // ROL (zero page)
                case 0x27: RLA(AM_ZeroPage()); break;                                                           // *RLA (zero page)
                case 0x28: BusRead(); GetBytePaged(1, S); PLP(); break;                                         // PLP (implied)
                case 0x29: A = AndR(A, AM_Immediate()); break;                                                  // AND (immediate)
                case 0x2a: BusRead(); A = ROL(A); break;                                                        // ROL A (implied)
                case 0x2b: ANC(AM_Immediate()); break;                                                          // *ANC (immediate)
                case 0x2c: BIT(A, AM_Absolute()); break;                                                        // BIT (absolute)
                case 0x2d: A = AndR(A, AM_Absolute()); break;                                                   // AND (absolute)
                case 0x2e: BusReadModifyWrite(ROL(AM_Absolute())); break;                                       // ROL (absolute)
                case 0x2f: RLA(AM_Absolute()); break;                                                           // *RLA (absolute)

                case 0x30: Branch(Negative != 0); break;                                                        // BMI (relative)
                case 0x31: A = AndR(A, AM_IndirectIndexedY()); break;                                           // AND (indirect indexed Y)
                case 0x32: break;
                case 0x33: RLA(AM_IndirectIndexedY()); break;                                                   // *RLA (indirect indexed Y)
                case 0x34: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0x35: A = AndR(A, AM_ZeroPageX()); break;                                                  // AND (zero page, X)
                case 0x36: BusReadModifyWrite(ROL(AM_ZeroPageX())); break;                                      // ROL (zero page, X)
                case 0x37: RLA(AM_ZeroPageX()); break;                                                          // *RLA (zero page, X)
                case 0x38: BusRead(); SetFlag(ref p, StatusBits.CF); break;                                     // SEC (implied)
                case 0x39: A = AndR(A, AM_AbsoluteY()); break;                                                  // AND (absolute, Y)
                case 0x3a: BusRead(); break;                                                                    // *NOP (implied)
                case 0x3b: RLA(AM_AbsoluteY()); break;                                                          // *RLA (absolute, Y)
                case 0x3c: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0x3d: A = AndR(A, AM_AbsoluteX()); break;                                                  // AND (absolute, X)
                case 0x3e: BusReadModifyWrite(ROL(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // ROL (absolute, X)
                case 0x3f: RLA(AM_AbsoluteX()); break;                                                          // *RLA (absolute, X)

                case 0x40: BusRead(); RTI(); break;                                                             // RTI (implied)
                case 0x41: A = EorR(A, AM_IndexedIndirectX()); break;                                           // EOR (indexed indirect X)
                case 0x42: break;
                case 0x43: SRE(AM_IndexedIndirectX()); break;                                                   // *SRE (indexed indirect X)
                case 0x44: AM_ZeroPage(); break;                                                                // *NOP (zero page)
                case 0x45: A = EorR(A, AM_ZeroPage()); break;                                                   // EOR (zero page)
                case 0x46: BusReadModifyWrite(LSR(AM_ZeroPage())); break;                                       // LSR (zero page)
                case 0x47: SRE(AM_ZeroPage()); break;                                                           // *SRE (zero page)
                case 0x48: BusRead(); Push(A); break;                                                           // PHA (implied)
                case 0x49: A = EorR(A, AM_Immediate()); break;                                                  // EOR (immediate)
                case 0x4a: BusRead(); A = LSR(A); break;                                                        // LSR A (implied)
                case 0x4b: ASR(AM_Immediate()); break;                                                          // *ASR (immediate)
                case 0x4c: Jump(Address_Absolute()); break;                                                     // JMP (absolute)
                case 0x4d: A = EorR(A, AM_Absolute()); break;                                                   // EOR (absolute)
                case 0x4e: BusReadModifyWrite(LSR(AM_Absolute())); break;                                       // LSR (absolute)
                case 0x4f: SRE(AM_Absolute()); break;                                                           // *SRE (absolute)

                case 0x50: Branch(Overflow == 0); break;                                                        // BVC (relative)
                case 0x51: A = EorR(A, AM_IndirectIndexedY()); break;                                           // EOR (indirect indexed Y)
                case 0x52: break;
                case 0x53: SRE(AM_IndirectIndexedY()); break;                                                   // *SRE (indirect indexed Y)
                case 0x54: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0x55: A = EorR(A, AM_ZeroPageX()); break;                                                  // EOR (zero page, X)
                case 0x56: BusReadModifyWrite(LSR(AM_ZeroPageX())); break;                                      // LSR (zero page, X)
                case 0x57: SRE(AM_ZeroPageX()); break;                                                          // *SRE (zero page, X)
                case 0x58: BusRead(); ClearFlag(ref p, StatusBits.IF); break;                                   // CLI (implied)
                case 0x59: A = EorR(A, AM_AbsoluteY()); break;                                                  // EOR (absolute, Y)
                case 0x5a: BusRead(); break;                                                                    // *NOP (implied)
                case 0x5b: SRE(AM_AbsoluteY()); break;                                                          // *SRE (absolute, Y)
                case 0x5c: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0x5d: A = EorR(A, AM_AbsoluteX()); break;                                                  // EOR (absolute, X)
                case 0x5e: BusReadModifyWrite(LSR(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // LSR (absolute, X)
                case 0x5f: SRE(AM_AbsoluteX()); break;                                                          // *SRE (absolute, X)

                case 0x60: BusRead(); RTS(); break;                                                             // RTS (implied)
                case 0x61: A = ADC(A, AM_IndexedIndirectX()); break;                                            // ADC (indexed indirect X)
                case 0x62: break;
                case 0x63: RRA(AM_IndexedIndirectX()); break;                                                   // *RRA (indexed indirect X)
                case 0x64: AM_ZeroPage(); break;                                                                // *NOP (zero page)
                case 0x65: A = ADC(A, AM_ZeroPage()); break;                                                    // ADC (zero page)
                case 0x66: BusReadModifyWrite(ROR(AM_ZeroPage())); break;                                       // ROR (zero page)
                case 0x67: RRA(AM_ZeroPage()); break;                                                           // *RRA (zero page)
                case 0x68: BusRead(); GetBytePaged(1, S); A = Through(Pop()); break;                            // PLA (implied)
                case 0x69: A = ADC(A, AM_Immediate()); break;                                                   // ADC (immediate)
                case 0x6a: BusRead(); A = ROR(A); break;                                                        // ROR A (implied)
                case 0x6b: ARR(AM_Immediate()); break;                                                          // *ARR (immediate)
                case 0x6c: Jump(Address_Indirect()); break;                                                     // JMP (indirect)
                case 0x6d: A = ADC(A, AM_Absolute()); break;                                                    // ADC (absolute)
                case 0x6e: BusReadModifyWrite(ROR(AM_Absolute())); break;                                       // ROR (absolute)
                case 0x6f: RRA(AM_Absolute()); break;                                                           // *RRA (absolute)

                case 0x70: Branch(Overflow != 0); break;                                                        // BVS (relative)
                case 0x71: A = ADC(A, AM_IndirectIndexedY()); break;                                            // ADC (indirect indexed Y)
                case 0x72: break;
                case 0x73: RRA(AM_IndirectIndexedY()); break;                                                   // *RRA (indirect indexed Y)
                case 0x74: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0x75: A = ADC(A, AM_ZeroPageX()); break;                                                   // ADC (zero page, X)
                case 0x76: BusReadModifyWrite(ROR(AM_ZeroPageX())); break;                                      // ROR (zero page, X)
                case 0x77: RRA(AM_ZeroPageX()); break;                                                          // *RRA (zero page, X)
                case 0x78: BusRead(); SetFlag(ref p, StatusBits.IF); break;                                     // SEI (implied)
                case 0x79: A = ADC(A, AM_AbsoluteY()); break;                                                   // ADC (absolute, Y)
                case 0x7a: BusRead(); break;                                                                    // *NOP (implied)
                case 0x7b: RRA(AM_AbsoluteY()); break;                                                          // *RRA (absolute, Y)
                case 0x7c: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0x7d: A = ADC(A, AM_AbsoluteX()); break;                                                   // ADC (absolute, X)
                case 0x7e: BusReadModifyWrite(ROR(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // ROR (absolute, X)
                case 0x7f: RRA(AM_AbsoluteX()); break;                                                          // *RRA (absolute, X)

                case 0x80: AM_Immediate(); break;                                                               // *NOP (immediate)
                case 0x81: BusWrite(Address_IndexedIndirectX(), A); break;                                      // STA (indexed indirect X)
                case 0x82: AM_Immediate(); break;                                                               // *NOP (immediate)
                case 0x83: BusWrite(Address_IndexedIndirectX(), (byte)(A & X)); break;                          // *SAX (indexed indirect X)
                case 0x84: BusWrite(Address_ZeroPage(), Y); break;                                              // STY (zero page)
                case 0x85: BusWrite(Address_ZeroPage(), A); break;                                              // STA (zero page)
                case 0x86: BusWrite(Address_ZeroPage(), X); break;                                              // STX (zero page)
                case 0x87: BusWrite(Address_ZeroPage(), (byte)(A & X)); break;                                  // *SAX (zero page)
                case 0x88: BusRead(); Y = DEC(Y); break;                                                        // DEY (implied)
                case 0x89: AM_Immediate(); break;                                                               // *NOP (immediate)
                case 0x8a: BusRead(); A = Through(X); break;                                                    // TXA (implied)
                case 0x8b: break;
                case 0x8c: BusWrite(Address_Absolute(), Y); break;                                              // STY (absolute)
                case 0x8d: BusWrite(Address_Absolute(), A); break;                                              // STA (absolute)
                case 0x8e: BusWrite(Address_Absolute(), X); break;                                              // STX (absolute)
                case 0x8f: BusWrite(Address_Absolute(), (byte)(A & X)); break;                                  // *SAX (absolute)

                case 0x90: Branch(Carry == 0); break;                                                           // BCC (relative)
                case 0x91: AM_IndirectIndexedY(); BusWrite(A); break;                                           // STA (indirect indexed Y)
                case 0x92: break;
                case 0x93: break;
                case 0x94: BusWrite(Address_ZeroPageX(), Y); break;                                             // STY (zero page, X)
                case 0x95: BusWrite(Address_ZeroPageX(), A); break;                                             // STA (zero page, X)
                case 0x96: BusWrite(Address_ZeroPageY(), X); break;                                             // STX (zero page, Y)
                case 0x97: BusWrite(Address_ZeroPageY(), (byte)(A & X)); break;                                 // *SAX (zero page, Y)
                case 0x98: BusRead(); A = Through(Y); break;                                                    // TYA (implied)
                case 0x99: STA_AbsoluteY(); break;                                                              // STA (absolute, Y)
                case 0x9a: BusRead(); S = X; break;                                                             // TXS (implied)
                case 0x9b: break;
                case 0x9c: break;
                case 0x9d: STA_AbsoluteX(); break;                                                              // STA (absolute, X)
                case 0x9e: break;
                case 0x9f: break;

                case 0xa0: Y = Through(AM_Immediate()); break;                                                  // LDY (immediate)
                case 0xa1: A = Through(AM_IndexedIndirectX()); break;                                           // LDA (indexed indirect X)
                case 0xa2: X = Through(AM_Immediate()); break;                                                  // LDX (immediate)
                case 0xa3: A = X = Through(AM_IndexedIndirectX()); break;                                       // *LAX (indexed indirect X)
                case 0xa4: Y = Through(AM_ZeroPage()); break;                                                   // LDY (zero page)
                case 0xa5: A = Through(AM_ZeroPage()); break;                                                   // LDA (zero page)
                case 0xa6: X = Through(AM_ZeroPage()); break;                                                   // LDX (zero page)
                case 0xa7: A = X = Through(AM_ZeroPage()); break;                                               // *LAX (zero page)
                case 0xa8: BusRead(); Y = Through(A); break;                                                    // TAY (implied)
                case 0xa9: A = Through(AM_Immediate()); break;                                                  // LDA (immediate)
                case 0xaa: BusRead(); X = Through(A); break;                                                    // TAX (implied)
                case 0xab: A = X = Through(AM_Immediate()); break;                                              // *ATX (immediate)
                case 0xac: Y = Through(AM_Absolute()); break;                                                   // LDY (absolute)
                case 0xad: A = Through(AM_Absolute()); break;                                                   // LDA (absolute)
                case 0xae: X = Through(AM_Absolute()); break;                                                   // LDX (absolute)
                case 0xaf: A = X = Through(AM_Absolute()); break;                                               // *LAX (absolute)

                case 0xb0: Branch(Carry != 0); break;                                                           // BCS (relative)
                case 0xb1: A = Through(AM_IndirectIndexedY()); break;                                           // LDA (indirect indexed Y)
                case 0xb2: break;
                case 0xb3: A = X = Through(AM_IndirectIndexedY()); break;                                       // *LAX (indirect indexed Y)
                case 0xb4: Y = Through(AM_ZeroPageX()); break;                                                  // LDY (zero page, X)
                case 0xb5: A = Through(AM_ZeroPageX()); break;                                                  // LDA (zero page, X)
                case 0xb6: X = Through(AM_ZeroPageY()); break;                                                  // LDX (zero page, Y)
                case 0xb7: A = X = Through(AM_ZeroPageY()); break;                                              // *LAX (zero page, Y)
                case 0xb8: BusRead(); ClearFlag(ref p, StatusBits.VF); break;                                   // CLV (implied)
                case 0xb9: A = Through(AM_AbsoluteY()); break;                                                  // LDA (absolute, Y)
                case 0xba: BusRead(); X = Through(S); break;                                                    // TSX (implied)
                case 0xbb: break;
                case 0xbc: Y = Through(AM_AbsoluteX()); break;                                                  // LDY (absolute, X)
                case 0xbd: A = Through(AM_AbsoluteX()); break;                                                  // LDA (absolute, X)
                case 0xbe: X = Through(AM_AbsoluteY()); break;                                                  // LDX (absolute, Y)
                case 0xbf: A = X = Through(AM_AbsoluteY()); break;                                              // *LAX (absolute, Y)

                case 0xc0: CMP(Y, AM_Immediate()); break;                                                       // CPY (immediate)
                case 0xc1: CMP(A, AM_IndexedIndirectX()); break;                                                // CMP (indexed indirect X)
                case 0xc2: AM_Immediate(); break;                                                               // *NOP (immediate)
                case 0xc3: DCP(AM_IndexedIndirectX()); break;                                                   // *DCP (indexed indirect X)
                case 0xc4: CMP(Y, AM_ZeroPage()); break;                                                        // CPY (zero page)
                case 0xc5: CMP(A, AM_ZeroPage()); break;                                                        // CMP (zero page)
                case 0xc6: BusReadModifyWrite(DEC(AM_ZeroPage())); break;                                       // DEC (zero page)
                case 0xc7: DCP(AM_ZeroPage()); break;                                                           // *DCP (zero page)
                case 0xc8: BusRead(); Y = INC(Y); break;                                                        // INY (implied)
                case 0xc9: CMP(A, AM_Immediate()); break;                                                       // CMP (immediate)
                case 0xca: BusRead(); X = DEC(X); break;                                                        // DEX (implied)
                case 0xcb: AXS(AM_Immediate()); break;                                                          // *AXS (immediate)
                case 0xcc: CMP(Y, AM_Absolute()); break;                                                        // CPY (absolute)
                case 0xcd: CMP(A, AM_Absolute()); break;                                                        // CMP (absolute)
                case 0xce: BusReadModifyWrite(DEC(AM_Absolute())); break;                                       // DEC (absolute)
                case 0xcf: DCP(AM_Absolute()); break;                                                           // *DCP (absolute)

                case 0xd0: Branch(Zero == 0); break;                                                            // BNE (relative)
                case 0xd1: CMP(A, AM_IndirectIndexedY()); break;                                                // CMP (indirect indexed Y)
                case 0xd2: break;
                case 0xd3: DCP(AM_IndirectIndexedY()); break;                                                   // *DCP (indirect indexed Y)
                case 0xd4: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0xd5: CMP(A, AM_ZeroPageX()); break;                                                       // CMP (zero page, X)
                case 0xd6: BusReadModifyWrite(DEC(AM_ZeroPageX())); break;                                      // DEC (zero page, X)
                case 0xd7: DCP(AM_ZeroPageX()); break;                                                          // *DCP (zero page, X)
                case 0xd8: BusRead(); ClearFlag(ref p, StatusBits.DF); break;                                   // CLD (implied)
                case 0xd9: CMP(A, AM_AbsoluteY()); break;                                                       // CMP (absolute, Y)
                case 0xda: BusRead(); break;                                                                    // *NOP (implied)
                case 0xdb: DCP(AM_AbsoluteY()); break;                                                          // *DCP (absolute, Y)
                case 0xdc: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0xdd: CMP(A, AM_AbsoluteX()); break;                                                       // CMP (absolute, X)
                case 0xde: BusReadModifyWrite(DEC(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // DEC (absolute, X)
                case 0xdf: DCP(AM_AbsoluteX()); break;                                                          // *DCP (absolute, X)

                case 0xe0: CMP(X, AM_Immediate()); break;                                                       // CPX (immediate)
                case 0xe1: A = SBC(A, AM_IndexedIndirectX()); break;                                            // SBC (indexed indirect X)
                case 0xe2: AM_Immediate(); break;                                                               // *NOP (immediate)
                case 0xe3: ISB(AM_IndexedIndirectX()); break;                                                   // *ISB (indexed indirect X)
                case 0xe4: CMP(X, AM_ZeroPage()); break;                                                        // CPX (zero page)
                case 0xe5: A = SBC(A, AM_ZeroPage()); break;                                                    // SBC (zero page)
                case 0xe6: BusReadModifyWrite(INC(AM_ZeroPage())); break;                                       // INC (zero page)
                case 0xe7: ISB(AM_ZeroPage()); break;                                                           // *ISB (zero page)
                case 0xe8: BusRead(); X = INC(X); break;                                                        // INX (implied)
                case 0xe9: A = SBC(A, AM_Immediate()); break;                                                   // SBC (immediate)
                case 0xea: BusRead(); break;                                                                    // NOP (implied)
                case 0xeb: A = SBC(A, AM_Immediate()); break;                                                   // *SBC (immediate)
                case 0xec: CMP(X, AM_Absolute()); break;                                                        // CPX (absolute)
                case 0xed: A = SBC(A, AM_Absolute()); break;                                                    // SBC (absolute)
                case 0xee: BusReadModifyWrite(INC(AM_Absolute())); break;                                       // *ISB (absolute)

                case 0xf0: Branch(Zero != 0); break;                                                            // BEQ (relative)
                case 0xf1: A = SBC(A, AM_IndirectIndexedY()); break;                                            // SBC (indirect indexed Y)
                case 0xf2: break;
                case 0xf3: ISB(AM_IndirectIndexedY()); break;                                                   // *ISB (indirect indexed Y)
                case 0xf4: AM_ZeroPageX(); break;                                                               // *NOP (zero page, X)
                case 0xf5: A = SBC(A, AM_ZeroPageX()); break;                                                   // SBC (zero page, X)
                case 0xf6: BusReadModifyWrite(INC(AM_ZeroPageX())); break;                                      // INC (zero page, X)
                case 0xf7: ISB(AM_ZeroPageX()); break;                                                          // *ISB (zero page, X)
                case 0xf8: BusRead(); SetFlag(ref p, StatusBits.DF); break;                                     // SED (implied)
                case 0xf9: A = SBC(A, AM_AbsoluteY()); break;                                                   // SBC (absolute, Y)
                case 0xfa: BusRead(); break;                                                                    // *NOP (implied)
                case 0xfb: ISB(AM_AbsoluteY()); break;                                                          // *ISB (absolute, Y)
                case 0xfc: AM_AbsoluteX(); break;                                                               // *NOP (absolute, X)
                case 0xfd: A = SBC(A, AM_AbsoluteX()); break;                                                   // SBC (absolute, X)
                case 0xfe: BusReadModifyWrite(INC(AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;  // INC (absolute, X)
                case 0xff: ISB(AM_AbsoluteX()); break;                                                          // *ISB (absolute, X)
            }

            return Cycles;
        }

        public override int Step()
        {
            ResetCycles();
            OnExecutingInstruction();
            if (Powered)
            {
                Tick();
                if (SO().Lowered())
                    HandleSO();
                if (RDY().Raised())
                {
                    LowerSYNC();    // Instruction fetch beginning
                    OpCode = Bus.Read(PC++);  // can't use fetchByte
                    if (RESET().Lowered())
                        HandleRESET();
                    else if (NMI().Lowered())
                        HandleNMI();
                    else if (INT().Lowered() && (InterruptMasked == 0))
                        HandleINT();
                    Execute();
                }
            }
            OnExecutedInstruction();
            return Cycles;
        }

        protected override byte Pop() => GetBytePaged(1, ++S);

        protected override void Push(byte value) => SetBytePaged(1, S--, value);

        protected override sealed void HandleRESET()
        {
            RaiseRESET();
            handlingRESET = true;
            OpCode = 0x00;	// BRK
        }

        protected override sealed void HandleINT()
        {
            RaiseINT();
            handlingINT = true;
            OpCode = 0x00;	// BRK
        }

        private void HandleNMI()
        {
            RaiseNMI();
            handlingNMI = true;
            OpCode = 0x00;	// BRK
        }

        private void HandleSO()
        {
            RaiseSO();
            P |= (byte)StatusBits.VF;
        }

        private void Interrupt()
        {
            var reset = handlingRESET;
            var nmi = handlingNMI;
            var irq = handlingINT;
            var hardware = nmi || irq || reset;
            var software = !hardware;
            if (reset)
            {
                DummyPush(PC.High);
                DummyPush(PC.Low);
                DummyPush(P);
            }
            else
            {
                PushWord(PC);
                Push((byte)(P | (int)(software ? StatusBits.BF : 0)));
            }
            SetFlag(ref p, StatusBits.IF);   // Disable IRQ
            var vector = reset ? RSTvector : (nmi ? NMIvector : IRQvector);
            Jump(GetWordPaged(0xff, vector));
            handlingRESET = handlingNMI = handlingINT = false;
        }

        private void DummyPush(byte value)
        {
            Tick();
            Bus.Data = value;
            Bus.Address.Low = S--;
            Bus.Address.High = 1;
        }

        // Addressing modes

        private Register16 Address_Absolute() => FetchWord();

        private byte Address_ZeroPage() => FetchByte();

        private Register16 Address_ZeroPageIndirect() => GetWordPaged(0, Address_ZeroPage());

        private Register16 Address_Indirect()
        {
            var address = Address_Absolute();
            return GetWordPaged(address.High, address.Low);
        }

        private byte Address_ZeroPageX()
        {
            var address = Address_ZeroPage();
            BusRead(address);
            return (byte)LowByte(address + X);
        }

        private byte Address_ZeroPageY()
        {
            var address = Address_ZeroPage();
            BusRead(address);
            return (byte)LowByte(address + Y);
        }

        private Tuple<Register16, byte> Address_AbsoluteX()
        {
            var address = Address_Absolute();
            var page = address.High;
            address.Word += X;
            return new Tuple<Register16, byte>(address, page);
        }

        private Tuple<Register16, byte> Address_AbsoluteY()
        {
            var address = Address_Absolute();
            var page = address.High;
            address.Word += Y;
            return new Tuple<Register16, byte>(address, page);
        }

        private Register16 Address_IndexedIndirectX() => GetWordPaged(0, Address_ZeroPageX());

        private Tuple<Register16, byte> Address_IndirectIndexedY()
        {
            var address = Address_ZeroPageIndirect();
            var page = address.High;
            address.Word += Y;
            return new Tuple<Register16, byte>(address, page);
        }

        private Register16 Address_relative_byte()
        {
            intermediate.Word = (ushort)(PC + (byte)FetchByte());
            return intermediate;
        }

        //

        private byte AM_Immediate() => FetchByte();

        private byte AM_Absolute() => BusRead(Address_Absolute());

        private byte AM_ZeroPage() => BusRead(Address_ZeroPage());

        private byte AM_AbsoluteX(PageCrossingBehavior behaviour = PageCrossingBehavior.MaybeReadTwice)
        {
            var crossed = Address_AbsoluteX();
            var address = crossed.Item1;
            var page = crossed.Item2;
            var possible = GetBytePaged(page, address.Low);
        	if ((behaviour == PageCrossingBehavior.AlwaysReadTwice) || (page != address.High))
		        possible = BusRead(address);
	        return possible;
        }

        private byte AM_AbsoluteY()
        {
            var crossed = Address_AbsoluteY();
            var address = crossed.Item1;
            var page = crossed.Item2;
            var possible = GetBytePaged(page, address.Low);
            if (page != address.High)
                possible = BusRead(address);
            return possible;
        }

        private byte AM_ZeroPageX() => BusRead(Address_ZeroPageX());

        private byte AM_ZeroPageY() => BusRead(Address_ZeroPageY());

        private byte AM_IndexedIndirectX() => BusRead(Address_IndexedIndirectX());

        private byte AM_IndirectIndexedY()
        {
            var crossed = Address_IndirectIndexedY();
            var address = crossed.Item1;
            var page = crossed.Item2;
            var possible = GetBytePaged(page, address.Low);
            if (page != address.High)
                possible = BusRead(address);
            return possible;
        }

        // Flag adjustment

        public static void SetFlag(ref byte f, StatusBits flag) => SetFlag(ref f, (byte)flag);
        private static void SetFlag(ref byte f, StatusBits flag, int condition) => SetFlag(ref f, (byte)flag, condition);
        private static void SetFlag(ref byte f, StatusBits flag, bool condition) => SetFlag(ref f, (byte)flag, condition);
        public static void ClearFlag(ref byte f, StatusBits flag) => ClearFlag(ref f, (byte)flag);
        private static void ClearFlag(ref byte f, StatusBits flag, int condition) => ClearFlag(ref f, (byte)flag, condition);
        private static void ClearFlag(ref byte f, StatusBits flag, bool condition) => ClearFlag(ref f, (byte)flag, condition);

        private void AdjustZero(byte datum) => ClearFlag(ref p, StatusBits.ZF, datum);
        private void AdjustNegative(byte datum) => SetFlag(ref p, StatusBits.NF, datum & (byte)StatusBits.NF);

        private void AdjustNZ(byte datum)
        {
            AdjustZero(datum);
            AdjustNegative(datum);
        }

        // Miscellaneous

        private void Branch(bool condition)
        {
            var destination = Address_relative_byte();
            if (condition) {
                BusRead();
                var page = PC.High;
                Jump(destination);
                if (PC.High != page)
                    BusRead(PC.Low, page);
            }
        }

        private byte Through(int data) => Through((byte)data);

        private byte Through(byte data)
        {
            AdjustNZ(data);
			return data;
		}

        private void BusReadModifyWrite(byte data)
        {
            // The read will have already taken place...
            BusWrite();
            BusWrite(data);
        }

        //

        private byte SBC(byte operand, byte data)
        {
            var returned = SUB(operand, data, ~P & (int)StatusBits.CF);

            var difference = intermediate;
            AdjustNZ(difference.Low);
            SetFlag(ref p, StatusBits.VF, (operand ^ data) & (operand ^ difference.Low) & (int)StatusBits.NF);
        	ClearFlag(ref p, StatusBits.CF, difference.High);

        	return returned;
        }

        private byte SUB(byte operand, byte data, int borrow = 0)
        {
            return Decimal != 0 ? SUB_d(operand, data, borrow) : SUB_b(operand, data, borrow);
        }

        private byte SUB_b(byte operand, byte data, int borrow)
        {
            intermediate.Word = (ushort)(operand - data - borrow);
            return intermediate.Low;
        }

        private byte SUB_d(byte operand, byte data, int borrow)
        {
            intermediate.Word = (ushort)(operand - data - borrow);

            byte low = (byte)(LowNibble(operand) - LowNibble(data) - borrow);
            var lowNegative = low & (byte)StatusBits.NF;
            if (lowNegative != 0)
                low -= 6;

            byte high = (byte)(HighNibble(operand) - HighNibble(data) - (lowNegative >> 7));
            var highNegative = high & (byte)StatusBits.NF;
            if (highNegative != 0)
                high -= 6;

            return (byte)(PromoteNibble(high) | LowNibble(low));
        }

        private byte ADC(byte operand, byte data)
        {
            var returned = ADD(operand, data, Carry);
            AdjustNZ(intermediate.Low);
            return returned;
        }

        private byte ADD(byte operand, byte data, int carry = 0)
        {
            return Decimal != 0 ? ADD_d(operand, data, carry) : ADD_b(operand, data, carry);
        }

        private byte ADD_b(byte operand, byte data, int carry)
        {
            intermediate.Word = (ushort)(operand + data + carry);

            SetFlag(ref p, StatusBits.VF, ~(operand ^ data) & (operand ^ intermediate.Low) & (int)StatusBits.NF);
            SetFlag(ref p, StatusBits.CF, intermediate.High & (int)StatusBits.CF);

            return intermediate.Low;
        }

        private byte ADD_d(byte operand, byte data, int carry)
        {
            intermediate.Word = (ushort)(operand + data + carry);

            byte low = (byte)(LowNibble(operand) + LowNibble(data) + carry);
            if (low > 9)
                low += 6;

            byte high = (byte)(HighNibble(operand) + HighNibble(data) + (low > 0xf ? 1 : 0));
            SetFlag(ref p, StatusBits.VF, ~(operand ^ data) & (operand ^ PromoteNibble(high)) & (int)StatusBits.NF);

            if (high > 9)
                high += 6;

            SetFlag(ref p, StatusBits.CF, high > 0xf);

            return (byte)(PromoteNibble(high) | LowNibble(low));
        }

        private byte AndR(byte operand, byte data) => Through(operand & data);

        private byte ASL(byte value)
        {
            SetFlag(ref p, StatusBits.CF, value & (byte)Bits.Bit7);
            return Through(value << 1);
        }

        private void BIT(byte operand, byte data)
        {
            SetFlag(ref p, StatusBits.VF, data & (byte)StatusBits.VF);
            AdjustZero((byte)(operand & data));
            AdjustNegative(data);
        }

        private void CMP(byte first, byte second)
        {
            intermediate.Word = (ushort)(first - second);
            AdjustNZ(intermediate.Low);
            ClearFlag(ref p, StatusBits.CF, intermediate.High);
        }

        private byte DEC(byte value) => Through(value - 1);

        byte EorR(byte operand, byte data) => Through(operand ^ data);

        private byte INC(byte value) => Through(value + 1);

        private void JSR()
        {
            var low = FetchByte();
            GetBytePaged(1, S); // dummy read
            PushWord(PC);
            PC.High = FetchByte();
            PC.Low = low;
        }

        private byte LSR(byte value)
        {
            SetFlag(ref p, StatusBits.CF, value & (byte)Bits.Bit0);
            return Through(value >> 1);
        }

        private byte OrR(byte operand, byte data) => Through(operand | data);

        private void PHP() => Push((byte)(P | (byte)StatusBits.BF));

        private void PLP() => P = (byte)((Pop() | (byte)StatusBits.RF) & (byte)~StatusBits.BF);

        private byte ROL(byte operand)
        {
            var carryIn = Carry;
            SetFlag(ref p, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (operand << 1) | carryIn;
            return Through(result);
        }

        private byte ROR(byte operand)
        {
            var carryIn = Carry;
            SetFlag(ref p, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (operand >> 1) | (carryIn << 7);
            return Through(result);
        }

        private void RTI()
        {
            GetBytePaged(1, S); // dummy read
            PLP();
            Return();
        }

        private void RTS()
        {
            GetBytePaged(1, S); // dummy read
            Return();
            FetchByte();
        }

        // Undocumented compound instructions

        private void ANC(byte value)
        {
            A = AndR(A, value);
            SetFlag(ref p, StatusBits.CF, A & (byte)Bits.Bit7);
        }

        private void ARR(byte value)
        {
            A = AndR(A, value);
            A = ROR(A);
            SetFlag(ref p, StatusBits.CF, A & (byte)Bits.Bit6);
            SetFlag(ref p, StatusBits.VF, ((A & (byte)Bits.Bit6) >> 6) ^ ((A & (byte)Bits.Bit5) >> 5));
        }

        private void ASR(byte value)
        {
            A = AndR(A, value);
            A = LSR(A);
        }

        private void AXS(byte value)
        {
            X = Through(SUB((byte)(A & X), value));
            ClearFlag(ref p, StatusBits.CF, intermediate.High);
        }

        private void DCP(byte value)
        {
            BusReadModifyWrite(DEC(value));
            CMP(A, Bus.Data);
        }

        private void ISB(byte value)
        {
            BusReadModifyWrite(INC(value));
            A = SBC(A, Bus.Data);
        }

        private void RLA(byte value)
        {
            BusReadModifyWrite(ROL(value));
            A = AndR(A, Bus.Data);
        }

        private void RRA(byte value)
        {
            BusReadModifyWrite(ROR(value));
            A = ADC(A, Bus.Data);
        }

        private void SLO(byte value)
        {
            BusReadModifyWrite(ASL(value));
            A = OrR(A, Bus.Data);
        }

        private void SRE(byte value)
        {
            BusReadModifyWrite(LSR(value));
            A = EorR(A, Bus.Data);
        }

        //

        private void STA_AbsoluteX()
        {
            var crossed = Address_AbsoluteX();
            var address = crossed.Item1;
            var page = crossed.Item2;
            GetBytePaged(page, address.Low);
            BusWrite(address, A);
        }

        private void STA_AbsoluteY()
        {
            var crossed = Address_AbsoluteY();
            var address = crossed.Item1;
            var page = crossed.Item2;
            GetBytePaged(page, address.Low);
            BusWrite(address, A);
        }
    }
}
