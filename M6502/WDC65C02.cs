// <copyright file="WDC65C02.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502
{
    using EightBit;

    public class WDC65C02(Bus bus) : Core(bus)
    {
        private bool _stopped;
        private bool _waiting;

        private bool Stopped
        {
            get => _stopped; set => _stopped = value;
        }

        private bool Waiting
        {
            get => _waiting; set => _waiting = value;
        }

        private bool Paused => Stopped || Waiting;

        #region Interrupts

        protected override void Interrupt(byte vector, InterruptSource source, InterruptType type)
        {
            base.Interrupt(vector, source, type);
            ResetFlag(StatusBits.DF);  // Disable decimal mode (Change from MOS6502)
        }

        #endregion

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
                case 0x02: SwallowFetch(); break;                                                         // NOP
                case 0x03: break;                                                                               // null
                case 0x04: ZeroPageRead(); TSB(); break;                                              // TSB zp
                case 0x07: ZeroPageRead(); RMB(Bit(0)); break;                                   // RMB0 zp
                case 0x0b: break;                                                                               // null
                case 0x0c: AbsoluteRead(); TSB(); break;                                              // TSB a
                case 0x0f: ZeroPageRead(); BBR(Bit(0)); break;                                   // BBR0 r

                case 0x12: ZeroPageIndirectAddress(); OrR(); break;                                   // ORA (zp),y
                case 0x13: break;                                                                               // null
                case 0x14: ZeroPageRead(); TRB(); break;                                              // TRB zp
                case 0x17: ZeroPageRead(); RMB(Bit(1)); break;                                   // RMB1 zp
                case 0x1a: SwallowRead(); A = INC(A); break;                                // INC A
                case 0x1b: break;                                                                               // null
                case 0x1c: AbsoluteRead(); TRB(); break;                                              // TRB a
                case 0x1f: ZeroPageRead(); BBR(Bit(1)); break;                                   // BBR1 r

                case 0x22: SwallowFetch(); break;                                                          // NOP
                case 0x23: break;                                                                               // null
                case 0x27: ZeroPageRead(); RMB(Bit(2)); break;                                   // RMB2 zp
                case 0x2b: break;                                                                               // null
                case 0x2f: ZeroPageRead(); BBR(Bit(2)); break;                                   // BBR2 r

                case 0x32: ZeroPageIndirectRead(); AndR(); break;                                     // AND (zp)
                case 0x33: break;                                                                               // null
                case 0x34: break;                                                                               // BIT zp,x
                case 0x37: ZeroPageRead(); RMB(Bit(3)); break;                                   // RMB3 zp
                case 0x3a: SwallowRead(); A = DEC(A); break;                                // DEC A
                case 0x3b: break;                                                                               // null
                case 0x3c: break;                                                                               // BIT a,x
                case 0x3f: ZeroPageRead(); BBR(Bit(3)); break;                                   // BBR3 r

                case 0x42: SwallowFetch(); break;                                                          // NOP
                case 0x43: break;                                                                               // null
                case 0x47: ZeroPageRead(); RMB(Bit(4)); break;                                   // RMB4 zp
                case 0x4b: break;                                                                               // null
                case 0x4f: ZeroPageRead(); BBR(Bit(4)); break;                                   // BBR4 r

                case 0x52: ZeroPageIndirectRead(); EorR(); break;                                     // EOR (zp)
                case 0x53: break;                                                                               // null
                case 0x57: ZeroPageRead(); RMB(Bit(5)); break;                                   // RMB5 zp
                case 0x5a: SwallowRead(); Push(Y); break;                                        // PHY s
                case 0x5b: break;                                                                               // null
                case 0x5c: break;                                                                               // null
                case 0x5f: ZeroPageRead(); BBR(Bit(5)); break;                                   // BBR5 r

                case 0x62: SwallowFetch(); break;                                                          // *NOP
                case 0x63: break;                                                                               // null
                case 0x64: ZeroPageAddress(); MemoryWrite(0); break;                                  // STZ zp
                case 0x67: ZeroPageRead(); RMB(Bit(6)); break;                                   // RMB6 zp
                case 0x6b: break;                                                                               // null
                case 0x6f: ZeroPageRead(); BBR(Bit(6)); break;                                   // BBR6 r

                case 0x72: ZeroPageIndirectRead(); ADC(); break;                                      // ADC (zp)
                case 0x73: break;                                                                               // null
                case 0x74: ZeroPageXAddress(); MemoryWrite(0); break;                                 // STZ zp,x
                case 0x77: ZeroPageRead(); RMB(Bit(7)); break;                                   // RMB7 zp
                case 0x7a: SwallowRead(); SwallowPop(); Y = Through(Pop()); break;     // PLY s
                case 0x7b: break;                                                                               // null
                case 0x7c: break;                                                                               // JMP (a,x)
                case 0x7f: ZeroPageRead(); BBR(Bit(7)); break;                                   // BBR7 r

                case 0x80: Branch(true); break;                                                                 // BRA r
                case 0x83: break;                                                                               // null
                case 0x87: ZeroPageRead(); SMB(Bit(0)); break;                                   // SMB0 zp
                case 0x89: break;                                                                               // BIT # (TBC)
                case 0x8b: break;                                                                               // null
                case 0x8f: ZeroPageRead(); BBS(Bit(0)); break;                                   // BBS0 r

                case 0x92: ZeroPageIndirectAddress(); MemoryWrite(A); break;                          // STA (zp)
                case 0x93: break;                                                                               // null
                case 0x97: ZeroPageRead(); SMB(Bit(1)); break;                                   // SMB1 zp
                case 0x9b: break;                                                                               // null
                case 0x9c: AbsoluteAddress(); MemoryWrite(0); break;                                  // STZ a
                case 0x9e: AbsoluteXAddress(); MemoryWrite(0); break;                                 // STZ a,x
                case 0x9f: ZeroPageRead(); BBS(Bit(1)); break;                                   // BBS1 r

                case 0xa3: break;                                                                               // null
                case 0xa7: ZeroPageRead(); SMB(Bit(2)); break;                                   // SMB2 zp
                case 0xab: break;                                                                               // null
                case 0xaf: ZeroPageRead(); BBS(Bit(2)); break;                                   // BBS2 r

                case 0xb2: ZeroPageIndirectRead(); A = Through(); break;                              // LDA (zp)
                case 0xb3: break;                                                                               // null
                case 0xb7: ZeroPageRead(); SMB(Bit(3)); break;                                   // SMB3 zp
                case 0xbb: break;                                                                               // null
                case 0xbf: ZeroPageRead(); BBS(Bit(3)); break;                                   // BBS3 r

                case 0xc3: break;                                                                               // null
                case 0xc7: ZeroPageRead(); SMB(Bit(4)); break;                                   // SMB4 zp
                case 0xcb: SwallowRead(); Waiting = true; break;                                      // WAI i
                case 0xcf: ZeroPageRead(); BBS(Bit(4)); break;                                   // BBS4 r

                case 0xd2: ZeroPageIndirectRead(); CMP(A); break;                                // CMP (zp)
                case 0xd3: break;                                                                               // null
                case 0xd7: ZeroPageRead(); SMB(Bit(5)); break;                                   // SMB5 zp
                case 0xda: SwallowRead(); Push(X); break;                                        // PHX s
                case 0xdb: SwallowRead(); Stopped = true; break;                                      // STP i
                case 0xdc: SwallowRead(); break;                                                                               // null
                case 0xdf: ZeroPageRead(); BBS(Bit(5)); break;                                   // BBS5 r

                case 0xe3: break;                                                                               // null
                case 0xe7: ZeroPageRead(); SMB(Bit(6)); break;                                   // SMB6 zp
                case 0xeb: break;                                                                               // null
                case 0xef: ZeroPageRead(); BBS(Bit(6)); break;                                   // BBS6 r

                case 0xf2: ZeroPageIndirectRead(); SBC(); break;                                      // SBC (zp)
                case 0xf3: break;                                                                               // null
                case 0xf7: ZeroPageRead(); SMB(Bit(7)); break;                                   // SMB7 zp
                case 0xfa: SwallowRead(); SwallowPop(); X = Through(Pop()); break;     // PLX s
                case 0xfb: break;                                                                               // null
                case 0xfc: break;                                                                               // null
                case 0xff: ZeroPageRead(); BBS(Bit(7)); break;                                   // BBS7 r
            }

            return cycles != Cycles;
        }

        public override void PoweredStep()
        {
            if (!Paused)
            {
                base.PoweredStep();
            }
        }

        protected override void OnLoweredRESET()
        {
            base.OnLoweredRESET();
            Stopped = Waiting = false;
        }

        protected override void OnLoweredINT()
        {
            base.OnLoweredINT();
            Waiting = false;
        }

        protected override void OnLoweredNMI()
        {
            base.OnLoweredNMI();
            Waiting = false;
        }

        #endregion

        #region Bus/Memory Access

        protected override void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            MemoryRead();      // Modify cycle (Change from MOS6502)
            MemoryWrite(data); // Write cycle
        }

        #endregion

        #region Addressing modes

        #region Address page fixup

        private readonly Register16 lastFetchAddress = new();

        protected override byte FetchByte()
        {
            lastFetchAddress.Assign(PC);
            return base.FetchByte();
        }

        protected override void Fixup()
        {
            var fixingLow = Bus.Address.Low;
            MemoryRead(lastFetchAddress);
            Bus.Address.Assign(fixingLow, FixedPage);
        }

        protected override void FixupBranch(sbyte relative)
        {
            NoteFixedAddress(PC.Word + relative);
            lastFetchAddress.Assign(Bus.Address);    // Effectively negate the use of "lastFetchAddress" for branch fixup usages
            MaybeFixup();
        }

        #endregion

        #region Address resolution

        protected void GetAddress()
        {
            GetWordPaged();

            if (Bus.Address.Low == 0)
            {
                Bus.Address.High++;
            }

            Bus.Address.Assign(Intermediate.Low, MemoryRead());
        }

        protected override void IndirectAddress()
        {
            AbsoluteAddress();
            GetAddress();
        }

        #endregion

        #region Address and read

        private void ZeroPageIndirectRead()
        {
            ZeroPageIndirectAddress();
            MemoryRead();
        }

        #endregion

        #endregion

        private void RMB(byte flag)
        {
            MemoryRead();
            Bus.Data &= (byte)~flag;
            MemoryWrite();
        }

        private void SMB(byte flag)
        {
            MemoryRead();
            Bus.Data |= flag;
            MemoryWrite();
        }

        private void BBS(byte flag)
        {
            MemoryRead();
            Branch(Bus.Data & flag);
        }

        private void BBR(byte flag)
        {
            MemoryRead();
            BranchNot(Bus.Data & flag);
        }

        private void TSB()
        {
            AdjustZero((byte)(A & Bus.Data));
            ModifyWrite((byte)(A | Bus.Data));
        }

        private void TRB()
        {
            AdjustZero((byte)(A & Bus.Data));
            ModifyWrite((byte)(~A & Bus.Data));
        }
    }
}