// <copyright file="M6502.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class M6502(Bus bus) : M6502Core(bus)
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
                case 0x02: this.Jam(); break;                                                                           // *JAM
                case 0x03: this.IndexedIndirectXRead(); this.SLO(); break;                                              // *SLO (indexed indirect X)
                case 0x04: this.ZeroPageRead(); break;                                                                  // *NOP (zero page)
                case 0x07: this.ZeroPageRead(); this.SLO(); break;                                                      // *SLO (zero page)
                case 0x0b: this.ImmediateRead(); this.ANC(); break;                                                     // *ANC (immediate)
                case 0x0c: this.AbsoluteRead(); break;                                                                  // *NOP (absolute)
                case 0x0f: this.AbsoluteRead(); this.SLO(); break;                                                      // *SLO (absolute)

                case 0x12: this.Jam(); break;                                                                           // *JAM
                case 0x13: this.IndirectIndexedYAddress(); this.FixupRead(); this.SLO(); break;                         // *SLO (indirect indexed Y)
                case 0x14: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0x17: this.ZeroPageXRead(); this.SLO(); break;                                                     // *SLO (zero page, X)
                case 0x1a: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0x1b: this.AbsoluteYAddress(); this.FixupRead(); this.SLO(); break;                                // *SLO (absolute, Y)
                case 0x1c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0x1f: this.AbsoluteXAddress(); this.FixupRead(); this.SLO(); break;                                // *SLO (absolute, X)

                case 0x22: this.Jam(); break;                                                                           // *JAM
                case 0x23: this.IndexedIndirectXRead(); this.RLA(); ; break;                                            // *RLA (indexed indirect X)
                case 0x27: this.ZeroPageRead(); this.RLA(); ; break;                                                    // *RLA (zero page)
                case 0x2b: this.ImmediateRead(); this.ANC(); break;                                                     // *ANC (immediate)
                case 0x2f: this.AbsoluteRead(); this.RLA(); break;                                                      // *RLA (absolute)

                case 0x32: this.Jam(); break;																			// *JAM
                case 0x33: this.IndirectIndexedYAddress(); this.FixupRead(); this.RLA(); break;                         // *RLA (indirect indexed Y)
                case 0x34: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0x37: this.ZeroPageXRead(); this.RLA(); ; break;                                                   // *RLA (zero page, X)
                case 0x3a: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0x3b: this.AbsoluteYAddress(); this.FixupRead(); this.RLA(); break;                                // *RLA (absolute, Y)
                case 0x3c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0x3f: this.AbsoluteXAddress(); this.FixupRead(); this.RLA(); break;                                // *RLA (absolute, X)

                case 0x42: this.Jam(); break;                                                                           // *JAM
                case 0x43: this.IndexedIndirectXRead(); this.SRE(); break;                                              // *SRE (indexed indirect X)
                case 0x44: this.ZeroPageRead(); break;                                                                  // *NOP (zero page)
                case 0x47: this.ZeroPageRead(); this.SRE(); break;                                                      // *SRE (zero page)
                case 0x4b: this.ImmediateRead(); this.ASR(); break;                                                     // *ASR (immediate)
                case 0x4f: this.AbsoluteRead(); this.SRE(); break;                                                      // *SRE (absolute)

                case 0x52: this.Jam(); break;                                                                           // *JAM
                case 0x53: this.IndirectIndexedYAddress(); this.FixupRead(); this.SRE(); break;                         // *SRE (indirect indexed Y)
                case 0x54: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0x57: this.ZeroPageXRead(); this.SRE(); break;                                                     // *SRE (zero page, X)
                case 0x5a: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0x5b: this.AbsoluteYAddress(); this.FixupRead(); this.SRE(); break;                                // *SRE (absolute, Y)
                case 0x5c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0x5f: this.AbsoluteXAddress(); this.FixupRead(); this.SRE(); break;                                // *SRE (absolute, X)

                case 0x62: this.Jam(); break;                                                                           // *JAM
                case 0x63: this.IndexedIndirectXRead(); this.RRA(); break;                                              // *RRA (indexed indirect X)
                case 0x64: this.ZeroPageRead(); break;                                                                  // *NOP (zero page)
                case 0x67: this.ZeroPageRead(); this.RRA(); break;                                                      // *RRA (zero page)
                case 0x6b: this.ImmediateRead(); this.ARR(); break;                                                     // *ARR (immediate)
                case 0x6f: this.AbsoluteRead(); this.RRA(); break;                                                      // *RRA (absolute)

                case 0x72: this.Jam(); break;                                                                           // *JAM
                case 0x73: this.IndirectIndexedYAddress(); this.FixupRead(); this.RRA(); break;                         // *RRA (indirect indexed Y)
                case 0x74: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0x77: this.ZeroPageXRead(); this.RRA(); break;                                                     // *RRA (zero page, X)
                case 0x7a: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0x7b: this.AbsoluteYAddress(); this.FixupRead(); this.RRA(); break;                                // *RRA (absolute, Y)
                case 0x7c: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0x7f: this.AbsoluteXAddress(); this.FixupRead(); this.RRA(); break;                                // *RRA (absolute, X)

                case 0x80: this.ImmediateRead(); break;                                                                 // *NOP (immediate)
                case 0x82: this.ImmediateRead(); break;                                                                 // *NOP (immediate)
                case 0x83: this.IndexedIndirectXAddress(); this.MemoryWrite((byte)(this.A & this.X)); break;            // *SAX (indexed indirect X)
                case 0x87: this.ZeroPageAddress(); this.MemoryWrite((byte)(this.A & this.X)); break;	                // *SAX (zero page)
                case 0x89: this.ImmediateRead(); break;	                                                                // *NOP (immediate)
                case 0x8b: this.ImmediateRead(); this.ANE(); break;	                                                    // *ANE (immediate)
                case 0x8f: this.AbsoluteAddress(); this.MemoryWrite((byte)(this.A & this.X)); break;	                // *SAX (absolute)

                case 0x92: this.Jam(); break;                                                                           // *JAM
                case 0x93: this.IndirectIndexedYAddress(); this.Fixup(); this.SHA(); break;                             // *SHA (indirect indexed, Y)
                case 0x97: this.ZeroPageYAddress(); this.MemoryWrite((byte)(this.A & this.X)); break;                   // *SAX (zero page, Y)
                case 0x9b: this.AbsoluteYAddress(); this.Fixup(); this.TAS(); break;                                    // *TAS (absolute, Y)
                case 0x9c: this.AbsoluteXAddress(); this.Fixup(); this.SYA(); break;                                    // *SYA (absolute, X)
                case 0x9e: this.AbsoluteYAddress(); this.Fixup(); this.SXA(); break;                                    // *SXA (absolute, Y)
                case 0x9f: this.AbsoluteYAddress(); this.Fixup(); this.SHA(); break;                                    // *SHA (absolute, Y)

                case 0xa3: this.IndexedIndirectXRead(); this.A = this.X = this.Through(); break;                        // *LAX (indexed indirect X)
                case 0xa7: this.ZeroPageRead(); this.A = this.X = this.Through(); break;                                // *LAX (zero page)
                case 0xab: this.ImmediateRead(); this.ATX(); break;                                                     // *ATX (immediate)
                case 0xaf: this.AbsoluteRead(); this.A = this.X = this.Through(); break;                                // *LAX (absolute)

                case 0xb2: this.Jam(); break;                                                                           // *JAM
                case 0xb3: this.IndirectIndexedYRead(); this.A = this.X = this.Through(); break;                        // *LAX (indirect indexed Y)
                case 0xb7: this.ZeroPageYRead(); this.A = this.X = this.Through(); break;                               // *LAX (zero page, Y)
                case 0xbb: this.AbsoluteYAddress(); this.MaybeFixup(); this.LAS(); break;                               // *LAS (absolute, Y)
                case 0xbf: this.AbsoluteYRead(); this.A = this.X = this.Through(); break;                               // *LAX (absolute, Y)

                case 0xc2: this.ImmediateRead(); break;                                                                 // *NOP (immediate)
                case 0xc3: this.IndexedIndirectXRead(); this.DCP(); break;                                              // *DCP (indexed indirect X)
                case 0xc7: this.ZeroPageRead(); this.DCP(); break;                                                      // *DCP (zero page)
                case 0xcb: this.ImmediateRead(); this.AXS(); break;                                                     // *AXS (immediate)
                case 0xcf: this.AbsoluteRead(); this.DCP(); break;                                                      // *DCP (absolute)

                case 0xd2: this.Jam(); break;                                                                           // *JAM
                case 0xd3: this.IndirectIndexedYAddress(); this.FixupRead(); this.DCP(); break;                         // *DCP (indirect indexed Y)
                case 0xd4: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0xd7: this.ZeroPageXRead(); this.DCP(); break;                                                     // *DCP (zero page, X)
                case 0xda: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0xdb: this.AbsoluteYAddress(); this.FixupRead(); this.DCP(); break;                                // *DCP (absolute, Y)
                case 0xdc: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0xdf: this.AbsoluteXAddress(); this.FixupRead(); this.DCP(); break;                                // *DCP (absolute, X)

                case 0xe2: this.ImmediateRead(); break;                                                                 // *NOP (immediate)
                case 0xe3: this.IndexedIndirectXRead(); this.ISB(); break;                                              // *ISB (indexed indirect X)
                case 0xe7: this.ZeroPageRead(); this.ISB(); break;                                                      // *ISB (zero page)
                case 0xeb: this.ImmediateRead(); this.SBC(); break;                                                     // *SBC (immediate)
                case 0xef: this.AbsoluteRead(); this.ISB(); break;                                                      // *ISB (absolute)

                case 0xf2: this.Jam(); break;                                                                           // *JAM
                case 0xf3: this.IndirectIndexedYAddress(); this.FixupRead(); this.ISB(); break;                         // *ISB (indirect indexed Y)
                case 0xf4: this.ZeroPageXRead(); break;                                                                 // *NOP (zero page, X)
                case 0xf7: this.ZeroPageXRead(); this.ISB(); break;                                                     // *ISB (zero page, X)
                case 0xfa: this.SwallowRead(); break;                                                                       // *NOP (implied)
                case 0xfb: this.AbsoluteYAddress(); this.FixupRead(); this.ISB(); break;                                // *ISB (absolute, Y)
                case 0xfc: this.AbsoluteXAddress(); this.MaybeFixupRead(); break;                                       // *NOP (absolute, X)
                case 0xff: this.AbsoluteXAddress(); this.FixupRead(); this.ISB(); break;	                            // *ISB (absolute, X)
            }

            return cycles != this.Cycles;
        }

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

        #endregion

        #region Undocumented instructions with fixup effects

        private void StoreFixupEffect(byte data)
        {
            var fixedAddress = (byte)(this.Bus.Address.High + 1);
            this.MemoryWrite((byte)(data & fixedAddress));
        }

        private void SHA() => this.StoreFixupEffect((byte)(this.A & this.X));

        private void SYA() => this.StoreFixupEffect(this.Y);

        private void SXA() => this.StoreFixupEffect(this.X);

        #endregion

        private void ANC()
        {
            this.AndR();
            this.SetFlag(StatusBits.CF, NegativeTest(this.A));
        }

        private void AXS()
        {
            this.X = this.Through(this.BinarySUB((byte)(this.A & this.X)));
            this.ResetFlag(StatusBits.CF, this.Intermediate.High);
        }

        private void Jam()
        {
            this.Bus.Address.Word = this.PC.Word--;
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

        #endregion

        #endregion
    }
}