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
        private byte fixedPage;

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

        public byte X { get; set; }

        public byte Y { get; set; }

        public byte A { get; set; }

        public byte S { get; set; }

        public byte P { get; set; }

        private int InterruptMasked => this.P & (byte)StatusBits.IF;

        private int Decimal => this.P & (byte)StatusBits.DF;

        private int Negative => NegativeTest(this.P);

        private int Zero => ZeroTest(this.P);

        private int Overflow => OverflowTest(this.P);

        private int Carry => CarryTest(this.P);

        private static int NegativeTest(byte data) => data & (byte)StatusBits.NF;

        private static int ZeroTest(byte data) => data & (byte)StatusBits.ZF;

        private static int OverflowTest(byte data) => data & (byte)StatusBits.VF;

        private static int CarryTest(byte data) => data & (byte)StatusBits.CF;

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

        public override void Execute()
        {
            switch (this.OpCode)
            {
                case 0x00: this.SwallowFetch(); this.Interrupt(IRQvector, InterruptSource.software); break;             // BRK (implied)
                case 0x01: this.AM_IndexedIndirectX(); this.OrR(); break;                                               // ORA (indexed indirect X)
                case 0x02: this.Jam(); break;                                                                           // *JAM
                case 0x03: this.AM_IndexedIndirectX(); this.SLO(); break;                                               // *SLO (indexed indirect X)
                case 0x04: this.AM_ZeroPage(); break;                                                                   // *NOP (zero page)
                case 0x05: this.AM_ZeroPage(); this.OrR(); break;                                                       // ORA (zero page)
                case 0x06: this.AM_ZeroPage(); this.ModifyWrite(this.ASL()); break;                                     // ASL (zero page)
                case 0x07: this.AM_ZeroPage(); this.SLO(); break;                                                       // *SLO (zero page)
                case 0x08: this.Swallow(); this.PHP(); break;                                                           // PHP (implied)
                case 0x09: this.AM_Immediate(); this.OrR(); break;                                                      // ORA (immediate)
                case 0x0a: this.Swallow(); A = this.ASL(A); break;                                                      // ASL A (implied)
                case 0x0b: this.AM_Immediate(); this.ANC(); break;                                                      // *ANC (immediate)
                case 0x0c: this.AM_Absolute(); break;                                                                   // *NOP (absolute)
                case 0x0d: this.AM_Absolute(); this.OrR(); break;                                                       // ORA (absolute)
                case 0x0e: this.AM_Absolute(); this.ModifyWrite(this.ASL()); break;                                     // ASL (absolute)
                case 0x0f: this.AM_Absolute(); this.SLO(); break;                                                       // *SLO (absolute)

                case 0x10: this.Branch(this.Negative == 0); break;                                                      // BPL (relative)
                case 0x11: this.AM_IndirectIndexedY(); this.OrR(); break;                                               // ORA (indirect indexed Y)
                case 0x12: this.Jam(); break;                                                                           // *JAM
                case 0x13: this.Address_IndirectIndexedY(); this.FixupR(); this.SLO(); break;                           // *SLO (indirect indexed Y)
                case 0x14: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0x15: this.AM_ZeroPageX(); this.OrR(); break;                                                      // ORA (zero page, X)
                case 0x16: this.AM_ZeroPageX(); this.ModifyWrite(this.ASL()); break;                                    // ASL (zero page, X)
                case 0x17: this.AM_ZeroPageX(); this.SLO(); break;                                                      // *SLO (zero page, X)
                case 0x18: this.Swallow(); this.ResetFlag(StatusBits.CF); break;                                        // CLC (implied)
                case 0x19: this.AM_AbsoluteY(); this.OrR(); break;                                                      // ORA (absolute, Y)
                case 0x1a: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0x1b: this.Address_AbsoluteY(); this.FixupR(); this.SLO(); break;                                  // *SLO (absolute, Y)
                case 0x1c: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0x1d: this.AM_AbsoluteX(); this.OrR(); break;                                                      // ORA (absolute, X)
                case 0x1e: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.ASL()); break;                // ASL (absolute, X)
                case 0x1f: this.Address_AbsoluteX(); this.FixupR(); this.SLO(); break;                                  // *SLO (absolute, X)

                case 0x20: this.JSR(); break;                                                                           // JSR (absolute)
                case 0x21: this.AM_IndexedIndirectX(); this.AndR(); break;                                              // AND (indexed indirect X)
                case 0x22: this.Jam(); break;                                                                           // *JAM
                case 0x23: this.AM_IndexedIndirectX(); this.RLA(); ; break;                                             // *RLA (indexed indirect X)
                case 0x24: this.AM_ZeroPage(); this.BIT(); break;                                                       // BIT (zero page)
                case 0x25: this.AM_ZeroPage(); this.AndR(); break;                                                      // AND (zero page)
                case 0x26: this.AM_ZeroPage(); this.ModifyWrite(this.ROL()); break;                                     // ROL (zero page)
                case 0x27: this.AM_ZeroPage(); this.RLA(); ; break;                                                     // *RLA (zero page)
                case 0x28: this.Swallow(); this.PLP(); break;                                                           // PLP (implied)
                case 0x29: this.AM_Immediate(); this.AndR(); break;                                                     // AND (immediate)
                case 0x2a: this.Swallow(); this.A = this.ROL(this.A); break;                                            // ROL A (implied)
                case 0x2b: this.AM_Immediate(); this.ANC(); break;                                                      // *ANC (immediate)
                case 0x2c: this.AM_Absolute(); this.BIT(); break;                                                       // BIT (absolute)
                case 0x2d: this.AM_Absolute(); this.AndR(); break;                                                      // AND (absolute)
                case 0x2e: this.AM_Absolute(); this.ModifyWrite(this.ROL()); break;                                     // ROL (absolute)
                case 0x2f: this.AM_Absolute(); this.RLA(); break;                                                       // *RLA (absolute)

                case 0x30: this.Branch(this.Negative); break;                                                           // BMI (relative)
                case 0x31: this.AM_IndirectIndexedY(); this.AndR(); break;                                              // AND (indirect indexed Y)
                case 0x32: this.Jam(); break;																			// *JAM
                case 0x33: this.Address_IndirectIndexedY(); this.FixupR(); this.RLA(); break;                           // *RLA (indirect indexed Y)
                case 0x34: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0x35: this.AM_ZeroPageX(); this.AndR(); break;                                                     // AND (zero page, X)
                case 0x36: this.AM_ZeroPageX(); this.ModifyWrite(this.ROL()); break;                                    // ROL (zero page, X)
                case 0x37: this.AM_ZeroPageX(); this.RLA(); ; break;                                                    // *RLA (zero page, X)
                case 0x38: this.Swallow(); this.SetFlag(StatusBits.CF); break;                                          // SEC (implied)
                case 0x39: this.AM_AbsoluteY(); this.AndR(); break;                                                     // AND (absolute, Y)
                case 0x3a: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0x3b: this.Address_AbsoluteY(); this.FixupR(); this.RLA(); break;                                  // *RLA (absolute, Y)
                case 0x3c: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0x3d: this.AM_AbsoluteX(); this.AndR(); break;                                                     // AND (absolute, X)
                case 0x3e: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.ROL()); break;                // ROL (absolute, X)
                case 0x3f: this.Address_AbsoluteX(); this.FixupR(); this.RLA(); break;                                  // *RLA (absolute, X)

                case 0x40: this.Swallow(); this.RTI(); break;                                                           // RTI (implied)
                case 0x41: this.AM_IndexedIndirectX(); this.EorR(); break;                                              // EOR (indexed indirect X)
                case 0x42: this.Jam(); break;                                                                           // *JAM
                case 0x43: this.AM_IndexedIndirectX(); this.SRE(); break;                                               // *SRE (indexed indirect X)
                case 0x44: this.AM_ZeroPage(); break;                                                                   // *NOP (zero page)
                case 0x45: this.AM_ZeroPage(); this.EorR(); break;                                                      // EOR (zero page)
                case 0x46: this.AM_ZeroPage(); this.ModifyWrite(this.LSR()); break;                                     // LSR (zero page)
                case 0x47: this.AM_ZeroPage(); this.SRE(); break;                                                       // *SRE (zero page)
                case 0x48: this.Swallow(); this.Push(this.A); break;                                                    // PHA (implied)
                case 0x49: this.AM_Immediate(); this.EorR(); break;                                                     // EOR (immediate)
                case 0x4a: this.Swallow(); this.A = this.LSR(this.A); break;                                            // LSR A (implied)
                case 0x4b: this.AM_Immediate(); this.ASR(); break;                                                      // *ASR (immediate)
                case 0x4c: this.Address_Absolute(); this.Jump(this.Bus.Address.Word); break;                            // JMP (absolute)
                case 0x4d: this.AM_Absolute(); this.EorR(); break;                                                      // EOR (absolute)
                case 0x4e: this.AM_Absolute(); this.ModifyWrite(this.LSR()); break;                                     // LSR (absolute)
                case 0x4f: this.AM_Absolute(); this.SRE(); break;                                                       // *SRE (absolute)

                case 0x50: this.Branch(this.Overflow == 0); break;                                                      // BVC (relative)
                case 0x51: this.AM_IndirectIndexedY(); this.EorR(); break;                                              // EOR (indirect indexed Y)
                case 0x52: this.Jam(); break;                                                                           // *JAM
                case 0x53: this.Address_IndirectIndexedY(); this.FixupR(); this.SRE(); break;                           // *SRE (indirect indexed Y)
                case 0x54: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0x55: this.AM_ZeroPageX(); this.EorR(); break;                                                     // EOR (zero page, X)
                case 0x56: this.AM_ZeroPageX(); this.ModifyWrite(this.LSR()); break;                                    // LSR (zero page, X)
                case 0x57: this.AM_ZeroPageX(); this.SRE(); break;                                                      // *SRE (zero page, X)
                case 0x58: this.Swallow(); this.ResetFlag(StatusBits.IF); break;                                        // CLI (implied)
                case 0x59: this.AM_AbsoluteY(); this.EorR(); break;                                                     // EOR (absolute, Y)
                case 0x5a: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0x5b: this.Address_AbsoluteY(); this.FixupR(); this.SRE(); break;                                  // *SRE (absolute, Y)
                case 0x5c: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0x5d: this.AM_AbsoluteX(); this.EorR(); break;                                                     // EOR (absolute, X)
                case 0x5e: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.LSR()); break;                // LSR (absolute, X)
                case 0x5f: this.Address_AbsoluteX(); this.FixupR(); this.SRE(); break;                                  // *SRE (absolute, X)

                case 0x60: this.Swallow(); this.RTS(); break;                                                           // RTS (implied)
                case 0x61: this.AM_IndexedIndirectX(); this.ADC(); break;                                               // ADC (indexed indirect X)
                case 0x62: this.Jam(); break;                                                                           // *JAM
                case 0x63: this.AM_IndexedIndirectX(); this.RRA(); break;                                               // *RRA (indexed indirect X)
                case 0x64: this.AM_ZeroPage(); break;                                                                   // *NOP (zero page)
                case 0x65: this.AM_ZeroPage(); this.ADC(); break;                                                       // ADC (zero page)
                case 0x66: this.AM_ZeroPage(); this.ModifyWrite(this.ROR()); break;                                     // ROR (zero page)
                case 0x67: this.AM_ZeroPage(); this.RRA(); break;                                                       // *RRA (zero page)
                case 0x68: this.Swallow(); this.SwallowStack(); this.A = this.Through(this.Pop()); break;               // PLA (implied)
                case 0x69: this.AM_Immediate(); this.ADC(); break;                                                      // ADC (immediate)
                case 0x6a: this.Swallow(); this.A = this.ROR(this.A); break;                                            // ROR A (implied)
                case 0x6b: this.AM_Immediate(); this.ARR(); break;                                                      // *ARR (immediate)
                case 0x6c: this.Address_Indirect(); this.Jump(this.Bus.Address.Word); break;                            // JMP (indirect)
                case 0x6d: this.AM_Absolute(); this.ADC(); break;                                                       // ADC (absolute)
                case 0x6e: this.AM_Absolute(); this.ModifyWrite(this.ROR()); break;                                     // ROR (absolute)
                case 0x6f: this.AM_Absolute(); this.RRA(); break;                                                       // *RRA (absolute)

                case 0x70: this.Branch(this.Overflow); break;                                                           // BVS (relative)
                case 0x71: this.AM_IndirectIndexedY(); this.ADC(); break;                                               // ADC (indirect indexed Y)
                case 0x72: this.Jam(); break;                                                                           // *JAM
                case 0x73: this.Address_IndirectIndexedY(); this.FixupR(); this.RRA(); break;                           // *RRA (indirect indexed Y)
                case 0x74: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0x75: this.AM_ZeroPageX(); this.ADC(); break;                                                      // ADC (zero page, X)
                case 0x76: this.AM_ZeroPageX(); this.ModifyWrite(this.ROR()); break;                                    // ROR (zero page, X)
                case 0x77: this.AM_ZeroPageX(); this.RRA(); break;                                                      // *RRA (zero page, X)
                case 0x78: this.Swallow(); this.SetFlag(StatusBits.IF); break;                                          // SEI (implied)
                case 0x79: this.AM_AbsoluteY(); this.ADC(); break;                                                      // ADC (absolute, Y)
                case 0x7a: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0x7b: this.Address_AbsoluteY(); this.FixupR(); this.RRA(); break;                                  // *RRA (absolute, Y)
                case 0x7c: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0x7d: this.AM_AbsoluteX(); this.ADC(); break;                                                      // ADC (absolute, X)
                case 0x7e: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.ROR()); break;	            // ROR (absolute, X)
                case 0x7f: this.Address_AbsoluteX(); this.FixupR(); this.RRA(); break;                                  // *RRA (absolute, X)

                case 0x80: this.AM_Immediate(); break;                                                                  // *NOP (immediate)
                case 0x81: this.Address_IndexedIndirectX(); this.MemoryWrite(A); break;                                 // STA (indexed indirect X)
                case 0x82: this.AM_Immediate(); break;                                                                  // *NOP (immediate)
                case 0x83: this.Address_IndexedIndirectX(); this.MemoryWrite((byte)(this.A & this.X)); break;	        // *SAX (indexed indirect X)
                case 0x84: this.Address_ZeroPage(); this.MemoryWrite(this.Y); break;                                    // STY (zero page)
                case 0x85: this.Address_ZeroPage(); this.MemoryWrite(this.A); break;	                                // STA (zero page)
                case 0x86: this.Address_ZeroPage(); this.MemoryWrite(this.X); break;	                                // STX (zero page)
                case 0x87: this.Address_ZeroPage(); this.MemoryWrite((byte)(this.A & this.X)); break;	                // *SAX (zero page)
                case 0x88: this.Swallow(); this.Y = this.DEC(this.Y); break;	                                        // DEY (implied)
                case 0x89: this.AM_Immediate(); break;	                                                                // *NOP (immediate)
                case 0x8a: this.Swallow(); this.A = this.Through(this.X); break;	                                    // TXA (implied)
                case 0x8b: this.AM_Immediate(); this.ANE(); break;	                                                    // *ANE (immediate)
                case 0x8c: this.Address_Absolute(); this.MemoryWrite(this.Y); break;	                                // STY (absolute)
                case 0x8d: this.Address_Absolute(); this.MemoryWrite(this.A); break;	                                // STA (absolute)
                case 0x8e: this.Address_Absolute(); this.MemoryWrite(this.X); break;	                                // STX (absolute)
                case 0x8f: this.Address_Absolute(); this.MemoryWrite((byte)(this.A & this.X)); break;	                // *SAX (absolute)

                case 0x90: this.Branch(this.Carry == 0); break;                                                         // BCC (relative)
                case 0x91: this.Address_IndirectIndexedY(); this.Fixup(); this.MemoryWrite(this.A); break;              // STA (indirect indexed Y)
                case 0x92: this.Jam(); break;                                                                           // *JAM
                case 0x93: this.Address_IndirectIndexedY(); this.Fixup(); this.SHA(); break;                            // *SHA (indirect indexed, Y)
                case 0x94: this.Address_ZeroPageX(); this.MemoryWrite(this.Y); break;                                   // STY (zero page, X)
                case 0x95: this.Address_ZeroPageX(); this.MemoryWrite(this.A); break;                                   // STA (zero page, X)
                case 0x96: this.Address_ZeroPageY(); this.MemoryWrite(this.X); break;                                   // STX (zero page, Y)
                case 0x97: this.Address_ZeroPageY(); this.MemoryWrite((byte)(this.A & this.X)); break;                  // *SAX (zero page, Y)
                case 0x98: this.Swallow(); this.A = this.Through(this.Y); break;                                        // TYA (implied)
                case 0x99: this.Address_AbsoluteY(); this.Fixup(); this.MemoryWrite(this.A); break;                     // STA (absolute, Y)
                case 0x9a: this.Swallow(); this.S = this.X; break;                                                      // TXS (implied)
                case 0x9b: this.Address_AbsoluteY(); this.Fixup(); this.TAS(); break;                                   // *TAS (absolute, Y)
                case 0x9c: this.Address_AbsoluteX(); this.Fixup(); this.SYA(); break;                                   // *SYA (absolute, X)
                case 0x9d: this.Address_AbsoluteX(); this.Fixup(); this.MemoryWrite(this.A); break;                     // STA (absolute, X)
                case 0x9e: this.Address_AbsoluteY(); this.Fixup(); this.SXA(); break;                                   // *SXA (absolute, Y)
                case 0x9f: this.Address_AbsoluteY(); this.Fixup(); this.SHA(); break;                                   // *SHA (absolute, Y)

                case 0xa0: this.AM_Immediate(); this.Y = this.Through(); break;                                         // LDY (immediate)
                case 0xa1: this.AM_IndexedIndirectX(); this.A = this.Through(); break;                                  // LDA (indexed indirect X)
                case 0xa2: this.AM_Immediate(); this.X = this.Through(); break;                                         // LDX (immediate)
                case 0xa3: this.AM_IndexedIndirectX(); this.A = this.X = this.Through(); break;                         // *LAX (indexed indirect X)
                case 0xa4: this.AM_ZeroPage(); this.Y = this.Through(); break;                                          // LDY (zero page)
                case 0xa5: this.AM_ZeroPage(); this.A = this.Through(); break;                                          // LDA (zero page)
                case 0xa6: this.AM_ZeroPage(); this.X = this.Through(); break;                                          // LDX (zero page)
                case 0xa7: this.AM_ZeroPage(); this.A = this.X = this.Through(); break;                                 // *LAX (zero page)
                case 0xa8: this.Swallow(); this.Y = Through(this.A); break;                                             // TAY (implied)
                case 0xa9: this.AM_Immediate(); this.A = this.Through(); break;                                         // LDA (immediate)
                case 0xaa: this.Swallow(); this.X = this.Through(this.A); break;                                        // TAX (implied)
                case 0xab: this.AM_Immediate(); this.ATX(); break;                                                      // *ATX (immediate)
                case 0xac: this.AM_Absolute(); this.Y = this.Through(); break;                                          // LDY (absolute)
                case 0xad: this.AM_Absolute(); this.A = this.Through(); break;                                          // LDA (absolute)
                case 0xae: this.AM_Absolute(); this.X = this.Through(); break;                                          // LDX (absolute)
                case 0xaf: this.AM_Absolute(); this.A = this.X = this.Through(); break;                                 // *LAX (absolute)

                case 0xb0: this.Branch(this.Carry); break;                                                              // BCS (relative)
                case 0xb1: this.AM_IndirectIndexedY(); this.A = this.Through(); break;                                  // LDA (indirect indexed Y)
                case 0xb2: this.Jam(); break;                                                                           // *JAM
                case 0xb3: this.AM_IndirectIndexedY(); this.A = this.X = this.Through(); break;                         // *LAX (indirect indexed Y)
                case 0xb4: this.AM_ZeroPageX(); this.Y = this.Through(); break;                                         // LDY (zero page, X)
                case 0xb5: this.AM_ZeroPageX(); this.A = this.Through(); break;                                         // LDA (zero page, X)
                case 0xb6: this.AM_ZeroPageY(); this.X = this.Through(); break;                                         // LDX (zero page, Y)
                case 0xb7: this.AM_ZeroPageY(); this.A = this.X = this.Through(); break;                                // *LAX (zero page, Y)
                case 0xb8: this.Swallow(); this.ResetFlag(StatusBits.VF); break;                                        // CLV (implied)
                case 0xb9: this.AM_AbsoluteY(); this.A = this.Through(); break;                                         // LDA (absolute, Y)
                case 0xba: this.Swallow(); this.X = this.Through(this.S); break;                                        // TSX (implied)
                case 0xbb: this.Address_AbsoluteY(); this.MaybeFixup(); this.LAS(); break;                              // *LAS (absolute, Y)
                case 0xbc: this.AM_AbsoluteX(); this.Y = this.Through(); break;                                         // LDY (absolute, X)
                case 0xbd: this.AM_AbsoluteX(); this.A = this.Through(); break;                                         // LDA (absolute, X)
                case 0xbe: this.AM_AbsoluteY(); this.X = this.Through(); break;                                         // LDX (absolute, Y)
                case 0xbf: this.AM_AbsoluteY(); this.A = this.X = this.Through(); break;                                // *LAX (absolute, Y)

                case 0xc0: this.AM_Immediate(); this.CMP(this.Y); break;                                                // CPY (immediate)
                case 0xc1: this.AM_IndexedIndirectX(); this.CMP(this.A); break;                                         // CMP (indexed indirect X)
                case 0xc2: this.AM_Immediate(); break;                                                                  // *NOP (immediate)
                case 0xc3: this.AM_IndexedIndirectX(); this.DCP(); break;                                               // *DCP (indexed indirect X)
                case 0xc4: this.AM_ZeroPage(); this.CMP(this.Y); break;                                                 // CPY (zero page)
                case 0xc5: this.AM_ZeroPage(); this.CMP(this.A); break;                                                 // CMP (zero page)
                case 0xc6: this.AM_ZeroPage(); this.ModifyWrite(this.DEC()); break;                                     // DEC (zero page)
                case 0xc7: this.AM_ZeroPage(); this.DCP(); break;                                                       // *DCP (zero page)
                case 0xc8: this.Swallow(); this.Y = this.INC(this.Y); break;                                            // INY (implied)
                case 0xc9: this.AM_Immediate(); this.CMP(this.A); break;                                                // CMP (immediate)
                case 0xca: this.Swallow(); this.X = this.DEC(this.X); break;                                            // DEX (implied)
                case 0xcb: this.AM_Immediate(); this.AXS(); break;                                                      // *AXS (immediate)
                case 0xcc: this.AM_Absolute(); this.CMP(this.Y); break;                                                 // CPY (absolute)
                case 0xcd: this.AM_Absolute(); this.CMP(this.A); break;                                                 // CMP (absolute)
                case 0xce: this.AM_Absolute(); this.ModifyWrite(this.DEC()); break;                                     // DEC (absolute)
                case 0xcf: this.AM_Absolute(); this.DCP(); break;                                                       // *DCP (absolute)

                case 0xd0: this.Branch(this.Zero == 0); break;                                                          // BNE (relative)
                case 0xd1: this.AM_IndirectIndexedY(); this.CMP(this.A); break;                                         // CMP (indirect indexed Y)
                case 0xd2: this.Jam(); break;                                                                           // *JAM
                case 0xd3: this.Address_IndirectIndexedY(); this.FixupR(); this.DCP(); break;                           // *DCP (indirect indexed Y)
                case 0xd4: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0xd5: this.AM_ZeroPageX(); this.CMP(this.A); break;                                                // CMP (zero page, X)
                case 0xd6: this.AM_ZeroPageX(); this.ModifyWrite(this.DEC()); break;                                    // DEC (zero page, X)
                case 0xd7: this.AM_ZeroPageX(); this.DCP(); break;                                                      // *DCP (zero page, X)
                case 0xd8: this.Swallow(); this.ResetFlag(StatusBits.DF); break;                                        // CLD (implied)
                case 0xd9: this.AM_AbsoluteY(); this.CMP(this.A); break;                                                // CMP (absolute, Y)
                case 0xda: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0xdb: this.Address_AbsoluteY(); this.FixupR(); this.DCP(); break;                                  // *DCP (absolute, Y)
                case 0xdc: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0xdd: this.AM_AbsoluteX(); this.CMP(this.A); break;                                                // CMP (absolute, X)
                case 0xde: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.DEC()); break;                // DEC (absolute, X)
                case 0xdf: this.Address_AbsoluteX(); this.FixupR(); this.DCP(); break;                                  // *DCP (absolute, X)

                case 0xe0: this.AM_Immediate(); this.CMP(this.X); break;                                                // CPX (immediate)
                case 0xe1: this.AM_IndexedIndirectX(); this.SBC(); break;                                               // SBC (indexed indirect X)
                case 0xe2: this.AM_Immediate(); break;                                                                  // *NOP (immediate)
                case 0xe3: this.AM_IndexedIndirectX(); this.ISB(); break;                                               // *ISB (indexed indirect X)
                case 0xe4: this.AM_ZeroPage(); this.CMP(this.X); break;                                                 // CPX (zero page)
                case 0xe5: this.AM_ZeroPage(); this.SBC(); break;                                                       // SBC (zero page)
                case 0xe6: this.AM_ZeroPage(); this.ModifyWrite(INC()); break;                                          // INC (zero page)
                case 0xe7: this.AM_ZeroPage(); this.ISB(); break;                                                       // *ISB (zero page)
                case 0xe8: this.Swallow(); this.X = this.INC(this.X); break;                                            // INX (implied)
                case 0xe9: this.AM_Immediate(); this.SBC(); break;                                                      // SBC (immediate)
                case 0xea: this.Swallow(); break;                                                                       // NOP (implied)
                case 0xeb: this.AM_Immediate(); this.SBC(); break;                                                      // *SBC (immediate)
                case 0xec: this.AM_Absolute(); this.CMP(this.X); break;                                                 // CPX (absolute)
                case 0xed: this.AM_Absolute(); this.SBC(); break;                                                       // SBC (absolute)
                case 0xee: this.AM_Absolute(); this.ModifyWrite(this.INC()); break;                                     // INC (absolute)
                case 0xef: this.AM_Absolute(); this.ISB(); break;                                                       // *ISB (absolute)

                case 0xf0: this.Branch(this.Zero); break;                                                               // BEQ (relative)
                case 0xf1: this.AM_IndirectIndexedY(); this.SBC(); break;                                               // SBC (indirect indexed Y)
                case 0xf2: this.Jam(); break;                                                                           // *JAM
                case 0xf3: this.Address_IndirectIndexedY(); this.FixupR(); this.ISB(); break;                           // *ISB (indirect indexed Y)
                case 0xf4: this.AM_ZeroPageX(); break;                                                                  // *NOP (zero page, X)
                case 0xf5: this.AM_ZeroPageX(); this.SBC(); break;                                                      // SBC (zero page, X)
                case 0xf6: this.AM_ZeroPageX(); this.ModifyWrite(this.INC()); break;                                    // INC (zero page, X)
                case 0xf7: this.AM_ZeroPageX(); this.ISB(); break;                                                      // *ISB (zero page, X)
                case 0xf8: this.Swallow(); this.SetFlag(StatusBits.DF); break;                                          // SED (implied)
                case 0xf9: this.AM_AbsoluteY(); this.SBC(); break;                                                      // SBC (absolute, Y)
                case 0xfa: this.Swallow(); break;                                                                       // *NOP (implied)
                case 0xfb: this.Address_AbsoluteY(); this.FixupR(); this.ISB(); break;                                  // *ISB (absolute, Y)
                case 0xfc: this.Address_AbsoluteX(); this.MaybeFixupR(); break;                                         // *NOP (absolute, X)
                case 0xfd: this.AM_AbsoluteX(); this.SBC(); break;                                                      // SBC (absolute, X)
                case 0xfe: this.Address_AbsoluteX(); this.FixupR(); this.ModifyWrite(this.INC()); break;                // INC (absolute, X)
                case 0xff: this.Address_AbsoluteX(); this.FixupR(); this.ISB(); break;	                                // *ISB (absolute, X)
            }
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

            this.OnExecutedInstruction();
            return this.Cycles;
        }

        private void FetchInstruction()
        {

            // Instruction fetch beginning
            this.LowerSYNC();

            System.Diagnostics.Debug.Assert(this.Cycles == 1, "An extra cycle has occurred");

            // Can't use fetchByte, since that would add an extra tick.
            this.Address_Immediate();
            this.OpCode = this.ReadFromBus();

            System.Diagnostics.Debug.Assert(this.Cycles == 1, "BUS read has introduced stray cycles");

            // Instruction fetch has now completed
            this.RaiseSYNC();
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
            this.WriteToBus();
        }

        protected override sealed byte BusRead()
        {
            this.Tick();
            return this.ReadFromBus();
        }

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        // Status flag operations

        private void SetFlag(StatusBits flag)
        {
            this.P = SetBit(this.P, flag);
        }

        private void SetFlag(StatusBits which, int condition)
        {
            this.P = SetBit(this.P, which, condition);
        }

        private void SetFlag(StatusBits which, bool condition)
        {
            this.P = SetBit(this.P, which, condition);
        }

        private void ResetFlag(StatusBits which)
        {
            this.P = ClearBit(this.P, which);
        }

        private void ResetFlag(StatusBits which, int condition)
        {
            this.P = ClearBit(this.P, which, condition);
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
                this.Push((byte)(this.P | (source == InterruptSource.hardware ? 0 : (byte)StatusBits.BF)));
            }
            this.SetFlag(StatusBits.IF);   // Disable IRQ
            this.Jump(this.GetWordPaged(0xff, vector).Word);
        }

        private void UpdateStack(byte position)
        {
            this.Bus.Address.Low = position;
            this.Bus.Address.High = 1;
        }

        private void LowerStack() => this.UpdateStack(this.S--);

        private void RaiseStack() => this.UpdateStack(++this.S);

        private void DummyPush()
        {
            this.LowerStack();
            this.Tick();    // In place of the memory write
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


        // Addressing modes

        private void NoteFixedAddress(int address)
        {
            this.NoteFixedAddress((ushort)address);
        }

        private void NoteFixedAddress(ushort address)
        {
            this.intermediate.Word = address;
            this.fixedPage = this.intermediate.High;
            this.Bus.Address.Low = this.intermediate.Low;
        }

        private void Address_Immediate() => this.Bus.Address.Word = this.PC.Word++;

        private void Address_Absolute() => this.Bus.Address.Word = this.FetchWord().Word;

        private void Address_ZeroPage()
        {
            this.Bus.Address.Low = this.FetchByte();
            this.Bus.Address.High = 0;
        }

        private void Address_ZeroPageIndirect()
        {
            this.Address_ZeroPage();
            this.Bus.Address.Word = this.GetWordPaged().Word;
        }

        private void Address_Indirect()
        {
            this.Address_Absolute();
            this.Bus.Address.Word = this.GetWordPaged().Word;
        }

        private void Address_ZeroPageWithIndex(byte index)
        {
            this.AM_ZeroPage();
            this.Bus.Address.Low += index;
        }

        private void Address_ZeroPageX() => this.Address_ZeroPageWithIndex(this.X);

        private void Address_ZeroPageY() => this.Address_ZeroPageWithIndex(this.Y);

        private void Address_AbsoluteWithIndex(byte index)
        {
            this.Address_Absolute();
            this.NoteFixedAddress(this.Bus.Address.Word + index);
        }

        private void Address_AbsoluteX() => this.Address_AbsoluteWithIndex(X);

        private void Address_AbsoluteY() => this.Address_AbsoluteWithIndex(Y);

        private void Address_IndexedIndirectX()
        {
            this.Address_ZeroPageX();
            this.Bus.Address.Word = this.GetWordPaged().Word;
        }

        private void Address_IndirectIndexedY()
        {
            this.Address_ZeroPageIndirect();
            this.NoteFixedAddress(this.Bus.Address.Word + Y);
        }

        // Addressing modes, with read

        private void AM_Immediate()
        {
            this.Address_Immediate();
            this.MemoryRead();
        }

        private void AM_Absolute()
        {
            this.Address_Absolute();
            this.MemoryRead();
        }

        private void AM_ZeroPage()
        {
            this.Address_ZeroPage();
            this.MemoryRead();
        }

        private void AM_ZeroPageX()
        {
            this.Address_ZeroPageX();
            this.MemoryRead();
        }

        private void AM_ZeroPageY()
        {
            this.Address_ZeroPageY();
            this.MemoryRead();
        }

        private void AM_IndexedIndirectX()
        {
            this.Address_IndexedIndirectX();
            this.MemoryRead();
        }

        private void AM_AbsoluteX()
        {
            this.Address_AbsoluteX();
            this.MaybeFixupR();
        }

        private void AM_AbsoluteY()
        {
            this.Address_AbsoluteY();
            this.MaybeFixupR();
        }

        private void AM_IndirectIndexedY()
        {
            this.Address_IndirectIndexedY();
            this.MaybeFixupR();
        }

        private void AdjustZero(byte datum) => this.ResetFlag(StatusBits.ZF, datum);

        private void AdjustNegative(byte datum) => this.SetFlag(StatusBits.NF, NegativeTest(datum));

        private void AdjustNZ(byte datum)
        {
            this.AdjustZero(datum);
            this.AdjustNegative(datum);
        }

        private void Branch(int condition) => this.Branch(condition != 0);

        private void Branch(bool condition)
        {
            this.AM_Immediate();
            if (condition)
            {
                var relative = (sbyte)this.Bus.Data;
                this.Swallow();
                this.NoteFixedAddress(this.PC.Word + relative);
                this.MaybeFixup();
                this.Jump(this.Bus.Address.Word);
            }
        }

        private byte Through() => this.Through(this.Bus.Data);

        private byte Through(int data) => this.Through((byte)data);

        private byte Through(byte data)
        {
            this.AdjustNZ(data);
            return data;
        }

        private void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            this.MemoryWrite();
            this.MemoryWrite(data);
        }

        // Flag adjustment

        private void AdjustOverflow_add(byte operand)
        {
            var data = Bus.Data;
            var intermediate = this.intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)(~(operand ^ data) & (operand ^ intermediate))));
        }

        private void AdjustOverflow_subtract(byte operand)
        {
            var data = Bus.Data;
            var intermediate = this.intermediate.Low;
            this.SetFlag(StatusBits.VF, NegativeTest((byte)((operand ^ data) & (operand ^ intermediate))));
        }

        // Miscellaneous

        private void MaybeFixup()
        {
            if (this.Bus.Address.High != this.fixedPage)
            {
                this.Fixup();
            }
        }

        private void Fixup()
        {
            this.MemoryRead();
            this.Bus.Address.High = this.fixedPage;
        }

        private void MaybeFixupR()
        {
            this.MaybeFixup();
            this.MemoryRead();
        }

        private void FixupR()
        {
            this.Fixup();
            this.MemoryRead();
        }

        // Chew up a cycle

        private void Swallow() => this.MemoryRead(this.PC);

        private void SwallowStack() => this.MemoryRead(this.S, 1);

        private void SwallowFetch() => this.FetchByte();

        // Instruction implementations

        // Instructions with BCD effects

        private void SBC()
        {
            var operand = this.A;
            A = this.SUB(operand, CarryTest((byte)~this.P));

            this.AdjustOverflow_subtract(operand);
            this.AdjustNZ(this.intermediate.Low);
            this.ResetFlag(StatusBits.CF, this.intermediate.High);
        }

        private byte SUB(byte operand, int borrow) => this.Decimal != 0 ? SUB_d(operand, borrow) : SUB_b(operand, borrow);

        private byte SUB_b(byte operand, int borrow = 0)
        {
            var data = Bus.Data;
            this.intermediate.Word = (ushort)(operand - data - borrow);
            return this.intermediate.Low;
        }

        private byte SUB_d(byte operand, int borrow)
        {
            _ = this.SUB_b(operand, borrow);

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

        private void ADC()
        {
            if (this.Decimal != 0)
                this.ADC_d();
            else
                this.ADC_b();
        }

        private void ADC_b()
        {
            var operand = A;
            var data = Bus.Data;
            this.intermediate.Word = (ushort)(operand + data + this.Carry);

            this.AdjustOverflow_add(operand);
            this.SetFlag(StatusBits.CF, CarryTest(this.intermediate.High));

            this.AdjustNZ(intermediate.Low);

            this.A = intermediate.Low;
        }

        private void ADC_d()
        {
            var operand = this.A;
            var data = this.Bus.Data;

            var low = (ushort)(LowerNibble(operand) + LowerNibble(data) + this.Carry);
            this.intermediate.Word = (ushort)(HigherNibble(operand) + HigherNibble(data));

            this.AdjustZero(LowByte((ushort)(low + this.intermediate.Word)));

            if (low > 0x09)
            {
                this.intermediate.Word += 0x10;
                low += 0x06;
            }

            this.AdjustNegative(this.intermediate.Low);
            this.AdjustOverflow_add(operand);

            if (this.intermediate.Word > 0x90)
                this.intermediate.Word += 0x60;

            this.SetFlag(StatusBits.CF, this.intermediate.High);

            this.A = (byte)(LowerNibble(LowByte(low)) | HigherNibble(this.intermediate.Low));
        }

        // Undocumented compound instructions (with BCD effects)

        private void ARR()
        {
            var value = this.Bus.Data;
            if (this.Decimal != 0)
                this.ARR_d(value);
            else
                this.ARR_b(value);
        }

        private void ARR_d(byte value)
        {
            // With thanks to https://github.com/TomHarte/CLK
            // What a very strange instruction ARR is...

            this.A &= value;
            var unshiftedA = this.A;
            this.A = this.Through((this.A >> 1) | (this.Carry << 7));
            this.SetFlag(StatusBits.VF, OverflowTest((byte)(this.A ^ (this.A << 1))));

            if (LowerNibble(unshiftedA) + (unshiftedA & 0x1) > 5)
                this.A = (byte)(LowerNibble((byte)(this.A + 6)) | HigherNibble(this.A));

            this.SetFlag(StatusBits.CF, HigherNibble(unshiftedA) + (unshiftedA & 0x10) > 0x50);

            if (this.Carry != 0)
                this.A += 0x60;
        }

        private void ARR_b(byte value)
        {
            this.A &= value;
            this.A = this.Through((this.A >> 1) | (this.Carry << 7));
            this.SetFlag(StatusBits.CF, OverflowTest(this.A));
            this.SetFlag(StatusBits.VF, OverflowTest((byte)(this.A ^ (this.A << 1))));
        }


        private void OrR() => this.A = this.Through(this.A | this.Bus.Data);

        private void AndR() => this.A = this.Through(this.A & this.Bus.Data);

        private void EorR() => this.A = this.Through(this.A ^ this.Bus.Data);

        private void BIT()
        {
            var data = this.Bus.Data;
            this.SetFlag(StatusBits.VF, OverflowTest(data));
            this.AdjustZero((byte)(this.A & data));
            this.AdjustNegative(data);
        }

        private void CMP(byte first)
        {
            var second = Bus.Data;
            this.intermediate.Word = (ushort)(first - second);
            AdjustNZ(this.intermediate.Low);
            ResetFlag(StatusBits.CF, this.intermediate.High);
        }


        private byte DEC() => this.DEC(this.Bus.Data);

        private byte DEC(byte value) => this.Through(value - 1);

        private byte INC() => this.INC(this.Bus.Data);

        private byte INC(byte value) => this.Through(value + 1);

        private void JSR()
        {
            this.intermediate.Low = this.FetchByte();
            this.SwallowStack();
            this.PushWord(this.PC);
            this.intermediate.High = this.FetchByte();
            this.PC.Word = this.intermediate.Word;
        }

        private void PHP() => this.Push((byte)(this.P | (byte)StatusBits.BF));

        private void PLP()
        {
            this.SwallowStack();
            this.P = (byte)((this.Pop() | (byte)StatusBits.RF) & (byte)~StatusBits.BF);
        }

        private void RTI()
        {
            this.PLP();
            this.Return();
        }

        private void RTS()
        {
            this.SwallowStack();
            this.Return();
            this.SwallowFetch();
        }

        private byte ASL() => this.ASL(this.Bus.Data);

        private byte ASL(byte value)
        {
            this.SetFlag(StatusBits.CF, NegativeTest(value));
            return this.Through(value << 1);
        }

        private byte ROL() => this.ROL(this.Bus.Data);

        private byte ROL(byte value)
        {
            var carryIn = this.Carry;
            return this.Through(this.ASL(value) | carryIn);
        }

        private byte LSR() => this.LSR(this.Bus.Data);

        private byte LSR(byte value)
        {
            this.SetFlag(StatusBits.CF, CarryTest(value));
            return this.Through(value >> 1);
        }

        private byte ROR() => this.ROR(this.Bus.Data);

        private byte ROR(byte value)
        {
            var carryIn = this.Carry;
            return this.Through(this.LSR(value) | (carryIn << 7));
        }

        // Undocumented compound instructions

        private void ANC()
        {
            this.AndR();
            this.SetFlag(StatusBits.CF, NegativeTest(this.A));
        }

        private void AXS()
        {
            this.X = this.Through(this.SUB_b((byte)(this.A & this.X)));
            this.ResetFlag(StatusBits.CF, this.intermediate.High);
        }


        private void Jam()
        {
            this.Bus.Address.Word = this.PC.Word--;
            this.MemoryRead();
            this.MemoryRead();
        }

        private void StoreFixupEffect(byte data)
        {
            var fixedAddress = (byte)(this.Bus.Address.High + 1);
            this.MemoryWrite((byte)(data & fixedAddress));
        }

        private void SHA() => this.StoreFixupEffect((byte)(this.A & this.X));

        private void SYA() => this.StoreFixupEffect(this.Y);

        private void SXA() => this.StoreFixupEffect(this.X);

        private void TAS()
        {
            this.S = (byte)(this.A & this.X);
            this.SHA();
        }

        private void LAS() => this.A = this.X = this.S = this.Through(this.MemoryRead() & this.S);

        private void ANE() => this.A = this.Through((this.A | 0xee) & this.X & this.Bus.Data);

        private void ATX() => this.A = this.X = this.Through((this.A | 0xee) & this.Bus.Data);

        private void ASR()
        {
            this.AndR();
            this.A = this.LSR(this.A);
        }

        private void ISB()
        {
            this.ModifyWrite(this.INC());
            this.SBC();
        }

        private void RLA()
        {
            this.ModifyWrite(this.ROL());
            this.AndR();
        }

        private void RRA()
        {
            this.ModifyWrite(this.ROR());
            this.ADC();
        }

        private void SLO()
        {
            this.ModifyWrite(this.ASL());
            this.OrR();
        }

        private void SRE()
        {
            this.ModifyWrite(this.LSR());
            this.EorR();
        }

        private void DCP()
        {
            this.ModifyWrite(this.DEC());
            this.CMP(this.A);
        }
    }
}