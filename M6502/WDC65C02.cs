// <copyright file="WDC65C02.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502
{
    using EightBit;

    public class WDC65C02 : Core
    {
        public WDC65C02(Bus bus)
        : base(bus)
        {
            this.LoweredRESET += this.WDC65C02_LoweredRESET;
            this.LoweredINT += this.WDC65C02_LoweredINT;
            this.LoweredNMI += this.WDC65C02_LoweredNMI;
        }

        private bool _stopped;
        private bool _waiting;

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
            this.ClearDecimal();  // Disable decimal mode (Change from MOS6502)
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
                case 0x02: this.SwallowFetch(); break;                              // NOP
                case 0x03: break;                                                   // null
                case 0x04: this.ZeroPage(); this.TSB(); break;                      // TSB zp
                case 0x07: this.ZeroPage(); this.RMB0(); break;                     // RMB0 zp
                case 0x0b: break;                                                   // null
                case 0x0c: this.Absolute(); this.TSB(); break;                      // TSB a
                case 0x0f: this.ZeroPage(); this.BBR0(); break;                     // BBR0 r

                case 0x12: this.ZeroPageIndirect(); this.ORA(); break;              // ORA (zp),y
                case 0x13: break;                                                   // null
                case 0x14: this.ZeroPage(); this.TRB(); break;                      // TRB zp
                case 0x17: this.ZeroPage(); this.RMB1(); break;                     // RMB1 zp
                case 0x1a: this.INCA(); break;                                      // INC A
                case 0x1b: break;                                                   // null
                case 0x1c: this.Absolute(); this.TRB(); break;                      // TRB a
                //case 0x1e: this.AbsoluteX(); this.ASL(); break; // ???
                case 0x1f: this.ZeroPage(); this.BBR1(); break;                     // BBR1 r

                case 0x22: this.SwallowFetch(); break;                              // NOP
                case 0x23: break;                                                   // null
                case 0x27: this.ZeroPage(); this.RMB2(); break;                     // RMB2 zp
                case 0x2b: break;                                                   // null
                case 0x2f: this.ZeroPage(); this.BBR2(); break;                     // BBR2 r

                case 0x32: this.ZeroPageIndirect(); this.AND(); break;              // AND (zp)
                case 0x33: break;                                                   // null
                case 0x34: ZeroPageX(); this.BIT(); break;                          // BIT zp,x
                case 0x37: this.ZeroPage(); this.RMB3(); break;                     // RMB3 zp
                case 0x3a: this.DECA(); break;                                      // DEC A
                case 0x3b: break;                                                   // null
                case 0x3c: AbsoluteX(); BIT(); break;                               // BIT abs,x
                //case 0x3e: break;// ????
                case 0x3f: this.ZeroPage(); this.BBR3(); break;                     // BBR3 r

                case 0x42: this.SwallowFetch(); break;                              // NOP
                case 0x43: break;                                                   // null
                case 0x47: this.ZeroPage(); this.RMB4(); break;                     // RMB4 zp
                case 0x4b: break;                                                   // null
                case 0x4f: this.ZeroPage(); this.BBR4(); break;                     // BBR4 r

                case 0x52: this.ZeroPageIndirect(); this.EOR(); break;              // EOR (zp)
                case 0x53: break;                                                   // null
                case 0x54: this.ZeroPageX(); break;                                 // zp,x NOP
                case 0x57: this.ZeroPage(); this.RMB5(); break;                     // RMB5 zp
                case 0x5a: this.PHY(); break;                                       // PHY s
                case 0x5b: break;                                                   // null
                case 0x5c: this.SwallowFetch(); this.SwallowRead(); this.SwallowFetch();  break;  // ???
                //case 0x5e: break;// ????
                case 0x5f: this.ZeroPage(); this.BBR5(); break;                     // BBR5 r

                case 0x62: this.SwallowFetch(); break;                              // *NOP
                case 0x63: break;                                                                               // null
                case 0x64: this.ZeroPageAddress(); this.STZ(); break;               // STZ zp
                case 0x67: this.ZeroPage(); this.RMB6(); break;                     // RMB6 zp
                case 0x6b: break;                                                   // null
                case 0x6f: this.ZeroPage(); this.BBR6(); break;                     // BBR6 r

                case 0x72: this.ZeroPageIndirect(); this.ADC(); break;              // ADC (zp)
                case 0x73: break;                                                                               // null
                case 0x74: this.ZeroPageXAddress(); this.STZ(); break;              // STZ zp,x
                case 0x77: this.ZeroPage(); this.RMB7(); break;                     // RMB7 zp
                case 0x7a: this.PLY(); break;                                       // PLY s
                case 0x7b: break;                                                                               // null
                case 0x7c: this.AbsoluteXAddress(); this.JumpIndirect(); break;     // JMP (a,x)
                case 0x7f: this.ZeroPage(); this.BBR7(); break;                     // BBR7 r

                case 0x80: this.BRA(); break;                                       // BRA r
                case 0x83: break;                                                                               // null
                case 0x87: this.ZeroPage(); this.SMB0(); break;                     // SMB0 zp
                case 0x89: this.Immediate(); this.BIT_immediate(); break;                                                   // BIT # (TBC)
                case 0x8b: break;                                                   // null
                case 0x8f: this.ZeroPage(); this.BBS0(); break;                     // BBS0 r

                case 0x92: this.ZeroPageIndirectAddress(); this.STA(); break;       // STA (zp)
                case 0x93: break;                                                                               // null
                case 0x97: this.ZeroPage(); this.SMB1(); break;                     // SMB1 zp
                case 0x9b: break;                                                   // null
                case 0x9c: this.AbsoluteAddress(); this.STZ(); break;               // STZ a
                case 0x9e: this.AbsoluteXAddress(); this.Fixup(); this.STZ(); break;              // STZ a,x
                case 0x9f: this.ZeroPage(); this.BBS1(); break;                     // BBS1 r

                case 0xa3: break;                                                                               // null
                case 0xa7: this.ZeroPage(); this.SMB2(); break;                     // SMB2 zp
                case 0xab: break;                                                   // null
                case 0xaf: this.ZeroPage(); this.BBS2(); break;                     // BBS2 r

                case 0xb2: this.ZeroPageIndirect(); this.LDA(); break;              // LDA (zp)
                case 0xb3: break;                                                   // null
                case 0xb7: this.ZeroPage(); this.SMB3(); break;                     // SMB3 zp
                case 0xbb: break;                                                   // null
                case 0xbf: this.ZeroPage(); this.BBS3(); break;                     // BBS3 r

                case 0xc3: break;                                                   // null
                case 0xc7: this.ZeroPage(); this.SMB4(); break;                     // SMB4 zp
                case 0xcb: this.WAI(); break;                                       // WAI i
                case 0xcf: this.ZeroPage(); this.BBS4(); break;                     // BBS4 r

                case 0xd2: this.ZeroPageIndirect(); this.CMP(); break;              // CMP (zp)
                case 0xd3: break;                                                                               // null
                case 0xd4: this.ZeroPageX(); break;                                 // zp,x NOP
                case 0xd7: this.ZeroPage(); this.SMB5(); break;                     // SMB5 zp
                case 0xda: this.PHX(); break;                                       // PHX s
                case 0xdb: this.STP(); break;                                       // STP i
                case 0xdc: this.Absolute(); break;                               // null
                case 0xdf: this.ZeroPage(); this.BBS5(); break;                     // BBS5 r

                case 0xe3: break;                                                   // null
                case 0xe7: this.ZeroPage(); this.SMB6(); break;                     // SMB6 zp
                case 0xeb: break;                                                   // null
                case 0xef: this.ZeroPage(); this.BBS6(); break;                     // BBS6 r

                case 0xf2: this.ZeroPageIndirect(); this.SBC(); break;              // SBC (zp)
                case 0xf3: break;                                                   // null
                case 0xf4: this.ZeroPageX(); break;                                 // zp,x NOP
                case 0xf7: this.ZeroPage(); this.SMB7(); break;                     // SMB7 zp
                case 0xfa: this.PLX(); break;                                       // PLX s
                case 0xfb: break;                                                   // null
                case 0xfc: break;                                                   // null
                case 0xff: this.ZeroPage(); this.BBS7(); break;                // BBS7 r
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

        private void WDC65C02_LoweredRESET(object? sender, EventArgs e)
        {
            this.Stopped = this.Waiting = false;
        }

        private void WDC65C02_LoweredINT(object? sender, EventArgs e)
        {
            this.Waiting = false;
        }

        private void WDC65C02_LoweredNMI(object? sender, EventArgs e)
        {
            this.Waiting = false;
        }

        #endregion

        #region Bus/Memory Access

        protected override void ModifyWrite(byte data)
        {
            // The read will have already taken place...
            this.MemoryRead();      // Modify cycle (Change from MOS6502)
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

        // Not used by BBR/BBS, but used by other branch instructions
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

        private byte ZeroPageIndirect()
        {
            this.ZeroPageIndirectAddress();
            return this.MemoryRead();
        }

        #endregion

        #endregion

        private void BIT_immediate()
        {
            this.AdjustZero((byte)(this.A & this.Bus.Data));
        }

        private void INCA()
        {
            this.SwallowRead();
            this.A = this.INC(this.A);
        }

        private void DECA()
        {
            this.SwallowRead();
            this.A = this.DEC(this.A);
        }

        private void PHX()
        {
            this.SwallowRead();
            this.Push(this.X);
        }

        private void PLX()
        {
            this.SwallowRead();
            this.SwallowPop();
            this.X = this.Through(this.Pop());
        }

        private void PHY()
        {
            this.SwallowRead();
            this.Push(this.Y);
        }

        private void PLY()
        {
            this.SwallowRead();
            this.SwallowPop();
            this.Y = this.Through(this.Pop());
        }

        private void JumpIndirect()
        {
            this.Bus.Address.Assign(this.Intermediate);
            this.GetAddressPaged();
            this.JMP();
        }

        private void BRA() => this.Branch(true);

        private void WAI()
        {
            this.SwallowRead();
            this.Waiting = true;
        }

        private void STP()
        {
            this.SwallowRead();
            this.Stopped = true;
        }

        private void STZ() => this.MemoryWrite(0);

        private void RMB0() => this.RMB(Bit(0));
        
        private void RMB1() => this.RMB(Bit(1));
        
        private void RMB2() => this.RMB(Bit(2));
        
        private void RMB3() => this.RMB(Bit(3));
        
        private void RMB4() => this.RMB(Bit(4));
        
        private void RMB5() => this.RMB(Bit(5));
        
        private void RMB6() => this.RMB(Bit(6));
        
        private void RMB7() => this.RMB(Bit(7));

        private void RMB(byte flag)
        {
            this.MemoryRead();
            this.Bus.Data &= (byte)~flag;
            this.MemoryWrite();
        }

        private void SMB0() => this.SMB(Bit(0));

        private void SMB1() => this.SMB(Bit(1));

        private void SMB2() => this.SMB(Bit(2));

        private void SMB3() => this.SMB(Bit(3));

        private void SMB4() => this.SMB(Bit(4));

        private void SMB5() => this.SMB(Bit(5));

        private void SMB6() => this.SMB(Bit(6));

        private void SMB7() => this.SMB(Bit(7));

        private void SMB(byte flag)
        {
            this.MemoryRead();
            this.Bus.Data |= flag;
            this.MemoryWrite();
        }

        private void BranchBit(bool condition)
        {
            _ = this.FetchByte();
            if (condition)
            {
                var relative = (sbyte)this.Bus.Data;
                this.SwallowRead();
                this.NoteFixedAddress(this.PC.Word + relative);
                if (this.Bus.Address.High != this.FixedPage)
                {
                    this.SwallowRead();
                }
                this.Jump(this.Intermediate);
            }
        }

        private void BBS0() => this.BBS(Bit(0));

        private void BBS1() => this.BBS(Bit(1));

        private void BBS2() => this.BBS(Bit(2));

        private void BBS3() => this.BBS(Bit(3));

        private void BBS4() => this.BBS(Bit(4));

        private void BBS5() => this.BBS(Bit(5));

        private void BBS6() => this.BBS(Bit(6));

        private void BBS7() => this.BBS(Bit(7));

        private void BBS(byte flag)
        {
            this.MemoryRead();
            this.BranchBit((this.Bus.Data & flag) != 0);
        }

        private void BBR0() => this.BBR(Bit(0));
        private void BBR1() => this.BBR(Bit(1));
        private void BBR2() => this.BBR(Bit(2));
        private void BBR3() => this.BBR(Bit(3));
        private void BBR4() => this.BBR(Bit(4));
        private void BBR5() => this.BBR(Bit(5));
        private void BBR6() => this.BBR(Bit(6));
        private void BBR7() => this.BBR(Bit(7));

        private void BBR(byte flag)
        {
            this.MemoryRead();
            this.BranchBit((this.Bus.Data & flag) == 0);
        }

        private void TSB()
        {
            this.AdjustZero((byte)(this.A & this.Bus.Data));
            this.ModifyWrite((byte)(this.A | this.Bus.Data));
        }

        private void TRB()
        {
            this.AdjustZero((byte)(this.A & this.Bus.Data));
            this.ModifyWrite((byte)(~this.A & this.Bus.Data));
        }
    }
}