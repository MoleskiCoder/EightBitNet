// <copyright file="W65C02.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class W65C02(Bus bus) : M6502Core(bus)
    {
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

            return false;
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

        #endregion
    }
}