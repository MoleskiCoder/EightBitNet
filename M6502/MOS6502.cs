// <copyright file="MOS6502.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502
{
    using EightBit;

    public class MOS6502(Bus bus) : Core(bus)
    {
        #region Core instruction dispatching

        protected override bool MaybeExecute()
        {
            if (base.MaybeExecute())
            {
                return true;
            }

            var cycles = this.Cycles;
            switch (this.OpCode)
            {
                case 0x02: this.JAM(); break;                                                               // *JAM
                case 0x03: this.IndexedIndirectX(); this.SLO(); break;                                      // *SLO (indexed indirect X)
                case 0x04: this.ZeroPage(); break;                                                          // *NOP (zero page)
                case 0x07: this.ZeroPage(); this.SLO(); break;                                              // *SLO (zero page)
                case 0x0b: this.FetchByte(); this.ANC(); break;                                             // *ANC (immediate)
                case 0x0c: this.Absolute(); break;                                                          // *NOP (absolute)
                case 0x0f: this.Absolute(); this.SLO(); break;                                              // *SLO (absolute)

                case 0x12: this.JAM(); break;                                                               // *JAM
                case 0x13: this.IndirectIndexedYAddress(); this.FixupRead(); this.SLO(); break;             // *SLO (indirect indexed Y)
                case 0x14: this.ZeroPageX(); break;                                                         // *NOP (zero page, X)
                case 0x17: this.ZeroPageX(); this.SLO(); break;                                             // *SLO (zero page, X)
                case 0x1a: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0x1b: this.AbsoluteYAddress(); this.FixupRead(); this.SLO(); break;                    // *SLO (absolute, Y)
                case 0x1c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0x1f: this.AbsoluteXAddress(); this.FixupRead(); this.SLO(); break;                    // *SLO (absolute, X)

                case 0x22: this.JAM(); break;                                                               // *JAM
                case 0x23: this.IndexedIndirectX(); this.RLA(); break;                                      // *RLA (indexed indirect X)
                case 0x27: this.ZeroPage(); this.RLA(); break;                                              // *RLA (zero page)
                case 0x2b: this.FetchByte(); this.ANC(); break;                                             // *ANC (immediate)
                case 0x2f: this.Absolute(); this.RLA(); break;                                              // *RLA (absolute)

                case 0x32: this.JAM(); break;													            // *JAM
                case 0x33: this.IndirectIndexedYAddress(); this.FixupRead(); this.RLA(); break;             // *RLA (indirect indexed Y)
                case 0x34: this.ZeroPageX(); break;                                                         // *NOP (zero page, X)
                case 0x37: this.ZeroPageX(); this.RLA(); break;                                             // *RLA (zero page, X)
                case 0x3a: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0x3b: this.AbsoluteYAddress(); this.FixupRead(); this.RLA(); break;                    // *RLA (absolute, Y)
                case 0x3c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0x3f: this.AbsoluteXAddress(); this.FixupRead(); this.RLA(); break;                    // *RLA (absolute, X)

                case 0x42: this.JAM(); break;                                                               // *JAM
                case 0x43: this.IndexedIndirectX(); this.SRE(); break;                                      // *SRE (indexed indirect X)
                case 0x47: this.ZeroPage(); this.SRE(); break;                                              // *SRE (zero page)
                case 0x4b: this.FetchByte(); this.ASR(); break;                                             // *ASR (immediate)
                case 0x4f: this.Absolute(); this.SRE(); break;                                              // *SRE (absolute)

                case 0x52: this.JAM(); break;                                                               // *JAM
                case 0x53: this.IndirectIndexedYAddress(); this.FixupRead(); this.SRE(); break;             // *SRE (indirect indexed Y)
                case 0x57: this.ZeroPageX(); this.SRE(); break;                                             // *SRE (zero page, X)
                case 0x5a: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0x5b: this.AbsoluteYAddress(); this.FixupRead(); this.SRE(); break;                    // *SRE (absolute, Y)
                case 0x5c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0x5f: this.AbsoluteXAddress(); this.FixupRead(); this.SRE(); break;                    // *SRE (absolute, X)

                case 0x62: this.JAM(); break;                                                               // *JAM
                case 0x63: this.IndexedIndirectX(); this.RRA(); break;                                      // *RRA (indexed indirect X)
                case 0x64: this.ZeroPage(); break;                                                          // *NOP (zero page)
                case 0x67: this.ZeroPage(); this.RRA(); break;                                              // *RRA (zero page)
                case 0x6b: this.FetchByte(); this.ARR(); break;                                             // *ARR (immediate)
                case 0x6f: this.Absolute(); this.RRA(); break;                                              // *RRA (absolute)

                case 0x72: this.JAM(); break;                                                               // *JAM
                case 0x73: this.IndirectIndexedYAddress(); this.FixupRead(); this.RRA(); break;             // *RRA (indirect indexed Y)
                case 0x74: this.ZeroPageX(); break;                                                         // *NOP (zero page, X)
                case 0x77: this.ZeroPageX(); this.RRA(); break;                                             // *RRA (zero page, X)
                case 0x7a: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0x7b: this.AbsoluteYAddress(); this.FixupRead(); this.RRA(); break;                    // *RRA (absolute, Y)
                case 0x7c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0x7f: this.AbsoluteXAddress(); this.FixupRead(); this.RRA(); break;                    // *RRA (absolute, X)

                case 0x80: this.FetchByte(); break;                                                         // *NOP (immediate)
                case 0x83: this.IndexedIndirectXAddress(); this.SAX(); break;                               // *SAX (indexed indirect X)
                case 0x87: this.ZeroPageAddress(); this.SAX(); break;	                                    // *SAX (zero page)
                case 0x89: this.FetchByte(); break;	                                                        // *NOP (immediate)
                case 0x8b: this.FetchByte(); this.ANE(); break;	                                            // *ANE (immediate)
                case 0x8f: this.AbsoluteAddress(); this.SAX(); break;	                                    // *SAX (absolute)

                case 0x92: this.JAM(); break;                                                               // *JAM
                case 0x93: this.IndirectIndexedYAddress(); this.Fixup(); this.SHA(); break;                 // *SHA (indirect indexed, Y)
                case 0x97: this.ZeroPageYAddress(); this.SAX(); break;                                      // *SAX (zero page, Y)
                case 0x9b: this.AbsoluteYAddress(); this.Fixup(); this.TAS(); break;                        // *TAS (absolute, Y)
                case 0x9c: this.AbsoluteXAddress(); this.Fixup(); this.SYA(); break;                        // *SYA (absolute, X)
                case 0x9e: this.AbsoluteYAddress(); this.Fixup(); this.SXA(); break;                        // *SXA (absolute, Y)
                case 0x9f: this.AbsoluteYAddress(); this.Fixup(); this.SHA(); break;                        // *SHA (absolute, Y)

                case 0xa3: this.IndexedIndirectX(); this.LAX(); break;                                      // *LAX (indexed indirect X)
                case 0xa7: this.ZeroPage(); this.LAX(); break;                                              // *LAX (zero page)
                case 0xab: this.FetchByte(); this.ATX(); break;                                             // *ATX (immediate)
                case 0xaf: this.Absolute(); this.LAX(); break;                                              // *LAX (absolute)

                case 0xb2: this.JAM(); break;                                                               // *JAM
                case 0xb3: this.IndirectIndexedY(); this.LAX(); break;                                      // *LAX (indirect indexed Y)
                case 0xb7: this.ZeroPageY(); this.LAX(); break;                                             // *LAX (zero page, Y)
                case 0xbb: this.AbsoluteYAddress(); this.MaybeFixup(); this.LAS(); break;                   // *LAS (absolute, Y)
                case 0xbf: this.AbsoluteY(); this.LAX(); break;                                             // *LAX (absolute, Y)

                case 0xc3: this.IndexedIndirectX(); this.DCP(); break;                                      // *DCP (indexed indirect X)
                case 0xc7: this.ZeroPage(); this.DCP(); break;                                              // *DCP (zero page)
                case 0xcb: this.FetchByte(); this.AXS(); break;                                             // *AXS (immediate)
                case 0xcf: this.Absolute(); this.DCP(); break;                                              // *DCP (absolute)

                case 0xd2: this.JAM(); break;                                                               // *JAM
                case 0xd3: this.IndirectIndexedYAddress(); this.FixupRead(); this.DCP(); break;             // *DCP (indirect indexed Y)
                case 0xd7: this.ZeroPageX(); this.DCP(); break;                                             // *DCP (zero page, X)
                case 0xda: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0xdb: this.AbsoluteYAddress(); this.FixupRead(); this.DCP(); break;                    // *DCP (absolute, Y)
                case 0xdc: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0xdf: this.AbsoluteXAddress(); this.FixupRead(); this.DCP(); break;                    // *DCP (absolute, X)

                case 0xe3: this.IndexedIndirectX(); this.ISB(); break;                                      // *ISB (indexed indirect X)
                case 0xe7: this.ZeroPage(); this.ISB(); break;                                              // *ISB (zero page)
                case 0xeb: this.FetchByte(); this.SBC(); break;                                             // *SBC (immediate)
                case 0xef: this.Absolute(); this.ISB(); break;                                              // *ISB (absolute)

                case 0xf2: this.JAM(); break;                                                               // *JAM
                case 0xf3: this.IndirectIndexedYAddress(); this.FixupRead(); this.ISB(); break;             // *ISB (indirect indexed Y)
                case 0xf7: this.ZeroPageX(); this.ISB(); break;                                             // *ISB (zero page, X)
                case 0xfa: this.SwallowRead(); break;                                                       // *NOP (implied)
                case 0xfb: this.AbsoluteYAddress(); this.FixupRead(); this.ISB(); break;                    // *ISB (absolute, Y)
                case 0xfc: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                           // *NOP (absolute, X)
                case 0xff: this.AbsoluteXAddress(); this.FixupRead(); this.ISB(); break;	                // *ISB (absolute, X)
            }

            return cycles != this.Cycles;
        }

        #endregion

        #region Bus/Memory Access

        protected override void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            this.MemoryWrite();     // Modify cycle
            this.MemoryWrite(data); // Write cycle
        }

        #endregion

        #region Addressing modes

        protected override void IndirectAddress()
        {
            this.AbsoluteAddress();
            this.GetAddressPaged();
        }

        #region Address page fixup

        protected override void Fixup()
        {
            this.MemoryRead();
            this.Bus.Address.High = this.FixedPage;
        }

        protected override void FixupBranch(sbyte relative)
        {
            this.NoteFixedAddress(this.PC.Word + relative);
            this.MaybeFixup();
        }

        #endregion

        #endregion

        #region Instruction implementations

        #region Undocumented instructions

        #region Undocumented instructions with BCD effects

        private void ARR()
        {
            var value = this.Bus.Data;
            if (this.DecimalMasked != 0)
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
            this.A = this.Through(this.A >> 1 | this.Carry << 7);
            this.SetFlag(StatusBits.VF, OverflowTest((byte)(this.A ^ this.A << 1)));

            if (LowerNibble(unshiftedA) + (unshiftedA & 0x1) > 5)
                this.A = (byte)(LowerNibble((byte)(this.A + 6)) | HigherNibble(this.A));

            this.SetFlag(StatusBits.CF, HigherNibble(unshiftedA) + (unshiftedA & 0x10) > 0x50);

            if (this.Carry != 0)
                this.A += 0x60;
        }

        private void ARR_b(byte value)
        {
            this.A &= value;
            this.A = this.Through(this.A >> 1 | this.Carry << 7);
            this.SetFlag(StatusBits.CF, OverflowTest(this.A));
            this.SetFlag(StatusBits.VF, OverflowTest((byte)(this.A ^ this.A << 1)));
        }

        #endregion

        #region Undocumented instructions with fixup effects

        private void StoreFixupEffect(byte data)
        {
            var mask = (byte)(this.UnfixedPage + 1);   // base_hi + 1, always
            var updated = (byte)(data & mask);

            if (this.Fixed)
            {
                this.Bus.Address.High = updated;
            }

            this.MemoryWrite(updated);
        }

        private void SHA() => this.StoreFixupEffect((byte)(this.A & this.X));

        private void SYA() => this.StoreFixupEffect(this.Y);

        private void SXA() => this.StoreFixupEffect(this.X);

        #endregion

        private void SAX() => this.MemoryWrite((byte)(this.A & this.X));

        private void LAX()
        {
            this.LDA();
            this.LDX();
        }

        private void ANC()
        {
            this.AND();
            this.SetFlag(StatusBits.CF, NegativeTest(this.A));
        }

        private void AXS()
        {
            this.X = this.Through(this.BinarySUB((byte)(this.A & this.X)));
            this.ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        private void JAM()
        {
            this.SwallowRead();
            this.MemoryRead(0xff, 0xff);
            this.Bus.Address.Low = 0xfe;
            this.MemoryRead();
            this.MemoryRead();
            this.Bus.Address.Low = 0xff;
            this.MemoryRead();
            this.MemoryRead();
            this.MemoryRead();
            this.MemoryRead();
            this.MemoryRead();
            this.MemoryRead();
        }

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
            this.AND();
            this.ImplementLSRA();
        }

        private void ISB()
        {
            this.INC();
            this.SBC();
        }

        private void RLA()
        {
            this.ROL();
            this.AND();
        }

        private void RRA()
        {
            this.ROR();
            this.ADC();
        }

        private void SLO()
        {
            this.ASL();
            this.ORA();
        }

        private void SRE()
        {
            this.LSR();
            this.EOR();
        }

        private void DCP()
        {
            this.DEC();
            this.CMP();
        }

        #endregion

        #endregion
    }
}