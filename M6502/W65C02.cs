// <copyright file="W65C02.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class W65C02(Bus bus) : M6502Core(bus)
    {
        private bool _stopped = false;
        private bool _waiting = false;

        private bool Stopped
        {
            get => this._stopped; set => this._stopped = value;
        }

        private bool Waiting
        {
            get => this._waiting; set => this._waiting = value;
        }

        private bool Paused => this.Stopped || this.Waiting;

        #region Interrupts

        protected override void Interrupt(byte vector, InterruptSource source, InterruptType type)
        {
            base.Interrupt(vector, source, type);
            this.ResetFlag(StatusBits.DF);  // Disable decimal mode (Change from M6502)
        }

        #endregion

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
                case 0x02: this.SwallowFetch();  break;                                                         // *NOP
                case 0x03: break;                           // null
                case 0x04: break;                           // TSB zp
                case 0x07: break;                           // RMB0 zp
                case 0x0b: break;                           // null
                case 0x0c: break;                           // TSB a
                case 0x0f: break;                           // BBR0 r

                case 0x12: this.ZeroPageIndirectAddress(); this.OrR(); break;                                   // ORA (zp),y
                case 0x13: break;                           // null
                case 0x14: break;                           // TRB zp
                case 0x17: break;                           // RMB1 zp
                case 0x1a: this.SwallowRead(); this.A = this.INC(this.A); break;                                // INC A
                case 0x1b: break;                           // null
                case 0x1c: break;                           // TRB a
                case 0x1f: break;                           // BBR1 r

                case 0x22: this.SwallowFetch(); break;                                                          // *NOP
                case 0x23: break;                           // null
                case 0x27: break;                           // RMB2 zp
                case 0x2b: break;                           // null
                case 0x2f: break;                           // BBR2 r

                case 0x32: this.ZeroPageIndirectRead(); this.AndR(); break;                           // AND (zp)
                case 0x33: break;                           // null
                case 0x34: break;                           // BIT zp,x
                case 0x37: break;                           // RMB3 zp
                case 0x3a: this.SwallowRead(); this.A = this.DEC(this.A); break;                                // DEC A
                case 0x3b: break;                           // null
                case 0x3c: break;                           // BIT a,x
                case 0x3f: break;                           // BBR3 r

                case 0x42: this.SwallowFetch(); break;                                                          // *NOP
                case 0x43: break;                           // null
                case 0x47: break;                           // RMB4 zp
                case 0x4b: break;                           // null
                case 0x4f: break;                           // BBR4 r

                case 0x52: this.ZeroPageIndirectRead(); this.EorR(); break;                           // EOR (zp)
                case 0x53: break;                           // null
                case 0x57: break;                           // RMB5 zp
                case 0x5a: this.SwallowRead(); this.Push(this.Y); break;                                        // PHY s
                case 0x5b: break;                           // null
                case 0x5c: break;                           // null
                case 0x5f: break;                           // BBR5 r

                case 0x62: this.SwallowFetch(); break;                                                          // *NOP
                case 0x63: break;                           // null
                case 0x64: this.ZeroPageAddress(); this.MemoryWrite(0); break;                                  // STZ zp
                case 0x67: break;                           // RMB6 zp
                case 0x6b: break;                           // null
                case 0x6f: break;                           // BBR6 r

                case 0x72: this.ZeroPageIndirectRead(); this.ADC(); break;                           // ADC (zp)
                case 0x73: break;                           // null
                case 0x74: this.ZeroPageXAddress(); this.MemoryWrite(0); break;                                 // STZ zp,x
                case 0x77: break;                           // RMB7 zp
                case 0x7a: this.SwallowRead(); this.SwallowPop(); this.Y = this.Through(this.Pop()); break;     // PLY s
                case 0x7b: break;                           // null
                case 0x7c: break;                           // JMP (a,x)
                case 0x7f: break;                           // BBR7 r

                case 0x80: break;                           // BRA r
                case 0x83: break;                           // null
                case 0x87: break;                           // SMB0 zp
                case 0x89: break;                           // BIT # (TBC)
                case 0x8b: break;                           // null
                case 0x8f: break;                           // BBS0 r

                case 0x92: ZeroPageIndirectAddress(); this.MemoryWrite(this.A); break;                           // STA (zp)
                case 0x93: break;                           // null
                case 0x97: break;                           // SMB1 zp
                case 0x9b: break;                           // null
                case 0x9c: this.AbsoluteAddress(); this.MemoryWrite(0); break;                                  // STZ a
                case 0x9e: this.AbsoluteXAddress(); this.MemoryWrite(0); break;                                 // STZ a,x
                case 0x9f: break;                           // BBS1 r

                case 0xa3: break;                           // null
                case 0xa7: break;                           // SMB2 zp
                case 0xab: break;                           // null
                case 0xaf: break;                           // BBS2 r

                case 0xb2: ZeroPageIndirectRead(); this.A = this.Through(); break;                           // LDA (zp)
                case 0xb3: break;                           // null
                case 0xb7: break;                           // SMB3 zp
                case 0xbb: break;                           // null
                case 0xbf: break;                           // BBS3 r

                case 0xc3: break;                           // null
                case 0xc7: break;                           // SMB4 zp
                case 0xcb: this.SwallowRead(); this.Waiting = true; break;                           // WAI i
                case 0xcf: break;                           // BBS4 r

                case 0xd2: this.ZeroPageIndirectRead(); this.CMP(this.A); break;                           // CMP (zp)
                case 0xd3: break;                           // null
                case 0xd7: break;                           // SMB5 zp
                case 0xda: this.SwallowRead(); this.Push(this.X); break;                                        // PHX s
                case 0xdb: this.SwallowRead(); this.Stopped = true; break;                           // STP i
                case 0xdc: break;                           // null
                case 0xdf: break;                           // BBS5 r

                case 0xe3: break;                           // null
                case 0xe7: break;                           // SMB6 zp
                case 0xeb: break;                           // null
                case 0xef: break;                           // BBS6 r

                case 0xf2: this.ZeroPageIndirectRead(); this.SBC(); break;                           // SBC (zp)
                case 0xf3: break;                           // null
                case 0xf7: break;                           // SMB7 zp
                case 0xfa: this.SwallowRead(); this.SwallowPop(); this.X = this.Through(this.Pop()); break;     // PLX s
                case 0xfb: break;                           // null
                case 0xfc: break;                           // null
                case 0xff: break;                           // BBS7 r
            }

            return cycles != this.Cycles;
        }

        public override void PoweredStep()
        {
            if (!this.Paused)
            {
                base.PoweredStep();
            }
        }

        protected override void OnLoweredRESET()
        {
            base.OnLoweredRESET();
            this.Stopped = this.Waiting = false;
        }

        protected override void OnLoweredINT()
        {
            base.OnLoweredINT();
            this.Waiting = false;
        }

        protected override void OnLoweredNMI()
        {
            base.OnLoweredNMI();
            this.Waiting = false;
        }

        #endregion

        #region Bus/Memory Access

        protected override void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            this.MemoryRead();      // Modify cycle (Change from M6502)
            this.MemoryWrite(data); // Write cycle
        }

        #endregion

        #region Addressing modes

        #region Address page fixup

        private readonly Register16 lastFetchAddress = new();

        protected override byte FetchByte()
        {
            this.lastFetchAddress.Assign(this.PC);
            return base.FetchByte();
        }

        protected override void Fixup()
        {
            var fixingLow = this.Bus.Address.Low;
            this.MemoryRead(this.lastFetchAddress);
            this.Bus.Address.Assign(fixingLow, this.FixedPage);
        }

        protected override void FixupBranch(sbyte relative)
        {
            this.NoteFixedAddress(this.PC.Word + relative);
            this.lastFetchAddress.Assign(this.Bus.Address);    // Effectively negate the use of "lastFetchAddress" for branch fixup usages
            this.MaybeFixup();
        }

        #endregion

        #region Address resolution

        protected void GetAddress()
        {
            this.GetWordPaged();

            if (this.Bus.Address.Low == 0)
            {
                this.Bus.Address.High++;
            }

            this.Bus.Address.Assign(this.Intermediate.Low, this.MemoryRead());
        }

        protected override void IndirectAddress()
        {
            this.AbsoluteAddress();
            this.GetAddress();
        }

        #endregion

        #region Address and read

        private void ZeroPageIndirectRead()
        {
            this.ZeroPageIndirectAddress();
            this.MemoryRead();
        }

        #endregion

        #endregion
    }
}