// <copyright file="M6502.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class M6502(Bus bus) : LittleEndianProcessor(bus)
    {
        private const byte IRQvector = 0xfe;  // IRQ vector
        private const byte RSTvector = 0xfc;  // RST vector
        private const byte NMIvector = 0xfa;  // NMI vector

        private readonly Register16 intermediate = new();
        private byte crossedPage;

        private bool handlingRESET = false;
        private bool handlingNMI = false;
        private bool handlingINT = false;

        private PinLevel nmiLine = PinLevel.Low;
        private PinLevel soLine = PinLevel.Low;
        private PinLevel syncLine = PinLevel.Low;
        private PinLevel rdyLine = PinLevel.Low;
        private PinLevel rwLine = PinLevel.Low;

        public event EventHandler<EventArgs>? ExecutingInstruction;

        public event EventHandler<EventArgs>? ExecutedInstruction;

        public event EventHandler<EventArgs>? RaisingNMI;

        public event EventHandler<EventArgs>? RaisedNMI;

        public event EventHandler<EventArgs>? LoweringNMI;

        public event EventHandler<EventArgs>? LoweredNMI;

        public event EventHandler<EventArgs>? RaisingSO;

        public event EventHandler<EventArgs>? RaisedSO;

        public event EventHandler<EventArgs>? LoweringSO;

        public event EventHandler<EventArgs>? LoweredSO;

        public event EventHandler<EventArgs>? RaisingSYNC;

        public event EventHandler<EventArgs>? RaisedSYNC;

        public event EventHandler<EventArgs>? LoweringSYNC;

        public event EventHandler<EventArgs>? LoweredSYNC;

        public event EventHandler<EventArgs>? RaisingRDY;

        public event EventHandler<EventArgs>? RaisedRDY;

        public event EventHandler<EventArgs>? LoweringRDY;

        public event EventHandler<EventArgs>? LoweredRDY;

        public event EventHandler<EventArgs>? RaisingRW;

        public event EventHandler<EventArgs>? RaisedRW;

        public event EventHandler<EventArgs>? LoweringRW;

        public event EventHandler<EventArgs>? LoweredRW;

        public ref PinLevel NMI => ref this.nmiLine;

        public ref PinLevel SO => ref this.soLine;

        public ref PinLevel SYNC => ref this.syncLine;

        public ref PinLevel RDY => ref this.rdyLine;

        public ref PinLevel RW => ref this.rwLine;

        public byte X { get; set; } = 0;

        public byte Y { get; set; } = 0;

        public byte A { get; set; } = 0;

        public byte S { get; set; } = 0;

        public byte P { get; set; } = 0;

        private int InterruptMasked => this.P & (byte)StatusBits.IF;

        private int Decimal => this.P & (byte)StatusBits.DF;

        private int Negative => this.P & (byte)StatusBits.NF;

        private int Zero => this.P & (byte)StatusBits.ZF;

        private int Overflow => this.P & (byte)StatusBits.VF;

        private int Carry => this.P & (byte)StatusBits.CF;

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

        public override int Execute()
        {
            this.RaiseSYNC();    // Instruction fetch has now completed

            switch (this.OpCode)
            {
                case 0x00: this.FetchByte(); this.Interrupt(IRQvector, InterruptSource.software); break;                        // BRK (implied)
                case 0x01: this.A = this.OrR(this.A, this.AM_IndexedIndirectX()); break;                                        // ORA (indexed indirect X)
                case 0x02: break;
                case 0x03: this.SLO(this.AM_IndexedIndirectX()); break;                                                         // *SLO (indexed indirect X)
                case 0x04: this.AM_ZeroPage(); break;                                                                           // *NOP (zero page)
                case 0x05: this.A = this.OrR(this.A, this.AM_ZeroPage()); break;                                                // ORA (zero page)
                case 0x06: this.ReadModifyWrite(this.ASL(this.AM_ZeroPage())); break;                                           // ASL (zero page)
                case 0x07: this.SLO(this.AM_ZeroPage()); break;                                                                 // *SLO (zero page)
                case 0x08: this.MemoryRead(); this.PHP(); break;                                                                // PHP (implied)
                case 0x09: this.A = this.OrR(this.A, this.AM_Immediate()); break;                                               // ORA (immediate)
                case 0x0a: this.MemoryRead(); this.A = this.ASL(this.A); break;                                                 // ASL A (implied)
                case 0x0b: this.ANC(this.AM_Immediate()); break;                                                                // *ANC (immediate)
                case 0x0c: this.AM_Absolute(); break;                                                                           // *NOP (absolute)
                case 0x0d: this.A = this.OrR(this.A, this.AM_Absolute()); break;                                                // ORA (absolute)
                case 0x0e: this.ReadModifyWrite(this.ASL(this.AM_Absolute())); break;                                           // ASL (absolute)
                case 0x0f: this.SLO(this.AM_Absolute()); break;                                                                 // *SLO (absolute)

                case 0x10: this.Branch(this.Negative == 0); break;                                                              // BPL (relative)
                case 0x11: this.A = this.OrR(this.A, this.AM_IndirectIndexedY()); break;                                        // ORA (indirect indexed Y)
                case 0x12: break;
                case 0x13: this.SLO(this.AM_IndirectIndexedY()); break;                                                         // *SLO (indirect indexed Y)
                case 0x14: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0x15: this.A = this.OrR(this.A, this.AM_ZeroPageX()); break;                                               // ORA (zero page, X)
                case 0x16: this.ReadModifyWrite(this.ASL(this.AM_ZeroPageX())); break;                                          // ASL (zero page, X)
                case 0x17: this.SLO(this.AM_ZeroPageX()); break;                                                                // *SLO (zero page, X)
                case 0x18: this.MemoryRead(); this.P = ClearBit(this.P, StatusBits.CF); break;                                  // CLC (implied)
                case 0x19: this.A = this.OrR(this.A, this.AM_AbsoluteY()); break;                                               // ORA (absolute, Y)
                case 0x1a: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0x1b: this.SLO(this.AM_AbsoluteY()); break;                                                                // *SLO (absolute, Y)
                case 0x1c: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0x1d: this.A = this.OrR(this.A, this.AM_AbsoluteX()); break;                                               // ORA (absolute, X)
                case 0x1e: this.ReadModifyWrite(this.ASL(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // ASL (absolute, X)
                case 0x1f: this.SLO(this.AM_AbsoluteX()); break;                                                                // *SLO (absolute, X)

                case 0x20: this.JSR(); break;                                                                                   // JSR (absolute)
                case 0x21: this.A = this.AndR(this.A, this.AM_IndexedIndirectX()); break;                                       // AND (indexed indirect X)
                case 0x22: break;
                case 0x23: this.RLA(this.AM_IndexedIndirectX()); break;                                                         // *RLA (indexed indirect X)
                case 0x24: this.BIT(this.A, this.AM_ZeroPage()); break;                                                         // BIT (zero page)
                case 0x25: this.A = this.AndR(this.A, this.AM_ZeroPage()); break;                                               // AND (zero page)
                case 0x26: this.ReadModifyWrite(this.ROL(this.AM_ZeroPage())); break;                                           // ROL (zero page)
                case 0x27: this.RLA(this.AM_ZeroPage()); break;                                                                 // *RLA (zero page)
                case 0x28: this.MemoryRead(); this.MemoryRead(this.S, 1); this.PLP(); break;                                    // PLP (implied)
                case 0x29: this.A = this.AndR(this.A, this.AM_Immediate()); break;                                              // AND (immediate)
                case 0x2a: this.MemoryRead(); this.A = this.ROL(this.A); break;                                                 // ROL A (implied)
                case 0x2b: this.ANC(this.AM_Immediate()); break;                                                                // *ANC (immediate)
                case 0x2c: this.BIT(this.A, this.AM_Absolute()); break;                                                         // BIT (absolute)
                case 0x2d: this.A = this.AndR(this.A, this.AM_Absolute()); break;                                               // AND (absolute)
                case 0x2e: this.ReadModifyWrite(this.ROL(this.AM_Absolute())); break;                                           // ROL (absolute)
                case 0x2f: this.RLA(this.AM_Absolute()); break;                                                                 // *RLA (absolute)

                case 0x30: this.Branch(this.Negative); break;                                                                   // BMI (relative)
                case 0x31: this.A = this.AndR(this.A, this.AM_IndirectIndexedY()); break;                                       // AND (indirect indexed Y)
                case 0x32: break;
                case 0x33: this.RLA(this.AM_IndirectIndexedY()); break;                                                         // *RLA (indirect indexed Y)
                case 0x34: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0x35: this.A = this.AndR(this.A, this.AM_ZeroPageX()); break;                                              // AND (zero page, X)
                case 0x36: this.ReadModifyWrite(this.ROL(this.AM_ZeroPageX())); break;                                          // ROL (zero page, X)
                case 0x37: this.RLA(this.AM_ZeroPageX()); break;                                                                // *RLA (zero page, X)
                case 0x38: this.MemoryRead(); this.P = SetBit(this.P, StatusBits.CF); break;                                    // SEC (implied)
                case 0x39: this.A = this.AndR(this.A, this.AM_AbsoluteY()); break;                                              // AND (absolute, Y)
                case 0x3a: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0x3b: this.RLA(this.AM_AbsoluteY()); break;                                                                // *RLA (absolute, Y)
                case 0x3c: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0x3d: this.A = this.AndR(this.A, this.AM_AbsoluteX()); break;                                              // AND (absolute, X)
                case 0x3e: this.ReadModifyWrite(this.ROL(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // ROL (absolute, X)
                case 0x3f: this.RLA(this.AM_AbsoluteX()); break;                                                                // *RLA (absolute, X)

                case 0x40: this.MemoryRead(); this.RTI(); break;                                                                // RTI (implied)
                case 0x41: this.A = this.EorR(this.A, this.AM_IndexedIndirectX()); break;                                       // EOR (indexed indirect X)
                case 0x42: break;
                case 0x43: this.SRE(this.AM_IndexedIndirectX()); break;                                                         // *SRE (indexed indirect X)
                case 0x44: this.AM_ZeroPage(); break;                                                                           // *NOP (zero page)
                case 0x45: this.A = this.EorR(this.A, this.AM_ZeroPage()); break;                                               // EOR (zero page)
                case 0x46: this.ReadModifyWrite(this.LSR(this.AM_ZeroPage())); break;                                           // LSR (zero page)
                case 0x47: this.SRE(this.AM_ZeroPage()); break;                                                                 // *SRE (zero page)
                case 0x48: this.MemoryRead(); this.Push(this.A); break;                                                         // PHA (implied)
                case 0x49: this.A = this.EorR(this.A, this.AM_Immediate()); break;                                              // EOR (immediate)
                case 0x4a: this.MemoryRead(); this.A = this.LSR(this.A); break;                                                 // LSR A (implied)
                case 0x4b: this.ASR(this.AM_Immediate()); break;                                                                // *ASR (immediate)
                case 0x4c: this.Jump(this.Address_Absolute().Word); break;                                                      // JMP (absolute)
                case 0x4d: this.A = this.EorR(this.A, this.AM_Absolute()); break;                                               // EOR (absolute)
                case 0x4e: this.ReadModifyWrite(this.LSR(this.AM_Absolute())); break;                                           // LSR (absolute)
                case 0x4f: this.SRE(this.AM_Absolute()); break;                                                                 // *SRE (absolute)

                case 0x50: this.Branch(this.Overflow == 0); break;                                                              // BVC (relative)
                case 0x51: this.A = this.EorR(this.A, this.AM_IndirectIndexedY()); break;                                       // EOR (indirect indexed Y)
                case 0x52: break;
                case 0x53: this.SRE(this.AM_IndirectIndexedY()); break;                                                         // *SRE (indirect indexed Y)
                case 0x54: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0x55: this.A = this.EorR(this.A, this.AM_ZeroPageX()); break;                                              // EOR (zero page, X)
                case 0x56: this.ReadModifyWrite(this.LSR(this.AM_ZeroPageX())); break;                                          // LSR (zero page, X)
                case 0x57: this.SRE(this.AM_ZeroPageX()); break;                                                                // *SRE (zero page, X)
                case 0x58: this.MemoryRead(); this.P = ClearBit(this.P, StatusBits.IF); break;                                  // CLI (implied)
                case 0x59: this.A = this.EorR(this.A, this.AM_AbsoluteY()); break;                                              // EOR (absolute, Y)
                case 0x5a: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0x5b: this.SRE(this.AM_AbsoluteY()); break;                                                                // *SRE (absolute, Y)
                case 0x5c: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0x5d: this.A = this.EorR(this.A, this.AM_AbsoluteX()); break;                                              // EOR (absolute, X)
                case 0x5e: this.ReadModifyWrite(this.LSR(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // LSR (absolute, X)
                case 0x5f: this.SRE(this.AM_AbsoluteX()); break;                                                                // *SRE (absolute, X)

                case 0x60: this.MemoryRead(); this.RTS(); break;                                                                // RTS (implied)
                case 0x61: this.A = this.ADC(this.A, this.AM_IndexedIndirectX()); break;                                        // ADC (indexed indirect X)
                case 0x62: break;
                case 0x63: this.RRA(this.AM_IndexedIndirectX()); break;                                                         // *RRA (indexed indirect X)
                case 0x64: this.AM_ZeroPage(); break;                                                                           // *NOP (zero page)
                case 0x65: this.A = this.ADC(this.A, this.AM_ZeroPage()); break;                                                // ADC (zero page)
                case 0x66: this.ReadModifyWrite(this.ROR(this.AM_ZeroPage())); break;                                           // ROR (zero page)
                case 0x67: this.RRA(this.AM_ZeroPage()); break;                                                                 // *RRA (zero page)
                case 0x68: this.MemoryRead(); this.MemoryRead(this.S, 1); this.A = this.Through(this.Pop()); break;             // PLA (implied)
                case 0x69: this.A = this.ADC(this.A, this.AM_Immediate()); break;                                               // ADC (immediate)
                case 0x6a: this.MemoryRead(); this.A = this.ROR(this.A); break;                                                 // ROR A (implied)
                case 0x6b: this.ARR(this.AM_Immediate()); break;                                                                // *ARR (immediate)
                case 0x6c: this.Jump(this.Address_Indirect().Word); break;                                                      // JMP (indirect)
                case 0x6d: this.A = this.ADC(this.A, this.AM_Absolute()); break;                                                // ADC (absolute)
                case 0x6e: this.ReadModifyWrite(this.ROR(this.AM_Absolute())); break;                                           // ROR (absolute)
                case 0x6f: this.RRA(this.AM_Absolute()); break;                                                                 // *RRA (absolute)

                case 0x70: this.Branch(this.Overflow); break;                                                                   // BVS (relative)
                case 0x71: this.A = this.ADC(this.A, this.AM_IndirectIndexedY()); break;                                        // ADC (indirect indexed Y)
                case 0x72: break;
                case 0x73: this.RRA(this.AM_IndirectIndexedY()); break;                                                         // *RRA (indirect indexed Y)
                case 0x74: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0x75: this.A = this.ADC(this.A, this.AM_ZeroPageX()); break;                                               // ADC (zero page, X)
                case 0x76: this.ReadModifyWrite(this.ROR(this.AM_ZeroPageX())); break;                                          // ROR (zero page, X)
                case 0x77: this.RRA(this.AM_ZeroPageX()); break;                                                                // *RRA (zero page, X)
                case 0x78: this.MemoryRead(); this.P = SetBit(this.P, StatusBits.IF); break;                                    // SEI (implied)
                case 0x79: this.A = this.ADC(this.A, this.AM_AbsoluteY()); break;                                               // ADC (absolute, Y)
                case 0x7a: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0x7b: this.RRA(this.AM_AbsoluteY()); break;                                                                // *RRA (absolute, Y)
                case 0x7c: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0x7d: this.A = this.ADC(this.A, this.AM_AbsoluteX()); break;                                               // ADC (absolute, X)
                case 0x7e: this.ReadModifyWrite(this.ROR(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // ROR (absolute, X)
                case 0x7f: this.RRA(this.AM_AbsoluteX()); break;                                                                // *RRA (absolute, X)

                case 0x80: this.AM_Immediate(); break;                                                                          // *NOP (immediate)
                case 0x81: this.MemoryWrite(this.Address_IndexedIndirectX(), this.A); break;                                    // STA (indexed indirect X)
                case 0x82: this.AM_Immediate(); break;                                                                          // *NOP (immediate)
                case 0x83: this.MemoryWrite(this.Address_IndexedIndirectX(), (byte)(this.A & this.X)); break;                   // *SAX (indexed indirect X)
                case 0x84: this.MemoryWrite(this.Address_ZeroPage(), this.Y); break;                                            // STY (zero page)
                case 0x85: this.MemoryWrite(this.Address_ZeroPage(), this.A); break;                                            // STA (zero page)
                case 0x86: this.MemoryWrite(this.Address_ZeroPage(), this.X); break;                                            // STX (zero page)
                case 0x87: this.MemoryWrite(this.Address_ZeroPage(), (byte)(this.A & this.X)); break;                           // *SAX (zero page)
                case 0x88: this.MemoryRead(); this.Y = this.DEC(this.Y); break;                                                 // DEY (implied)
                case 0x89: this.AM_Immediate(); break;                                                                          // *NOP (immediate)
                case 0x8a: this.MemoryRead(); this.A = this.Through(this.X); break;                                             // TXA (implied)
                case 0x8b: break;
                case 0x8c: this.MemoryWrite(this.Address_Absolute(), this.Y); break;                                            // STY (absolute)
                case 0x8d: this.MemoryWrite(this.Address_Absolute(), this.A); break;                                            // STA (absolute)
                case 0x8e: this.MemoryWrite(this.Address_Absolute(), this.X); break;                                            // STX (absolute)
                case 0x8f: this.MemoryWrite(this.Address_Absolute(), (byte)(this.A & this.X)); break;                           // *SAX (absolute)

                case 0x90: this.Branch(this.Carry == 0); break;                                                                 // BCC (relative)
                case 0x91: this.AM_IndirectIndexedY(); this.MemoryWrite(this.A); break;                                         // STA (indirect indexed Y)
                case 0x92: break;
                case 0x93: break;
                case 0x94: this.MemoryWrite(this.Address_ZeroPageX(), this.Y); break;                                           // STY (zero page, X)
                case 0x95: this.MemoryWrite(this.Address_ZeroPageX(), this.A); break;                                           // STA (zero page, X)
                case 0x96: this.MemoryWrite(this.Address_ZeroPageY(), this.X); break;                                           // STX (zero page, Y)
                case 0x97: this.MemoryWrite(this.Address_ZeroPageY(), (byte)(this.A & this.X)); break;                          // *SAX (zero page, Y)
                case 0x98: this.MemoryRead(); this.A = this.Through(this.Y); break;                                             // TYA (implied)
                case 0x99: this.STA_AbsoluteY(); break;                                                                         // STA (absolute, Y)
                case 0x9a: this.MemoryRead(); this.S = this.X; break;                                                           // TXS (implied)
                case 0x9b: break;
                case 0x9c: break;
                case 0x9d: this.STA_AbsoluteX(); break;                                                                         // STA (absolute, X)
                case 0x9e: break;
                case 0x9f: break;

                case 0xa0: this.Y = this.Through(this.AM_Immediate()); break;                                                   // LDY (immediate)
                case 0xa1: this.A = this.Through(this.AM_IndexedIndirectX()); break;                                            // LDA (indexed indirect X)
                case 0xa2: this.X = this.Through(this.AM_Immediate()); break;                                                   // LDX (immediate)
                case 0xa3: this.A = this.X = this.Through(this.AM_IndexedIndirectX()); break;                                   // *LAX (indexed indirect X)
                case 0xa4: this.Y = this.Through(this.AM_ZeroPage()); break;                                                    // LDY (zero page)
                case 0xa5: this.A = this.Through(this.AM_ZeroPage()); break;                                                    // LDA (zero page)
                case 0xa6: this.X = this.Through(this.AM_ZeroPage()); break;                                                    // LDX (zero page)
                case 0xa7: this.A = this.X = this.Through(this.AM_ZeroPage()); break;                                           // *LAX (zero page)
                case 0xa8: this.MemoryRead(); this.Y = this.Through(this.A); break;                                             // TAY (implied)
                case 0xa9: this.A = this.Through(this.AM_Immediate()); break;                                                   // LDA (immediate)
                case 0xaa: this.MemoryRead(); this.X = this.Through(this.A); break;                                             // TAX (implied)
                case 0xab: this.A = this.X = this.Through(this.AM_Immediate()); break;                                          // *ATX (immediate)
                case 0xac: this.Y = this.Through(this.AM_Absolute()); break;                                                    // LDY (absolute)
                case 0xad: this.A = this.Through(this.AM_Absolute()); break;                                                    // LDA (absolute)
                case 0xae: this.X = this.Through(this.AM_Absolute()); break;                                                    // LDX (absolute)
                case 0xaf: this.A = this.X = this.Through(this.AM_Absolute()); break;                                           // *LAX (absolute)

                case 0xb0: this.Branch(this.Carry); break;                                                                      // BCS (relative)
                case 0xb1: this.A = this.Through(this.AM_IndirectIndexedY()); break;                                            // LDA (indirect indexed Y)
                case 0xb2: break;
                case 0xb3: this.A = this.X = this.Through(this.AM_IndirectIndexedY()); break;                                   // *LAX (indirect indexed Y)
                case 0xb4: this.Y = this.Through(this.AM_ZeroPageX()); break;                                                   // LDY (zero page, X)
                case 0xb5: this.A = this.Through(this.AM_ZeroPageX()); break;                                                   // LDA (zero page, X)
                case 0xb6: this.X = this.Through(this.AM_ZeroPageY()); break;                                                   // LDX (zero page, Y)
                case 0xb7: this.A = this.X = this.Through(this.AM_ZeroPageY()); break;                                          // *LAX (zero page, Y)
                case 0xb8: this.MemoryRead(); this.P = ClearBit(this.P, StatusBits.VF); break;                                  // CLV (implied)
                case 0xb9: this.A = this.Through(this.AM_AbsoluteY()); break;                                                   // LDA (absolute, Y)
                case 0xba: this.MemoryRead(); this.X = this.Through(this.S); break;                                             // TSX (implied)
                case 0xbb: break;
                case 0xbc: this.Y = this.Through(this.AM_AbsoluteX()); break;                                                   // LDY (absolute, X)
                case 0xbd: this.A = this.Through(this.AM_AbsoluteX()); break;                                                   // LDA (absolute, X)
                case 0xbe: this.X = this.Through(this.AM_AbsoluteY()); break;                                                   // LDX (absolute, Y)
                case 0xbf: this.A = this.X = this.Through(this.AM_AbsoluteY()); break;                                          // *LAX (absolute, Y)

                case 0xc0: this.CMP(this.Y, this.AM_Immediate()); break;                                                        // CPY (immediate)
                case 0xc1: this.CMP(this.A, this.AM_IndexedIndirectX()); break;                                                 // CMP (indexed indirect X)
                case 0xc2: this.AM_Immediate(); break;                                                                          // *NOP (immediate)
                case 0xc3: this.DCP(this.AM_IndexedIndirectX()); break;                                                         // *DCP (indexed indirect X)
                case 0xc4: this.CMP(this.Y, this.AM_ZeroPage()); break;                                                         // CPY (zero page)
                case 0xc5: this.CMP(this.A, this.AM_ZeroPage()); break;                                                         // CMP (zero page)
                case 0xc6: this.ReadModifyWrite(this.DEC(this.AM_ZeroPage())); break;                                           // DEC (zero page)
                case 0xc7: this.DCP(this.AM_ZeroPage()); break;                                                                 // *DCP (zero page)
                case 0xc8: this.MemoryRead(); this.Y = this.INC(this.Y); break;                                                 // INY (implied)
                case 0xc9: this.CMP(this.A, this.AM_Immediate()); break;                                                        // CMP (immediate)
                case 0xca: this.MemoryRead(); this.X = this.DEC(this.X); break;                                                 // DEX (implied)
                case 0xcb: this.AXS(this.AM_Immediate()); break;                                                                // *AXS (immediate)
                case 0xcc: this.CMP(this.Y, this.AM_Absolute()); break;                                                         // CPY (absolute)
                case 0xcd: this.CMP(this.A, this.AM_Absolute()); break;                                                         // CMP (absolute)
                case 0xce: this.ReadModifyWrite(this.DEC(this.AM_Absolute())); break;                                           // DEC (absolute)
                case 0xcf: this.DCP(this.AM_Absolute()); break;                                                                 // *DCP (absolute)

                case 0xd0: this.Branch(this.Zero == 0); break;                                                                  // BNE (relative)
                case 0xd1: this.CMP(this.A, this.AM_IndirectIndexedY()); break;                                                 // CMP (indirect indexed Y)
                case 0xd2: break;
                case 0xd3: this.DCP(this.AM_IndirectIndexedY()); break;                                                         // *DCP (indirect indexed Y)
                case 0xd4: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0xd5: this.CMP(this.A, this.AM_ZeroPageX()); break;                                                        // CMP (zero page, X)
                case 0xd6: this.ReadModifyWrite(this.DEC(this.AM_ZeroPageX())); break;                                          // DEC (zero page, X)
                case 0xd7: this.DCP(this.AM_ZeroPageX()); break;                                                                // *DCP (zero page, X)
                case 0xd8: this.MemoryRead(); this.P = ClearBit(this.P, StatusBits.DF); break;                                  // CLD (implied)
                case 0xd9: this.CMP(this.A, this.AM_AbsoluteY()); break;                                                        // CMP (absolute, Y)
                case 0xda: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0xdb: this.DCP(this.AM_AbsoluteY()); break;                                                                // *DCP (absolute, Y)
                case 0xdc: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0xdd: this.CMP(this.A, this.AM_AbsoluteX()); break;                                                        // CMP (absolute, X)
                case 0xde: this.ReadModifyWrite(this.DEC(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // DEC (absolute, X)
                case 0xdf: this.DCP(this.AM_AbsoluteX()); break;                                                                // *DCP (absolute, X)

                case 0xe0: this.CMP(this.X, this.AM_Immediate()); break;                                                        // CPX (immediate)
                case 0xe1: this.A = this.SBC(this.A, this.AM_IndexedIndirectX()); break;                                        // SBC (indexed indirect X)
                case 0xe2: this.AM_Immediate(); break;                                                                          // *NOP (immediate)
                case 0xe3: this.ISB(this.AM_IndexedIndirectX()); break;                                                         // *ISB (indexed indirect X)
                case 0xe4: this.CMP(this.X, this.AM_ZeroPage()); break;                                                         // CPX (zero page)
                case 0xe5: this.A = this.SBC(this.A, this.AM_ZeroPage()); break;                                                // SBC (zero page)
                case 0xe6: this.ReadModifyWrite(this.INC(this.AM_ZeroPage())); break;                                           // INC (zero page)
                case 0xe7: this.ISB(this.AM_ZeroPage()); break;                                                                 // *ISB (zero page)
                case 0xe8: this.MemoryRead(); this.X = this.INC(this.X); break;                                                 // INX (implied)
                case 0xe9: this.A = this.SBC(this.A, this.AM_Immediate()); break;                                               // SBC (immediate)
                case 0xea: this.MemoryRead(); break;                                                                            // NOP (implied)
                case 0xeb: this.A = this.SBC(this.A, this.AM_Immediate()); break;                                               // *SBC (immediate)
                case 0xec: this.CMP(this.X, this.AM_Absolute()); break;                                                         // CPX (absolute)
                case 0xed: this.A = this.SBC(this.A, this.AM_Absolute()); break;                                                // SBC (absolute)
                case 0xee: this.ReadModifyWrite(this.INC(this.AM_Absolute())); break;                                           // *ISB (absolute)

                case 0xf0: this.Branch(this.Zero); break;                                                                       // BEQ (relative)
                case 0xf1: this.A = this.SBC(this.A, this.AM_IndirectIndexedY()); break;                                        // SBC (indirect indexed Y)
                case 0xf2: break;
                case 0xf3: this.ISB(this.AM_IndirectIndexedY()); break;                                                         // *ISB (indirect indexed Y)
                case 0xf4: this.AM_ZeroPageX(); break;                                                                          // *NOP (zero page, X)
                case 0xf5: this.A = this.SBC(this.A, this.AM_ZeroPageX()); break;                                               // SBC (zero page, X)
                case 0xf6: this.ReadModifyWrite(this.INC(this.AM_ZeroPageX())); break;                                          // INC (zero page, X)
                case 0xf7: this.ISB(this.AM_ZeroPageX()); break;                                                                // *ISB (zero page, X)
                case 0xf8: this.MemoryRead(); this.P = SetBit(this.P, StatusBits.DF); break;                                    // SED (implied)
                case 0xf9: this.A = this.SBC(this.A, this.AM_AbsoluteY()); break;                                               // SBC (absolute, Y)
                case 0xfa: this.MemoryRead(); break;                                                                            // *NOP (implied)
                case 0xfb: this.ISB(this.AM_AbsoluteY()); break;                                                                // *ISB (absolute, Y)
                case 0xfc: this.AM_AbsoluteX(); break;                                                                          // *NOP (absolute, X)
                case 0xfd: this.A = this.SBC(this.A, this.AM_AbsoluteX()); break;                                               // SBC (absolute, X)
                case 0xfe: this.ReadModifyWrite(this.INC(this.AM_AbsoluteX(PageCrossingBehavior.AlwaysReadTwice))); break;      // INC (absolute, X)
                case 0xff: this.ISB(this.AM_AbsoluteX()); break;                                                                // *ISB (absolute, X)
            }

            return this.Cycles;
        }

        public override int Step()
        {
            this.ResetCycles();
            this.OnExecutingInstruction();
            if (this.Powered)
            {
                this.Tick();
                if (this.SO.Lowered())
                {
                    this.HandleSO();
                }

                if (this.RDY.Raised())
                {
                    this.LowerSYNC();    // Instruction fetch beginning
                    this.RaiseRW();
                    this.OpCode = this.Bus.Read(this.PC.Word++);  // can't use fetchByte
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

                    this.Execute();
                }
            }

            this.OnExecutedInstruction();
            return this.Cycles;
        }

        protected virtual void OnExecutingInstruction() => this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnExecutedInstruction() => this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingNMI() => this.RaisingNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedNMI() => this.RaisedNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringNMI() => this.LoweringNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredNMI() => this.LoweredNMI?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingSO() => this.RaisingSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedSO() => this.RaisedSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringSO() => this.LoweringSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredSO() => this.LoweredSO?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingSYNC() => this.RaisingSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedSYNC() => this.RaisedSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringSYNC() => this.LoweringSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredSYNC() => this.LoweredSYNC?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRDY() => this.RaisingRDY?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRDY() => this.RaisedRDY?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRDY() => this.LoweringRDY?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRDY() => this.LoweredRDY?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingRW() => this.RaisingRW?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRW() => this.RaisedRW?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRW() => this.LoweringRW?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRW() => this.LoweredRW?.Invoke(this, EventArgs.Empty);

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

        protected override sealed void BusWrite()
        {
            this.Tick();
            this.LowerRW();
            base.BusWrite();
        }

        protected override sealed byte BusRead()
        {
            this.Tick();
            this.RaiseRW();
            return base.BusRead();
        }

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private void SetFlag(StatusBits flag)
        {
            this.P = SetBit(this.P, flag);
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.Interrupt(NMIvector);
        }

        private void HandleSO()
        {
            this.RaiseSO();
            this.P |= (byte)StatusBits.VF;
        }

        enum InterruptSource { hardware, software };

        enum InterruptType { reset, non_reset };

        private void Interrupt(byte vector, InterruptSource source = InterruptSource.hardware, InterruptType type = InterruptType.non_reset)
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
                this.Push((byte)(this.P | (int)(source == InterruptSource.hardware ? 0 : StatusBits.BF)));
            }
            this.SetFlag(StatusBits.IF);   // Disable IRQ
            this.Jump(this.GetWordPaged(0xff, vector).Word);
        }

        private void UpdateStack(byte position)
        {
            this.Bus.Address.Word = new Register16(position, 1).Word;
        }

        private void LowerStack()
        {
            this.UpdateStack(this.S--);
        }
		
        private void RaiseStack()
        {
            this.UpdateStack(++this.S);
        }

        private void DummyPush()
        {
            this.LowerStack();
            this.Tick();
        }

        private Register16 Address_Absolute() => this.FetchWord();

        private byte Address_ZeroPage() => this.FetchByte();

        private Register16 Address_ZeroPageIndirect() => this.GetWordPaged(0, this.Address_ZeroPage());

        private Register16 Address_Indirect()
        {
            var address = this.Address_Absolute();
            return this.GetWordPaged(address.High, address.Low);
        }

        private byte Address_ZeroPageX()
        {
            var address = this.Address_ZeroPage();
            this.MemoryRead(address);
            return Chip.LowByte(address + this.X);
        }

        private byte Address_ZeroPageY()
        {
            var address = this.Address_ZeroPage();
            this.MemoryRead(address);
            return Chip.LowByte(address + this.Y);
        }

        private Register16 Address_AbsoluteX()
        {
            var address = this.Address_Absolute();
            this.crossedPage = address.High;
            address.Word += this.X;
            return address;
        }

        private Register16 Address_AbsoluteY()
        {
            var address = this.Address_Absolute();
            this.crossedPage = address.High;
            address.Word += this.Y;
            return address;
        }

        private Register16 Address_IndexedIndirectX() => this.GetWordPaged(0, this.Address_ZeroPageX());

        private Register16 Address_IndirectIndexedY()
        {
            var address = this.Address_ZeroPageIndirect();
            this.crossedPage = address.High;
            address.Word += this.Y;
            return address;
        }

        private ushort Address_relative_byte()
        {
            var offset = (sbyte)this.FetchByte();
            this.intermediate.Word = (ushort)(this.PC.Word + offset);
            return this.intermediate.Word;
        }

        private byte AM_Immediate() => this.FetchByte();

        private byte AM_Absolute() => this.MemoryRead(this.Address_Absolute());

        private byte AM_ZeroPage() => this.MemoryRead(this.Address_ZeroPage());

        private byte AM_AbsoluteX(PageCrossingBehavior behaviour = PageCrossingBehavior.MaybeReadTwice)
        {
            var address = this.Address_AbsoluteX();
            var possible = this.MemoryRead(address.Low, this.crossedPage);
            if ((behaviour == PageCrossingBehavior.AlwaysReadTwice) || (this.crossedPage != address.High))
            {
                possible = this.MemoryRead(address.Word);
            }

            return possible;
        }

        private byte AM_AbsoluteY()
        {
            var address = this.Address_AbsoluteY();
            var possible = this.MemoryRead(address.Low, this.crossedPage);
            if (this.crossedPage != address.High)
            {
                possible = this.MemoryRead(address.Word);
            }

            return possible;
        }

        private byte AM_ZeroPageX() => this.MemoryRead(this.Address_ZeroPageX());

        private byte AM_ZeroPageY() => this.MemoryRead(this.Address_ZeroPageY());

        private byte AM_IndexedIndirectX() => this.MemoryRead(this.Address_IndexedIndirectX());

        private byte AM_IndirectIndexedY()
        {
            var address = this.Address_IndirectIndexedY();
            var possible = this.MemoryRead(address.Low, this.crossedPage);
            if (this.crossedPage != address.High)
            {
                possible = this.MemoryRead(address);
            }

            return possible;
        }

        private void AdjustZero(byte datum) => this.P = ClearBit(this.P, StatusBits.ZF, datum);

        private void AdjustNegative(byte datum) => this.P = SetBit(this.P, StatusBits.NF, datum & (byte)StatusBits.NF);

        private void AdjustNZ(byte datum)
        {
            this.AdjustZero(datum);
            this.AdjustNegative(datum);
        }

        private void Branch(int condition) => this.Branch(condition != 0);

        private void Branch(bool condition)
        {
            var destination = this.Address_relative_byte();
            if (condition)
            {
                this.MemoryRead();
                var page = this.PC.High;
                this.Jump(destination);
                if (this.PC.High != page)
                {
                    this.MemoryRead(this.PC.Low, page);
                }
            }
        }

        private byte Through(int data) => this.Through((byte)data);

        private byte Through(byte data)
        {
            this.AdjustNZ(data);
            return data;
        }

        private void ReadModifyWrite(byte data)
        {
            // The read will have already taken place...
            this.MemoryWrite();
            this.MemoryWrite(data);
        }

        private byte SBC(byte operand, byte data)
        {
            var returned = this.SUB(operand, data, ~this.P & (int)StatusBits.CF);

            this.AdjustNZ(this.intermediate.Low);
            this.P = SetBit(this.P, StatusBits.VF, (operand ^ data) & (operand ^ this.intermediate.Low) & (int)StatusBits.NF);
            this.P = ClearBit(this.P, StatusBits.CF, this.intermediate.High);

            return returned;
        }

        private byte SUB(byte operand, byte data, int borrow = 0) => this.Decimal != 0 ? this.SUB_d(operand, data, borrow) : this.SUB_b(operand, data, borrow);

        private byte SUB_b(byte operand, byte data, int borrow)
        {
            this.intermediate.Word = (ushort)(operand - data - borrow);
            return this.intermediate.Low;
        }

        private byte SUB_d(byte operand, byte data, int borrow)
        {
            this.intermediate.Word = (ushort)(operand - data - borrow);

            var low = (byte)(LowNibble(operand) - LowNibble(data) - borrow);
            var lowNegative = low & (byte)StatusBits.NF;
            if (lowNegative != 0)
            {
                low -= 6;
            }

            var high = (byte)(HighNibble(operand) - HighNibble(data) - (lowNegative >> 7));
            var highNegative = high & (byte)StatusBits.NF;
            if (highNegative != 0)
            {
                high -= 6;
            }

            return (byte)(PromoteNibble(high) | LowNibble(low));
        }

        private byte ADC(byte operand, byte data)
        {
            var returned = this.ADD(operand, data, this.Carry);
            this.AdjustNZ(this.intermediate.Low);
            return returned;
        }

        private byte ADD(byte operand, byte data, int carry = 0) => this.Decimal != 0 ? this.ADD_d(operand, data, carry) : this.ADD_b(operand, data, carry);

        private byte ADD_b(byte operand, byte data, int carry)
        {
            this.intermediate.Word = (ushort)(operand + data + carry);

            this.P = SetBit(this.P, StatusBits.VF, ~(operand ^ data) & (operand ^ this.intermediate.Low) & (int)StatusBits.NF);
            this.P = SetBit(this.P, StatusBits.CF, this.intermediate.High & (int)StatusBits.CF);

            return this.intermediate.Low;
        }

        private byte ADD_d(byte operand, byte data, int carry)
        {
            this.intermediate.Word = (ushort)(operand + data + carry);

            var low = (byte)(LowNibble(operand) + LowNibble(data) + carry);
            if (low > 9)
            {
                low += 6;
            }

            var high = (byte)(HighNibble(operand) + HighNibble(data) + (low > 0xf ? 1 : 0));
            this.P = SetBit(this.P, StatusBits.VF, ~(operand ^ data) & (operand ^ Chip.PromoteNibble(high)) & (int)StatusBits.NF);

            if (high > 9)
            {
                high += 6;
            }

            this.P = SetBit(this.P, StatusBits.CF, high > 0xf);

            return (byte)(PromoteNibble(high) | LowNibble(low));
        }

        private byte AndR(byte operand, byte data) => this.Through(operand & data);

        private byte ASL(byte value)
        {
            this.P = SetBit(this.P, StatusBits.CF, value & (byte)Bits.Bit7);
            return this.Through(value << 1);
        }

        private void BIT(byte operand, byte data)
        {
            this.P = SetBit(this.P, StatusBits.VF, data & (byte)StatusBits.VF);
            this.AdjustZero((byte)(operand & data));
            this.AdjustNegative(data);
        }

        private void CMP(byte first, byte second)
        {
            this.intermediate.Word = (ushort)(first - second);
            this.AdjustNZ(this.intermediate.Low);
            this.P = ClearBit(this.P, StatusBits.CF, this.intermediate.High);
        }

        private byte DEC(byte value) => this.Through(--value);

        private byte EorR(byte operand, byte data) => this.Through(operand ^ data);

        private byte INC(byte value) => this.Through(++value);

        private void JSR()
        {
            var low = this.FetchByte();
            this.MemoryRead(this.S, 1); // dummy read
            this.PushWord(this.PC);
            var high = this.FetchByte();
            this.PC.Low = low;
            this.PC.High = high;
        }

        private byte LSR(byte value)
        {
            this.P = SetBit(this.P, StatusBits.CF, value & (byte)Bits.Bit0);
            return this.Through(value >> 1);
        }

        private byte OrR(byte operand, byte data) => this.Through(operand | data);

        private void PHP() => this.Push((byte)(this.P | (byte)StatusBits.BF));

        private void PLP() => this.P = (byte)((this.Pop() | (byte)StatusBits.RF) & (byte)~StatusBits.BF);

        private byte ROL(byte operand)
        {
            var carryIn = this.Carry;
            this.P = SetBit(this.P, StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (operand << 1) | carryIn;
            return this.Through(result);
        }

        private byte ROR(byte operand)
        {
            var carryIn = this.Carry;
            this.P = SetBit(this.P, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (operand >> 1) | (carryIn << 7);
            return this.Through(result);
        }

        private void RTI()
        {
            this.MemoryRead(this.S, 1); // dummy read
            this.PLP();
            this.Return();
        }

        private void RTS()
        {
            this.MemoryRead(this.S, 1); // dummy read
            this.Return();
            this.FetchByte();
        }

        private void ANC(byte value)
        {
            this.A = this.AndR(this.A, value);
            this.P = SetBit(this.P, StatusBits.CF, this.A & (byte)Bits.Bit7);
        }

        private void ARR(byte value)
        {
            this.A = this.AndR(this.A, value);
            this.A = this.ROR(this.A);
            this.P = SetBit(this.P, StatusBits.CF, this.A & (byte)Bits.Bit6);
            this.P = SetBit(this.P, StatusBits.VF, ((this.A & (byte)Bits.Bit6) >> 6) ^ ((this.A & (byte)Bits.Bit5) >> 5));
        }

        private void ASR(byte value)
        {
            this.A = this.AndR(this.A, value);
            this.A = this.LSR(this.A);
        }

        private void AXS(byte value)
        {
            this.X = this.Through(this.SUB((byte)(this.A & this.X), value));
            this.P = ClearBit(this.P, StatusBits.CF, this.intermediate.High);
        }

        private void DCP(byte value)
        {
            this.ReadModifyWrite(this.DEC(value));
            this.CMP(this.A, this.Bus.Data);
        }

        private void ISB(byte value)
        {
            this.ReadModifyWrite(this.INC(value));
            this.A = this.SBC(this.A, this.Bus.Data);
        }

        private void RLA(byte value)
        {
            this.ReadModifyWrite(this.ROL(value));
            this.A = this.AndR(this.A, this.Bus.Data);
        }

        private void RRA(byte value)
        {
            this.ReadModifyWrite(this.ROR(value));
            this.A = this.ADC(this.A, this.Bus.Data);
        }

        private void SLO(byte value)
        {
            this.ReadModifyWrite(this.ASL(value));
            this.A = this.OrR(this.A, this.Bus.Data);
        }

        private void SRE(byte value)
        {
            this.ReadModifyWrite(this.LSR(value));
            this.A = this.EorR(this.A, this.Bus.Data);
        }

        private void STA_AbsoluteX()
        {
            var address = this.Address_AbsoluteX();
            this.MemoryRead(address.Low, this.crossedPage);
            this.MemoryWrite(address, this.A);
        }

        private void STA_AbsoluteY()
        {
            var address = this.Address_AbsoluteY();
            this.MemoryRead(address.Low, this.crossedPage);
            this.MemoryWrite(address, this.A);
        }
    }
}
