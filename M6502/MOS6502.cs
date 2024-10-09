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

            var cycles = Cycles;
            switch (OpCode)
            {
                case 0x02: Jam(); break;                                                    // *JAM
                case 0x03: IndexedIndirectXRead(); SLO(); break;                            // *SLO (indexed indirect X)
                case 0x04: ZeroPageRead(); break;                                           // *NOP (zero page)
                case 0x07: ZeroPageRead(); SLO(); break;                                    // *SLO (zero page)
                case 0x0b: ImmediateRead(); ANC(); break;                                   // *ANC (immediate)
                case 0x0c: AbsoluteRead(); break;                                           // *NOP (absolute)
                case 0x0f: AbsoluteRead(); SLO(); break;                                    // *SLO (absolute)

                case 0x12: Jam(); break;                                                    // *JAM
                case 0x13: IndirectIndexedYAddress(); FixupRead(); SLO(); break;            // *SLO (indirect indexed Y)
                case 0x14: ZeroPageXRead(); break;                                          // *NOP (zero page, X)
                case 0x17: ZeroPageXRead(); SLO(); break;                                   // *SLO (zero page, X)
                case 0x1a: SwallowRead(); break;                                            // *NOP (implied)
                case 0x1b: AbsoluteYAddress(); FixupRead(); SLO(); break;                   // *SLO (absolute, Y)
                case 0x1c: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0x1f: AbsoluteXAddress(); FixupRead(); SLO(); break;                   // *SLO (absolute, X)

                case 0x22: Jam(); break;                                                    // *JAM
                case 0x23: IndexedIndirectXRead(); RLA(); ; break;                          // *RLA (indexed indirect X)
                case 0x27: ZeroPageRead(); RLA(); ; break;                                  // *RLA (zero page)
                case 0x2b: ImmediateRead(); ANC(); break;                                   // *ANC (immediate)
                case 0x2f: AbsoluteRead(); RLA(); break;                                    // *RLA (absolute)

                case 0x32: Jam(); break;													// *JAM
                case 0x33: IndirectIndexedYAddress(); FixupRead(); RLA(); break;            // *RLA (indirect indexed Y)
                case 0x34: ZeroPageXRead(); break;                                          // *NOP (zero page, X)
                case 0x37: ZeroPageXRead(); RLA(); ; break;                                 // *RLA (zero page, X)
                case 0x3a: SwallowRead(); break;                                            // *NOP (implied)
                case 0x3b: AbsoluteYAddress(); FixupRead(); RLA(); break;                   // *RLA (absolute, Y)
                case 0x3c: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0x3f: AbsoluteXAddress(); FixupRead(); RLA(); break;                   // *RLA (absolute, X)

                case 0x42: Jam(); break;                                                    // *JAM
                case 0x43: IndexedIndirectXRead(); SRE(); break;                            // *SRE (indexed indirect X)
                case 0x47: ZeroPageRead(); SRE(); break;                                    // *SRE (zero page)
                case 0x4b: ImmediateRead(); ASR(); break;                                   // *ASR (immediate)
                case 0x4f: AbsoluteRead(); SRE(); break;                                    // *SRE (absolute)

                case 0x52: Jam(); break;                                                    // *JAM
                case 0x53: IndirectIndexedYAddress(); FixupRead(); SRE(); break;            // *SRE (indirect indexed Y)
                case 0x57: ZeroPageXRead(); SRE(); break;                                   // *SRE (zero page, X)
                case 0x5a: SwallowRead(); break;                                            // *NOP (implied)
                case 0x5b: AbsoluteYAddress(); FixupRead(); SRE(); break;                   // *SRE (absolute, Y)
                case 0x5c: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0x5f: AbsoluteXAddress(); FixupRead(); SRE(); break;                   // *SRE (absolute, X)

                case 0x62: Jam(); break;                                                    // *JAM
                case 0x63: IndexedIndirectXRead(); RRA(); break;                            // *RRA (indexed indirect X)
                case 0x64: ZeroPageRead(); break;                                           // *NOP (zero page)
                case 0x67: ZeroPageRead(); RRA(); break;                                    // *RRA (zero page)
                case 0x6b: ImmediateRead(); ARR(); break;                                   // *ARR (immediate)
                case 0x6f: AbsoluteRead(); RRA(); break;                                    // *RRA (absolute)

                case 0x72: Jam(); break;                                                    // *JAM
                case 0x73: IndirectIndexedYAddress(); FixupRead(); RRA(); break;            // *RRA (indirect indexed Y)
                case 0x74: ZeroPageXRead(); break;                                          // *NOP (zero page, X)
                case 0x77: ZeroPageXRead(); RRA(); break;                                   // *RRA (zero page, X)
                case 0x7a: SwallowRead(); break;                                            // *NOP (implied)
                case 0x7b: AbsoluteYAddress(); FixupRead(); RRA(); break;                   // *RRA (absolute, Y)
                case 0x7c: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0x7f: AbsoluteXAddress(); FixupRead(); RRA(); break;                   // *RRA (absolute, X)

                case 0x80: ImmediateRead(); break;                                          // *NOP (immediate)
                case 0x83: IndexedIndirectXAddress(); MemoryWrite((byte)(A & X)); break;    // *SAX (indexed indirect X)
                case 0x87: ZeroPageAddress(); MemoryWrite((byte)(A & X)); break;	        // *SAX (zero page)
                case 0x89: ImmediateRead(); break;	                                        // *NOP (immediate)
                case 0x8b: ImmediateRead(); ANE(); break;	                                // *ANE (immediate)
                case 0x8f: AbsoluteAddress(); MemoryWrite((byte)(A & X)); break;	        // *SAX (absolute)

                case 0x92: Jam(); break;                                                    // *JAM
                case 0x93: IndirectIndexedYAddress(); Fixup(); SHA(); break;                // *SHA (indirect indexed, Y)
                case 0x97: ZeroPageYAddress(); MemoryWrite((byte)(A & X)); break;           // *SAX (zero page, Y)
                case 0x9b: AbsoluteYAddress(); Fixup(); TAS(); break;                       // *TAS (absolute, Y)
                case 0x9c: AbsoluteXAddress(); Fixup(); SYA(); break;                       // *SYA (absolute, X)
                case 0x9e: AbsoluteYAddress(); Fixup(); SXA(); break;                       // *SXA (absolute, Y)
                case 0x9f: AbsoluteYAddress(); Fixup(); SHA(); break;                       // *SHA (absolute, Y)

                case 0xa3: IndexedIndirectXRead(); A = X = Through(); break;                // *LAX (indexed indirect X)
                case 0xa7: ZeroPageRead(); A = X = Through(); break;                        // *LAX (zero page)
                case 0xab: ImmediateRead(); ATX(); break;                                   // *ATX (immediate)
                case 0xaf: AbsoluteRead(); A = X = Through(); break;                        // *LAX (absolute)

                case 0xb2: Jam(); break;                                                    // *JAM
                case 0xb3: IndirectIndexedYRead(); A = X = Through(); break;                // *LAX (indirect indexed Y)
                case 0xb7: ZeroPageYRead(); A = X = Through(); break;                       // *LAX (zero page, Y)
                case 0xbb: AbsoluteYAddress(); MaybeFixup(); LAS(); break;                  // *LAS (absolute, Y)
                case 0xbf: AbsoluteYRead(); A = X = Through(); break;                       // *LAX (absolute, Y)

                case 0xc3: IndexedIndirectXRead(); DCP(); break;                            // *DCP (indexed indirect X)
                case 0xc7: ZeroPageRead(); DCP(); break;                                    // *DCP (zero page)
                case 0xcb: ImmediateRead(); AXS(); break;                                   // *AXS (immediate)
                case 0xcf: AbsoluteRead(); DCP(); break;                                    // *DCP (absolute)

                case 0xd2: Jam(); break;                                                    // *JAM
                case 0xd3: IndirectIndexedYAddress(); FixupRead(); DCP(); break;            // *DCP (indirect indexed Y)
                case 0xd7: ZeroPageXRead(); DCP(); break;                                   // *DCP (zero page, X)
                case 0xda: SwallowRead(); break;                                            // *NOP (implied)
                case 0xdb: AbsoluteYAddress(); FixupRead(); DCP(); break;                   // *DCP (absolute, Y)
                case 0xdc: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0xdf: AbsoluteXAddress(); FixupRead(); DCP(); break;                   // *DCP (absolute, X)

                case 0xe3: IndexedIndirectXRead(); ISB(); break;                            // *ISB (indexed indirect X)
                case 0xe7: ZeroPageRead(); ISB(); break;                                    // *ISB (zero page)
                case 0xeb: ImmediateRead(); SBC(); break;                                   // *SBC (immediate)
                case 0xef: AbsoluteRead(); ISB(); break;                                    // *ISB (absolute)

                case 0xf2: Jam(); break;                                                    // *JAM
                case 0xf3: IndirectIndexedYAddress(); FixupRead(); ISB(); break;            // *ISB (indirect indexed Y)
                case 0xf7: ZeroPageXRead(); ISB(); break;                                   // *ISB (zero page, X)
                case 0xfa: SwallowRead(); break;                                            // *NOP (implied)
                case 0xfb: AbsoluteYAddress(); FixupRead(); ISB(); break;                   // *ISB (absolute, Y)
                case 0xfc: AbsoluteXAddress(); MaybeFixupRead(); break;                     // *NOP (absolute, X)
                case 0xff: AbsoluteXAddress(); FixupRead(); ISB(); break;	                // *ISB (absolute, X)
            }

            return cycles != Cycles;
        }

        #endregion

        #region Bus/Memory Access

        protected override void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            MemoryWrite();     // Modify cycle
            MemoryWrite(data); // Write cycle
        }

        #endregion

        #region Addressing modes

        protected override void IndirectAddress()
        {
            AbsoluteAddress();
            GetAddressPaged();
        }

        #region Address page fixup

        protected override void Fixup()
        {
            MemoryRead();
            Bus.Address.High = FixedPage;
        }

        protected override void FixupBranch(sbyte relative)
        {
            NoteFixedAddress(PC.Word + relative);
            MaybeFixup();
        }

        #endregion

        #endregion

        #region Instruction implementations

        #region Undocumented instructions

        #region Undocumented instructions with BCD effects

        private void ARR()
        {
            var value = Bus.Data;
            if (DecimalMasked != 0)
                ARR_d(value);
            else
                ARR_b(value);
        }

        private void ARR_d(byte value)
        {
            // With thanks to https://github.com/TomHarte/CLK
            // What a very strange instruction ARR is...

            A &= value;
            var unshiftedA = A;
            A = Through(A >> 1 | Carry << 7);
            SetFlag(StatusBits.VF, OverflowTest((byte)(A ^ A << 1)));

            if (LowerNibble(unshiftedA) + (unshiftedA & 0x1) > 5)
                A = (byte)(LowerNibble((byte)(A + 6)) | HigherNibble(A));

            SetFlag(StatusBits.CF, HigherNibble(unshiftedA) + (unshiftedA & 0x10) > 0x50);

            if (Carry != 0)
                A += 0x60;
        }

        private void ARR_b(byte value)
        {
            A &= value;
            A = Through(A >> 1 | Carry << 7);
            SetFlag(StatusBits.CF, OverflowTest(A));
            SetFlag(StatusBits.VF, OverflowTest((byte)(A ^ A << 1)));
        }

        #endregion

        #region Undocumented instructions with fixup effects

        private void StoreFixupEffect(byte data)
        {
            ////var fixedAddress = (byte)(this.Bus.Address.High + 1);
            //var fixedAddress = this.FixedPage + 1;
            //var updated = (byte)(data & fixedAddress);
            //if (this.Fixed)
            //{
            //    this.Bus.Address.High = updated;
            //}

            //this.MemoryWrite(updated);

            byte updated;
            if (Fixed)
            {
                updated = (byte)(data & FixedPage);
                Bus.Address.High = updated;
            }
            else
            {
                updated = (byte)(data & UnfixedPage);
                Bus.Address.High = updated;
            }
            MemoryWrite(updated);
        }

        private void SHA() => StoreFixupEffect((byte)(A & X));

        private void SYA() => StoreFixupEffect(Y);

        private void SXA() => StoreFixupEffect(X);

        #endregion

        private void ANC()
        {
            AndR();
            SetFlag(StatusBits.CF, NegativeTest(A));
        }

        private void AXS()
        {
            X = Through(BinarySUB((byte)(A & X)));
            ResetFlag(StatusBits.CF, Intermediate.High);
        }

        private void Jam()
        {
            Bus.Address.Assign(PC);
            MemoryRead();
            MemoryRead(0xff, 0xff);
            Bus.Address.Low = 0xfe;
            MemoryRead();
            MemoryRead();
            Bus.Address.Low = 0xff;
            MemoryRead();
            MemoryRead();
            MemoryRead();
            MemoryRead();
            MemoryRead();
            MemoryRead();
        }

        private void TAS()
        {
            S = (byte)(A & X);
            SHA();
        }

        private void LAS() => A = X = S = Through(MemoryRead() & S);

        private void ANE() => A = Through((A | 0xee) & X & Bus.Data);

        private void ATX() => A = X = Through((A | 0xee) & Bus.Data);

        private void ASR()
        {
            AndR();
            A = LSR(A);
        }

        private void ISB()
        {
            ModifyWrite(INC());
            SBC();
        }

        private void RLA()
        {
            ModifyWrite(ROL());
            AndR();
        }

        private void RRA()
        {
            ModifyWrite(ROR());
            ADC();
        }

        private void SLO()
        {
            ModifyWrite(ASL());
            OrR();
        }

        private void SRE()
        {
            ModifyWrite(LSR());
            EorR();
        }

        private void DCP()
        {
            ModifyWrite(DEC());
            CMP(A);
        }

        #endregion

        #endregion
    }
}