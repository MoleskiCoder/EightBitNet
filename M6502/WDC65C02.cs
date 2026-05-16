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
            this.CLD();  // Disable decimal mode (Change from MOS6502)
        }

        #endregion

        #region Cycle wastage

        private void SwallowFixup() => this.MemoryRead(this.lastFetchAddress);

        private void SwallowSpin(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                this.MemoryRead();
            }
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
                case 0x02: this.SwallowFetch(); NOP(); break;                                           // NOP (implied)
                case 0x04: this.ZeroPage(); this.TSB(); break;                                          // TSB (zero page)
                case 0x07: this.ZeroPage(); this.RMB0(); break;                                         // RMB0 (zero page)
                case 0x0c: this.Absolute(); this.TSB(); break;                                          // TSB (absolute)
                case 0x0f: this.ZeroPage(); this.BBR0(); break;                                         // BBR0 (zero page)

                case 0x12: this.ZeroPageIndirect(); this.ORA(); break;                                  // ORA (zero page indirect)
                case 0x14: this.ZeroPage(); this.TRB(); break;                                          // TRB (zero page)
                case 0x17: this.ZeroPage(); this.RMB1(); break;                                         // RMB1 (zero page)
                case 0x1a: this.SwallowRead(); this.INCA(); break;                                      // INC A (implied)
                case 0x1c: this.Absolute(); this.TRB(); break;                                          // TRB (absolute)
                case 0x1e: this.AbsoluteX(); this.ASL(); break;                                         // ASL (absolute, X)
                case 0x1f: this.ZeroPage(); this.BBR1(); break;                                         // BBR1 (zero page)

                case 0x22: this.SwallowFetch(); NOP(); break;                                           // NOP (implied)
                case 0x27: this.ZeroPage(); this.RMB2(); break;                                         // RMB2 (zero page)
                case 0x2f: this.ZeroPage(); this.BBR2(); break;                                         // BBR2 (zero page)

                case 0x32: this.ZeroPageIndirect(); this.AND(); break;                                  // AND (zero page indirect)
                case 0x34: this.ZeroPageX(); this.BIT(); break;                                         // BIT (zero page, X)
                case 0x37: this.ZeroPage(); this.RMB3(); break;                                         // RMB3 (zero page)
                case 0x3a: this.SwallowRead(); this.DECA(); break;                                      // DEC A (implied)
                case 0x3c: this.AbsoluteX(); this.BIT(); break;                                         // BIT (absolute, X)
                case 0x3e: this.AbsoluteX(); this.ROL(); break;                                         // ROL (absolute, X)
                case 0x3f: this.ZeroPage(); this.BBR3(); break;                                         // BBR3 (zero page)

                case 0x42: this.SwallowFetch(); NOP(); break;                                           // NOP (implied)
                case 0x47: this.ZeroPage(); this.RMB4(); break;                                         // RMB4 (zero page)
                case 0x4f: this.ZeroPage(); this.BBR4(); break;                                         // BBR4 (zero page)

                case 0x52: this.ZeroPageIndirect(); this.EOR(); break;                                  // EOR (zero page indirect)
                case 0x54: this.ZeroPageX(); NOP(); break;                                              // NOP (zero page, X)
                case 0x57: this.ZeroPage(); this.RMB5(); break;                                         // RMB5 (zero page)
                case 0x5a: this.SwallowRead(); this.PHY(); break;                                       // PHY (implied)
                case 0x5c: this.AbsoluteAddress(); this.SwallowFixup(); NOP(); break;                   // NOP (absolute)
                case 0x5e: this.AbsoluteX(); this.LSR(); break;                                         // LSR (absolute, X)
                case 0x5f: this.ZeroPage(); this.BBR5(); break;                                         // BBR5 (zero page)

                case 0x62: this.SwallowFetch(); NOP(); break;                                           // NOP (implied)
                case 0x64: this.ZeroPageAddress(); this.STZ(); break;                                   // STZ (zero page)
                case 0x67: this.ZeroPage(); this.RMB6(); break;                                         // RMB6 (zero page)
                case 0x6f: this.ZeroPage(); this.BBR6(); break;                                         // BBR6 (zero page)

                case 0x72: this.ZeroPageIndirect(); this.ADC(); break;                                  // ADC (zero page indirect)
                case 0x74: this.ZeroPageXAddress(); this.STZ(); break;                                  // STZ (zero page, X)
                case 0x77: this.ZeroPage(); this.RMB7(); break;                                         // RMB7 (zero page)
                case 0x7a: this.SwallowRead(); this.PLY(); break;                                       // PLY (implied)
                case 0x7c: this.AbsoluteXIndirectAddress(); JMP(); break;                               // JMP (absolute, X indirect)
                case 0x7e: this.AbsoluteX(); this.ROR(); break;                                         // ROR (absolute, X)
                case 0x7f: this.ZeroPage(); this.BBR7(); break;                                         // BBR7 (zero page)

                case 0x80: this.Immediate(); this.BRA(); break;                                         // BRA (immediate)
                case 0x87: this.ZeroPage(); this.SMB0(); break;                                         // SMB0 (zero page)
                case 0x89: this.Immediate(); this.BIT_immediate(); break;                               // BIT (immediate)
                case 0x8f: this.ZeroPage(); this.BBS0(); break;                                         // BBS0 (zero page)

                case 0x92: this.ZeroPageIndirectAddress(); this.STA(); break;                           // STA (zero page indirect)
                case 0x97: this.ZeroPage(); this.SMB1(); break;                                         // SMB1 (zero page)
                case 0x9c: this.AbsoluteAddress(); this.STZ(); break;                                   // STZ (absolute)
                case 0x9e: this.AbsoluteXAddress(); this.Fixup(); this.STZ(); break;                    // STZ (absolute, X)
                case 0x9f: this.ZeroPage(); this.BBS1(); break;                                         // BBS1 (zero page)

                case 0xa7: this.ZeroPage(); this.SMB2(); break;                                         // SMB2 (zero page)
                case 0xaf: this.ZeroPage(); this.BBS2(); break;                                         // BBS2 (zero page)

                case 0xb2: this.ZeroPageIndirect(); this.LDA(); break;                                  // LDA (zero page indirect)
                case 0xb7: this.ZeroPage(); this.SMB3(); break;                                         // SMB3 (zero page)
                case 0xbf: this.ZeroPage(); this.BBS3(); break;                                         // BBS3 (zero page)

                case 0xc7: this.ZeroPage(); this.SMB4(); break;                                         // SMB4 (zero page)
                case 0xcb: this.SwallowRead(); this.WAI(); break;                                       // WAI (implied)
                case 0xcf: this.ZeroPage(); this.BBS4(); break;                                         // BBS4 (zero page)

                case 0xd2: this.ZeroPageIndirect(); this.CMP(); break;                                  // CMP (zero page indirect)
                case 0xd4: this.ZeroPageX(); NOP(); break;                                              // NOP (zero page, X)
                case 0xd7: this.ZeroPage(); this.SMB5(); break;                                         // SMB5 (zero page)
                case 0xda: this.SwallowRead(); this.PHX(); break;                                       // PHX (implied)
                case 0xdb: this.SwallowRead(); this.STP(); break;                                       // STP (implied)
                case 0xdc: this.AbsoluteAddress(); this.SwallowFixup(); NOP(); break;                   // NOP (absolute)
                case 0xdf: this.ZeroPage(); this.BBS5(); break;                                         // BBS5 (zero page)

                case 0xe7: this.ZeroPage(); this.SMB6(); break;                                         // SMB6 (zero page)
                case 0xef: this.ZeroPage(); this.BBS6(); break;                                         // BBS6 (zero page)

                case 0xf2: this.ZeroPageIndirect(); this.SBC(); break;                                  // SBC (zero page indirect)
                case 0xf4: this.ZeroPageX(); NOP(); break;                                              // NOP (zero page, X)
                case 0xf7: this.ZeroPage(); this.SMB7(); break;                                         // SMB7 (zero page)
                case 0xfa: this.SwallowRead(); this.PLX(); break;                                       // PLX (implied)
                case 0xfc: this.AbsoluteAddress(); this.SwallowFixup(); NOP(); break;                   // NOP (absolute)
                case 0xff: this.ZeroPage(); this.BBS7(); break;                                         // BBS7 (zero page)
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
            this.SwallowSpin();     // Modify cycle (Change from MOS6502)
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
            this.SwallowFixup();
            this.Bus.Address.Assign(fixingLow, this.FixedPage);
        }

        // Not used by BBR/BBS, but used by other branch instructions
        protected override void FixupBranch(sbyte relative)
        {
            this.NoteFixedAddress(this.PC.Joined + relative);
            this.lastFetchAddress.Assign(this.Bus.Address);    // Effectively negate the use of "lastFetchAddress" for branch fixup usages
            this.MaybeFixup();
        }

        #endregion

        #region Address resolution

        protected void GetAddress()
        {
            this.GetShortPaged();

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

        private void AbsoluteXIndirectAddress()
        {
            var current = this.PC.Joined;
            this.AbsoluteXAddress();
            this.Bus.Address.Joined = current;
            this.SwallowSpin();
            this.Bus.Address.Assign(this.Intermediate);
            this.GetInto(this.Intermediate);
            this.Bus.Address.Assign(this.Intermediate);
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

        #region Processor variations

        protected override byte BinaryADC(byte data)
        {
            var returned = base.BinaryADC(data);
            this.AdjustNZ(returned);
            return returned;
        }

        protected override byte DecimalADC(byte data)
        {
            var returned = base.DecimalADC(data);
            this.AdjustNZ(returned);
            if (this.ImmediateInstruction)
            {
                this.MemoryRead(0x7f, 0x00);
            }
            else
            {
                this.SwallowSpin();
            }
            return returned;
        }

        protected override void PostSUB(byte operand)
        {
            // PostSUB needs to be empty, so we can sort out the
            // post calculations memory operations in DecimalSUB.
        }

        protected override byte BinarySUB(byte operand, int borrow = 0)
        {
            var result = base.BinarySUB(operand, borrow);
            base.PostSUB(operand);
            this.AdjustNZ(result);
            return result;
        }

        protected override byte DecimalSUB(byte operand, int borrow)
        {
            base.BinarySUB(operand, borrow);

            var data = this.Bus.Data;
            var result = this.Intermediate.Low;

            var loBorrow = (LowNibble(operand) < LowNibble(data) + borrow) ? 1 : 0;
            if (loBorrow != 0)
            {
                result -= 6;
            }

            if (HighNibble(operand) < HighNibble(data) + loBorrow)
            {
                result -= 0x60;
            }

            base.PostSUB(operand);
            this.AdjustNZ(result);

            if (this.ImmediateInstruction)
                this.MemoryRead(0x00, 0x00);
            else
                this.SwallowSpin();

            return result;
        }

        #endregion

        private void BIT_immediate() => this.BitSet(this.Bus.Data);

        private void INCA() => this.A = this.INC(this.A);

        private void DECA() => this.A = this.DEC(this.A);

        private void PHX() => this.Push(this.X);

        private void PLX()
        {
            this.SwallowPop();
            this.X = this.Through(this.Pop());
        }

        private void PHY() => this.Push(this.Y);

        private void PLY()
        {
            this.SwallowPop();
            this.Y = this.Through(this.Pop());
        }

        private void BRA() => this.Branch(true);

        private void WAI() => this.Waiting = true;

        private void STP() => this.Stopped = true;

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
            this.SwallowSpin();
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
            this.SwallowSpin();
            this.Bus.Data |= flag;
            this.MemoryWrite();
        }

        private void BranchBit(bool condition)
        {
            this.SwallowSpin();
            this.FetchByte();
            if (condition)
            {
                var relative = (sbyte)this.Bus.Data;
                this.SwallowRead();
                this.NoteFixedAddress(this.PC.Joined + relative);
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

        private void BBS(byte flag) => this.BranchBit((this.Bus.Data & flag) != 0);

        private void BBR0() => this.BBR(Bit(0));
        private void BBR1() => this.BBR(Bit(1));
        private void BBR2() => this.BBR(Bit(2));
        private void BBR3() => this.BBR(Bit(3));
        private void BBR4() => this.BBR(Bit(4));
        private void BBR5() => this.BBR(Bit(5));
        private void BBR6() => this.BBR(Bit(6));
        private void BBR7() => this.BBR(Bit(7));

        private void BBR(byte flag) => this.BranchBit((this.Bus.Data & flag) == 0);

        private void TSB()
        {
            this.BitSet(this.Bus.Data);
            this.ModifyWrite((byte)(this.A | this.Bus.Data));
        }

        private void TRB()
        {
            this.BitSet(this.Bus.Data);
            this.ModifyWrite((byte)(~this.A & this.Bus.Data));
        }
    }
}