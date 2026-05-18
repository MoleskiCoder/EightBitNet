// <copyright file="HD6309.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    using EightBit;
    using System.Diagnostics;

    // HD6309 - Hitachi CMOS implementation of the MC6809
    // References:
    // https://colorcomputerarchive.com/repo/Documents/Books/Motorola%206809%20and%20Hitachi%206309%20Programming%20Reference%20(Darren%20Atkinson).pdf
    // http://www.6809.org.uk/dragon/hd6309.shtml

    public sealed class HD6309(Bus bus) : MC6809(bus)
    {
        // MD register bits
        private const byte MD_NATIVE = 0x01;   // Native mode (vs 6809 emulation)
        private const byte MD_FIRQ   = 0x02;   // FIRQ uses entire register save
        private const byte MD_ILL    = 0x40;   // Illegal instruction flag
        private const byte MD_DIV0   = 0x80;   // Division by zero flag

        private byte _md;

        #region Registers

        public Register16 W { get; } = new();

        public ref byte E => ref this.W.High;

        public ref byte F => ref this.W.Low;

        public Register16 V { get; } = new();

        public ref byte MD => ref this._md;

        public uint Q
        {
            get => Chip.MakeInteger(this.W.Joined, this.D.Joined);
            set
            {
                this.D.Joined = Chip.HighShort(value);
                this.W.Joined = Chip.LowShort(value);
            }
        }

        public bool NativeMode => (this._md & MD_NATIVE) != 0;

        #endregion

        #region Register transfer extensions

        protected override Register16 ReferenceTransfer16(int specifier)
        {
            return specifier switch
            {
                0b0110 => this.W,
                0b0111 => this.V,
                _ => base.ReferenceTransfer16(specifier),
            };
        }

        protected override ref byte ReferenceTransfer8(int specifier)
        {
            switch (specifier)
            {
                case 0b1110:
                    return ref this.E;
                case 0b1111:
                    return ref this.F;
                default:
                    return ref base.ReferenceTransfer8(specifier);
            }
        }

        #endregion

        #region Instruction dispatch overrides

        protected override void ExecuteUnprefixed()
        {
            switch (this.OpCode)
            {
                // OIM (OR immediate with memory)
                case 0x01: this.OIM_Direct(); break;    // OIM direct
                case 0x61: this.OIM_Indexed(); break;   // OIM indexed
                case 0x71: this.OIM_Extended(); break;  // OIM extended

                // AIM (AND immediate with memory)
                case 0x02: this.AIM_Direct(); break;    // AIM direct
                case 0x62: this.AIM_Indexed(); break;   // AIM indexed
                case 0x72: this.AIM_Extended(); break;  // AIM extended

                // EIM (EOR immediate with memory)
                case 0x05: this.EIM_Direct(); break;    // EIM direct
                case 0x65: this.EIM_Indexed(); break;   // EIM indexed
                case 0x75: this.EIM_Extended(); break;  // EIM extended

                // TIM (test immediate with memory, no write-back)
                case 0x0B: this.TIM_Direct(); break;    // TIM direct
                case 0x6B: this.TIM_Indexed(); break;   // TIM indexed
                case 0x7B: this.TIM_Extended(); break;  // TIM extended

                default:
                    base.ExecuteUnprefixed();
                    break;
            }
        }

        protected override void Execute10()
        {
            switch (this.OpCode)
            {
                // Register-to-register operations (postbyte encodes source:destination)
                case 0x30: this.ImmediateByte(); this.ADDR(); Debug.Assert(this.Cycles == 4); break;   // ADDR
                case 0x31: this.ImmediateByte(); this.ADCR(); Debug.Assert(this.Cycles == 4); break;   // ADCR
                case 0x32: this.ImmediateByte(); this.SUBR(); Debug.Assert(this.Cycles == 4); break;   // SUBR
                case 0x33: this.ImmediateByte(); this.SBCR(); Debug.Assert(this.Cycles == 4); break;   // SBCR
                case 0x34: this.ImmediateByte(); this.ANDR(); Debug.Assert(this.Cycles == 4); break;   // ANDR
                case 0x35: this.ImmediateByte(); this.ORR();  Debug.Assert(this.Cycles == 4); break;   // ORR
                case 0x36: this.ImmediateByte(); this.EORR(); Debug.Assert(this.Cycles == 4); break;   // EORR
                case 0x37: this.ImmediateByte(); this.CMPR(); Debug.Assert(this.Cycles == 4); break;   // CMPR

                // W register stack operations
                case 0x38: this.SwallowCurrent(); this.PSHSW(); Debug.Assert(this.Cycles == 6); break; // PSHSW
                case 0x39: this.SwallowCurrent(); this.PULSW(); Debug.Assert(this.Cycles == 6); break; // PULSW
                case 0x3A: this.SwallowCurrent(); this.PSHUW(); Debug.Assert(this.Cycles == 6); break; // PSHUW
                case 0x3B: this.SwallowCurrent(); this.PULUW(); Debug.Assert(this.Cycles == 6); break; // PULUW

                // E register operations (inherent)
                case 0x40: this.SwallowCurrent(); this.NEGE();  Debug.Assert(this.Cycles == 3); break;
                case 0x43: this.SwallowCurrent(); this.COME();  Debug.Assert(this.Cycles == 3); break;
                case 0x44: this.SwallowCurrent(); this.LSRE();  Debug.Assert(this.Cycles == 3); break;
                case 0x46: this.SwallowCurrent(); this.RORE();  Debug.Assert(this.Cycles == 3); break;
                case 0x47: this.SwallowCurrent(); this.ASRE();  Debug.Assert(this.Cycles == 3); break;
                case 0x48: this.SwallowCurrent(); this.ASLE();  Debug.Assert(this.Cycles == 3); break;
                case 0x49: this.SwallowCurrent(); this.ROLE();  Debug.Assert(this.Cycles == 3); break;
                case 0x4A: this.SwallowCurrent(); this.DECE();  Debug.Assert(this.Cycles == 3); break;
                case 0x4C: this.SwallowCurrent(); this.INCE();  Debug.Assert(this.Cycles == 3); break;
                case 0x4D: this.SwallowCurrent(); this.TSTE();  Debug.Assert(this.Cycles == 3); break;
                case 0x4F: this.SwallowCurrent(); this.CLRE();  Debug.Assert(this.Cycles == 3); break;

                // F register operations (inherent)
                case 0x50: this.SwallowCurrent(); this.NEGF(); Debug.Assert(this.Cycles == 3); break;
                case 0x53: this.SwallowCurrent(); this.COMF();  Debug.Assert(this.Cycles == 3); break;
                case 0x54: this.SwallowCurrent(); this.LSRF();  Debug.Assert(this.Cycles == 3); break;
                case 0x56: this.SwallowCurrent(); this.RORF();  Debug.Assert(this.Cycles == 3); break;
                case 0x57: this.SwallowCurrent(); this.ASRF();  Debug.Assert(this.Cycles == 3); break;
                case 0x58: this.SwallowCurrent(); this.ASLF();  Debug.Assert(this.Cycles == 3); break;
                case 0x59: this.SwallowCurrent(); this.ROLF();  Debug.Assert(this.Cycles == 3); break;
                case 0x5A: this.SwallowCurrent(); this.DECF();  Debug.Assert(this.Cycles == 3); break;
                case 0x5C: this.SwallowCurrent(); this.INCF();  Debug.Assert(this.Cycles == 3); break;
                case 0x5D: this.SwallowCurrent(); this.TSTF();  Debug.Assert(this.Cycles == 3); break;
                case 0x5F: this.SwallowCurrent(); this.CLRF();  Debug.Assert(this.Cycles == 3); break;

                // W register load/store/arithmetic
                case 0x80: this.ImmediateShort(); this.SUBW();  Debug.Assert(this.Cycles == 5); break;  // SUBW immediate
                case 0x81: this.ImmediateShort(); this.CMPW();  Debug.Assert(this.Cycles == 5); break;  // CMPW immediate
                case 0x86: this.ImmediateShort(); this.LDW();   Debug.Assert(this.Cycles == 4); break;  // LDW immediate
                case 0x8B: this.ImmediateShort(); this.ADDW();  Debug.Assert(this.Cycles == 5); break;  // ADDW immediate

                case 0x90: this.DirectShort(); this.SUBW();     Debug.Assert(this.Cycles == 7); break;  // SUBW direct
                case 0x91: this.DirectShort(); this.CMPW();     Debug.Assert(this.Cycles == 7); break;  // CMPW direct
                case 0x96: this.DirectShort(); this.LDW();      Debug.Assert(this.Cycles == 6); break;  // LDW direct
                case 0x97: this.DirectAddress(); this.STW();   Debug.Assert(this.Cycles == 6); break;  // STW direct
                case 0x9B: this.DirectShort(); this.ADDW();     Debug.Assert(this.Cycles == 7); break;  // ADDW direct

                case 0xA0: this.IndexedShort(); this.SUBW();    Debug.Assert(this.Cycles >= 7); break;  // SUBW indexed
                case 0xA1: this.IndexedShort(); this.CMPW();    Debug.Assert(this.Cycles >= 7); break;  // CMPW indexed
                case 0xA6: this.IndexedShort(); this.LDW();     Debug.Assert(this.Cycles >= 6); break;  // LDW indexed
                case 0xA7: this.IndexedAddress(); this.STW();  Debug.Assert(this.Cycles >= 6); break;  // STW indexed
                case 0xAB: this.IndexedShort(); this.ADDW();    Debug.Assert(this.Cycles >= 7); break;  // ADDW indexed

                case 0xB0: this.ExtendedShort(); this.SUBW();   Debug.Assert(this.Cycles == 8); break;  // SUBW extended
                case 0xB1: this.ExtendedShort(); this.CMPW();   Debug.Assert(this.Cycles == 8); break;  // CMPW extended
                case 0xB6: this.ExtendedShort(); this.LDW();    Debug.Assert(this.Cycles == 7); break;  // LDW extended
                case 0xB7: this.ExtendedAddress(); this.STW(); Debug.Assert(this.Cycles == 7); break;  // STW extended
                case 0xBB: this.ExtendedShort(); this.ADDW();   Debug.Assert(this.Cycles == 8); break;  // ADDW extended

                // Q register load/store
                case 0xCD: this.LDQ_Immediate(); break;                          // LDQ immediate
                case 0xDC: this.DirectShort(); this.LDQ_Lower(); break;           // LDQ direct
                case 0xEC: this.IndexedShort(); this.LDQ_Lower(); break;          // LDQ indexed
                case 0xFC: this.ExtendedShort(); this.LDQ_Lower(); break;         // LDQ extended

                case 0xDD: this.DirectAddress(); this.STQ(); break;              // STQ direct
                case 0xED: this.IndexedAddress(); this.STQ(); break;             // STQ indexed
                case 0xFD: this.ExtendedAddress(); this.STQ(); break;            // STQ extended

                default:
                    base.Execute10();
                    break;
            }
        }

        protected override void Execute11()
        {
            switch (this.OpCode)
            {
                // Block transfers (TFM) — $11,38-$11,3B
                case 0x38: this.ImmediateByte(); this.TFM_IncrBoth(); break;   // TFM R0+,R1+
                case 0x39: this.ImmediateByte(); this.TFM_DecrBoth(); break;   // TFM R0-,R1-
                case 0x3A: this.ImmediateByte(); this.TFM_IncrSrc();  break;   // TFM R0+,R1
                case 0x3B: this.ImmediateByte(); this.TFM_IncrDst();  break;   // TFM R0,R1+

                // LDMD — $11,3D immediate
                case 0x3D: this.ImmediateByte(); this.LDMD(); break;           // LDMD immediate

                // Bit manipulation instructions — $11,30-$11,37
                case 0x30: this.ImmediateByte(); this.BAND();  break;   // BAND
                case 0x31: this.ImmediateByte(); this.BIAND(); break;   // BIAND
                case 0x32: this.ImmediateByte(); this.BOR();   break;   // BOR
                case 0x33: this.ImmediateByte(); this.BIOR();  break;   // BIOR
                case 0x34: this.ImmediateByte(); this.BEOR();  break;   // BEOR
                case 0x35: this.ImmediateByte(); this.BIEOR(); break;   // BIEOR
                case 0x36: this.ImmediateByte(); this.LDBT();  break;   // LDBT
                case 0x37: this.ImmediateByte(); this.STBT();  break;   // STBT

                // E register operations
                case 0x80: this.ImmediateByte(); this.SUBE();  Debug.Assert(this.Cycles == 3); break;   // SUBE immediate
                case 0x81: this.ImmediateByte(); this.CMPE();  Debug.Assert(this.Cycles == 3); break;   // CMPE immediate
                case 0x86: this.ImmediateByte(); this.LDE();   Debug.Assert(this.Cycles == 2); break;   // LDE immediate
                case 0x8B: this.ImmediateByte(); this.ADDE();  Debug.Assert(this.Cycles == 3); break;   // ADDE immediate

                case 0x90: this.DirectByte(); this.SUBE();     Debug.Assert(this.Cycles == 5); break;   // SUBE direct
                case 0x91: this.DirectByte(); this.CMPE();     Debug.Assert(this.Cycles == 5); break;   // CMPE direct
                case 0x96: this.DirectByte(); this.LDE();      Debug.Assert(this.Cycles == 4); break;   // LDE direct
                case 0x97: this.DirectAddress(); this.STE();   Debug.Assert(this.Cycles == 4); break;   // STE direct
                case 0x9B: this.DirectByte(); this.ADDE();     Debug.Assert(this.Cycles == 5); break;   // ADDE direct

                case 0xA0: this.IndexedByte(); this.SUBE();    Debug.Assert(this.Cycles >= 5); break;   // SUBE indexed
                case 0xA1: this.IndexedByte(); this.CMPE();    Debug.Assert(this.Cycles >= 5); break;   // CMPE indexed
                case 0xA6: this.IndexedByte(); this.LDE();     Debug.Assert(this.Cycles >= 4); break;   // LDE indexed
                case 0xA7: this.IndexedAddress(); this.STE();  Debug.Assert(this.Cycles >= 4); break;   // STE indexed
                case 0xAB: this.IndexedByte(); this.ADDE();    Debug.Assert(this.Cycles >= 5); break;   // ADDE indexed

                case 0xB0: this.ExtendedByte(); this.SUBE();   Debug.Assert(this.Cycles == 6); break;   // SUBE extended
                case 0xB1: this.ExtendedByte(); this.CMPE();   Debug.Assert(this.Cycles == 6); break;   // CMPE extended
                case 0xB6: this.ExtendedByte(); this.LDE();    Debug.Assert(this.Cycles == 5); break;   // LDE extended
                case 0xB7: this.ExtendedAddress(); this.STE(); Debug.Assert(this.Cycles == 5); break;   // STE extended
                case 0xBB: this.ExtendedByte(); this.ADDE();   Debug.Assert(this.Cycles == 6); break;   // ADDE extended

                // F register operations
                case 0xC0: this.ImmediateByte(); this.SUBF();  Debug.Assert(this.Cycles == 3); break;   // SUBF immediate
                case 0xC1: this.ImmediateByte(); this.CMPF();  Debug.Assert(this.Cycles == 3); break;   // CMPF immediate
                case 0xC6: this.ImmediateByte(); this.LDF();   Debug.Assert(this.Cycles == 2); break;   // LDF immediate
                case 0xCB: this.ImmediateByte(); this.ADDF();  Debug.Assert(this.Cycles == 3); break;   // ADDF immediate

                case 0xD0: this.DirectByte(); this.SUBF();     Debug.Assert(this.Cycles == 5); break;   // SUBF direct
                case 0xD1: this.DirectByte(); this.CMPF();     Debug.Assert(this.Cycles == 5); break;   // CMPF direct
                case 0xD6: this.DirectByte(); this.LDF();      Debug.Assert(this.Cycles == 4); break;   // LDF direct
                case 0xD7: this.DirectAddress(); this.STF();   Debug.Assert(this.Cycles == 4); break;   // STF direct
                case 0xDB: this.DirectByte(); this.ADDF();     Debug.Assert(this.Cycles == 5); break;   // ADDF direct

                case 0xE0: this.IndexedByte(); this.SUBF();    Debug.Assert(this.Cycles >= 5); break;   // SUBF indexed
                case 0xE1: this.IndexedByte(); this.CMPF();    Debug.Assert(this.Cycles >= 5); break;   // CMPF indexed
                case 0xE6: this.IndexedByte(); this.LDF();     Debug.Assert(this.Cycles >= 4); break;   // LDF indexed
                case 0xE7: this.IndexedAddress(); this.STF();  Debug.Assert(this.Cycles >= 4); break;   // STF indexed
                case 0xEB: this.IndexedByte(); this.ADDF();    Debug.Assert(this.Cycles >= 5); break;   // ADDF indexed

                case 0xF0: this.ExtendedByte(); this.SUBF();   Debug.Assert(this.Cycles == 6); break;   // SUBF extended
                case 0xF1: this.ExtendedByte(); this.CMPF();   Debug.Assert(this.Cycles == 6); break;   // CMPF extended
                case 0xF6: this.ExtendedByte(); this.LDF();    Debug.Assert(this.Cycles == 5); break;   // LDF extended
                case 0xF7: this.ExtendedAddress(); this.STF(); Debug.Assert(this.Cycles == 5); break;   // STF extended
                case 0xFB: this.ExtendedByte(); this.ADDF();   Debug.Assert(this.Cycles == 6); break;   // ADDF extended

                // Extended arithmetic

                case 0x8D: this.ImmediateByte(); this.DIVD(); break;        // DIVD immediate
                case 0x9D: this.DirectByte(); this.DIVD(); break;           // DIVD direct
                case 0xAD: this.IndexedByte(); this.DIVD(); break;          // DIVD indexed
                case 0xBD: this.ExtendedByte(); this.DIVD(); break;         // DIVD extended

                case 0x8E: this.ImmediateShort(); this.MULD(); break;        // MULD immediate
                case 0x9E: this.DirectShort(); this.MULD(); break;           // MULD direct
                case 0xAE: this.IndexedShort(); this.MULD(); break;          // MULD indexed
                case 0xBE: this.ExtendedShort(); this.MULD(); break;         // MULD extended

                default:
                    base.Execute11();
                    break;
            }
        }

        #endregion

        #region OIM / AIM / EIM / TIM - immediate-with-memory operations

        private void OIM_Direct()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.DirectByte();
            this.MemoryWrite(this.EA, this.Or(mask));
        }

        private void OIM_Indexed()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.IndexedByte();
            this.MemoryWrite(this.EA, this.Or(mask));
        }

        private void OIM_Extended()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.ExtendedByte();
            this.MemoryWrite(this.EA, this.Or(mask));
        }

        private void AIM_Direct()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.DirectByte();
            this.MemoryWrite(this.EA, this.And(mask));
        }

        private void AIM_Indexed()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.IndexedByte();
            this.MemoryWrite(this.EA, this.And(mask));
        }

        private void AIM_Extended()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.ExtendedByte();
            this.MemoryWrite(this.EA, this.And(mask));
        }

        private void EIM_Direct()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.DirectByte();
            this.MemoryWrite(this.EA, this.ExclusiveOr(mask));
        }

        private void EIM_Indexed()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.IndexedByte();
            this.MemoryWrite(this.EA, this.ExclusiveOr(mask));
        }

        private void EIM_Extended()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.ExtendedByte();
            this.MemoryWrite(this.EA, this.ExclusiveOr(mask));
        }

        private void TIM_Direct()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.DirectByte();
            this.And(mask);
            this.SwallowRead(2);
        }

        private void TIM_Indexed()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.IndexedByte();
            this.And(mask);
            this.SwallowRead(2);
        }

        private void TIM_Extended()
        {
            this.FetchByte();
            var mask = this.Bus.Data;
            this.ExtendedByte();
            this.And(mask);
            this.SwallowRead(2);
        }

        #endregion

        #region Register-to-register operations

        private void ADDR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Add(destination, source, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.Add(destination, source);
            }
        }

        private void ADCR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Add(destination, source, this.CarryFlag, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.Add(destination, source, this.CarryFlag);
            }
        }

        private void SUBR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Subtract(destination, source, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.Subtract(destination, source);
            }
        }

        private void SBCR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Subtract(destination, source, this.CarryFlag, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.Subtract(destination, source, this.CarryFlag);
            }
        }

        private void ANDR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.And(destination, source, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.And(destination, source);
            }
        }

        private void ORR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Or(destination, source, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.Or(destination, source);
            }
        }

        private void EORR()
        {
            var post = this.Bus.Data;
            var sourceSpecification = HighNibble(post);
            var destinationSpecification = LowNibble(post);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.ExclusiveOr(destination, source, destination);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                destination = this.ExclusiveOr(destination, source);
            }
        }

        private void CMPR()
        {
            var immediate = this.Bus.Data;
            var sourceSpecification = HighNibble(immediate);
            var destinationSpecification = LowNibble(immediate);
            this.SwallowRead(2);
            var sourceType = sourceSpecification & 0x8;
            var destinationType = destinationSpecification & 0x8;
            if (sourceType == 0 && destinationType == 0)
            {
                var source = this.ReferenceTransfer16(sourceSpecification);
                var destination = this.ReferenceTransfer16(destinationSpecification);
                this.Compare(destination, source);
            }
            else if (sourceType != 0 && destinationType != 0)
            {
                ref var source = ref this.ReferenceTransfer8(sourceSpecification);
                ref var destination = ref this.ReferenceTransfer8(destinationSpecification);
                this.Compare(destination, source);
            }
        }

        #endregion

        #region W register stack operations

        private void PSHSW()
        {
            this.SwallowRead(3);
            this.Push(this.S, this.W);
        }

        private void PULSW()
        {
            this.SwallowRead(3);
            this.W.Assign(this.PopWord(this.S));
        }

        private void PSHUW()
        {
            this.SwallowRead(3);
            this.Push(this.U, this.W);
        }

        private void PULUW()
        {
            this.SwallowRead(3);
            this.W.Assign(this.PopWord(this.U));
        }

        #endregion

        #region E register operations

        private void NEGE() => this.E = this.Negate(this.E);
        private void NEGF() => this.F = this.Negate(this.F);
        private void COME() => this.E = this.Complement(this.E);
        private void LSRE() => this.E = this.LogicalShiftRight(this.E);
        private void RORE() => this.E = this.RotateRight(this.E);
        private void ASRE() => this.E = this.ArithmeticShiftRight(this.E);
        private void ASLE() => this.E = this.ArithmeticShiftLeft(this.E);
        private void ROLE() => this.E = this.RotateLeft(this.E);
        private void DECE() => this.E = this.Decrement(this.E);
        private void INCE() => this.E = this.Increment(this.E);
        private void TSTE() => this.Through(this.E);
        private void CLRE() => this.E = this.Clear();

        private void LDE()  => this.E = this.Through(this.Bus.Data);
        private void STE()  => this.MemoryWrite(this.EA, this.Through(this.E));
        private void ADDE() => this.E = this.Add(this.E, this.Bus.Data);
        private void SUBE() => this.E = this.Subtract(this.E, this.Bus.Data);
        private void CMPE() => this.Compare(this.E, this.Bus.Data);

        #endregion

        #region F register operations

        private void COMF() => this.F = this.Complement(this.F);
        private void LSRF() => this.F = this.LogicalShiftRight(this.F);
        private void RORF() => this.F = this.RotateRight(this.F);
        private void ASRF() => this.F = this.ArithmeticShiftRight(this.F);
        private void ASLF() => this.F = this.ArithmeticShiftLeft(this.F);
        private void ROLF() => this.F = this.RotateLeft(this.F);
        private void DECF() => this.F = this.Decrement(this.F);
        private void INCF() => this.F = this.Increment(this.F);
        private void TSTF() => this.Through(this.F);
        private void CLRF() => this.F = this.Clear();

        private void LDF()  => this.F = this.Through(this.Bus.Data);
        private void STF()  => this.MemoryWrite(this.EA, this.Through(this.F));
        private void ADDF() => this.F = this.Add(this.F, this.Bus.Data);
        private void SUBF() => this.F = this.Subtract(this.F, this.Bus.Data);
        private void CMPF() => this.Compare(this.F, this.Bus.Data);

        #endregion

        #region W register operations

        private void LDW()
        {
            this.W.Assign(this.Through(this.Intermediate));
        }

        private void STW()
        {
            this.SetShort(this.EA, this.Through(this.W));
        }

        private void CMPW()
        {
            this.Subtract(this.W, this.Intermediate, this.Intermediate);
        }

        private void ADDW()
        {
            this.Add(this.W, this.Intermediate, this.W);
        }

        private void SUBW()
        {
            this.Subtract(this.W, this.Intermediate, this.W);
        }

        #endregion

        #region Q register operations

        private void LDQ_Immediate()
        {
            this.FetchInto(this.D);
            this.FetchInto(this.W);
            this.Through(this.Q != 0 ? (byte)1 : (byte)0);
        }

        private void LDQ_Lower()
        {
            // Intermediate holds upper word (D); read the lower word (W) from the next address
            this.D.Assign(this.Intermediate);
            this.Bus.Address.Joined += 2;
            this.GetInto(this.W);
            this.Through(this.Q != 0 ? (byte)1 : (byte)0);
        }

        private void STQ()
        {
            // Write D then W to consecutive memory locations
            this.SetShort(this.EA, this.D);
            this.Bus.Address.Joined += 2;
            this.SetShort(this.W);
            this.Through(this.D.NonZero || this.W.NonZero ? (byte)1 : (byte)0);
        }

        #endregion

        #region Bit manipulation instructions

        // postbyte: bits 7:6 = register (CC=00, A=01, B=10), bits 5:3 = destination bit, bits 2:0 = source bit
        // followed by a direct-page address

        private void BAND()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d & s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void BIAND()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d & ~s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void BOR()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d | s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void BIOR()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d | ~s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void BEOR()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d ^ s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void BIEOR()
        {
            var immediate = this.Bus.Data;
            this.DirectByte();
            var result = this.BitTransfer(immediate, this.Bus.Data, (s, d) => (byte)(d ^ ~s));
            this.ApplyBitTransfer(immediate, result);
        }

        private void LDBT()
        {
            // Load a single bit from memory into a register bit
            var immediate = this.Bus.Data;
            this.DirectByte();
            var memBit    = (immediate >> 0) & 0x07;
            var regBit    = (immediate >> 3) & 0x07;
            var regSelect = (immediate >> 6) & 0x03;
            var memVal    = (this.Bus.Data >> memBit) & 1;
            ref byte reg  = ref this.BitRegister(regSelect);
            if (memVal != 0)
                reg |= (byte)(1 << regBit);
            else
                reg &= (byte)~(1 << regBit);
        }

        private void STBT()
        {
            // Store a single register bit into a memory bit
            var immediate  = this.Bus.Data;
            this.DirectAddress();
            var memBit    = (immediate >> 0) & 0x07;
            var regBit    = (immediate >> 3) & 0x07;
            var regSelect = (immediate >> 6) & 0x03;
            ref byte reg  = ref this.BitRegister(regSelect);
            var regVal    = (reg >> regBit) & 1;
            this.MemoryRead(this.EA);
            byte mem      = this.Bus.Data;
            if (regVal != 0)
                mem |= (byte)(1 << memBit);
            else
                mem &= (byte)~(1 << memBit);
            this.MemoryWrite(this.EA, mem);
        }

        private ref byte BitRegister(int sel)
        {
            switch (sel)
            {
                case 1: return ref this.A;
                case 2: return ref this.B;
                default: return ref this.CC;
            }
        }

        // Returns the new value of the destination bit in position 0
        private byte BitTransfer(byte postbyte, byte memByte, Func<byte, byte, byte> op)
        {
            var srcBit    = (postbyte >> 0) & 0x07;
            var dstBit    = (postbyte >> 3) & 0x07;
            var regSelect = (postbyte >> 6) & 0x03;
            ref byte reg  = ref this.BitRegister(regSelect);
            var s = (byte)((memByte >> srcBit) & 1);
            var d = (byte)((reg    >> dstBit) & 1);
            return op(s, d);
        }

        private void ApplyBitTransfer(byte postbyte, byte bitValue)
        {
            var dstBit    = (postbyte >> 3) & 0x07;
            var regSelect = (postbyte >> 6) & 0x03;
            ref byte reg  = ref this.BitRegister(regSelect);
            if ((bitValue & 1) != 0)
                reg |= (byte)(1 << dstBit);
            else
                reg &= (byte)~(1 << dstBit);
        }

        #endregion

        #region Block transfer (TFM)

        // TFM register specifiers: D=0, X=1, Y=2, U=3, S=4
        private Register16 TFMRegister(int spec)
        {
            return spec switch
            {
                0x0000 => this.D,
                0x0001 => this.X,
                0x0010 => this.Y,
                0x0011 => this.U,
                0x0100 => this.S,
                _ => throw new ArgumentOutOfRangeException(nameof(spec), spec, "Invalid TFM register specifier"),
            };
        }

        // TFM R0+,R1+: transfer bytes from R0 to R1, incrementing both
        private void TFM_IncrBoth()
        {
            var immediate = this.Bus.Data;

            this.SwallowRead(6);
            while (this.W.NonZero)
            {
                var sourceSpecifier = HighNibble(immediate);
                var source = this.TFMRegister(sourceSpecifier);
                this.MemoryRead(source);
                source.Increment();

                var destinationSpecifier = LowNibble(immediate);
                var destination = this.TFMRegister(destinationSpecifier);
                this.MemoryWrite(destination, this.Bus.Data);
                destination.Increment();

                this.W.Decrement();
                
                this.SwallowRead(3);
            }
        }

        // TFM R0-,R1-: transfer bytes from R0 to R1, decrementing both
        private void TFM_DecrBoth()
        {
            var immediate = this.Bus.Data;

            this.SwallowRead(6);
            while (this.W.NonZero)
            {
                var sourceSpecifier = HighNibble(immediate);
                var source = this.TFMRegister(sourceSpecifier);
                this.MemoryRead(source);
                source.Decrement();

                var destinationSpecifier = LowNibble(immediate);
                var destination = this.TFMRegister(destinationSpecifier);
                this.MemoryWrite(destination, this.Bus.Data);
                destination.Decrement();

                this.W.Decrement();

                this.SwallowRead(3);
            }
        }

        // TFM R0+,R1: transfer from incrementing R0 to fixed R1
        private void TFM_IncrSrc()
        {
            var immediate = this.Bus.Data;

            this.SwallowRead(6);
            while (this.W.NonZero)
            {
                var sourceSpecifier = HighNibble(immediate);
                var source = this.TFMRegister(sourceSpecifier);
                this.MemoryRead(source);
                source.Increment();

                var destinationSpecifier = LowNibble(immediate);
                var destination = this.TFMRegister(destinationSpecifier);
                this.MemoryWrite(destination, this.Bus.Data);

                this.W.Decrement();

                this.SwallowRead(3);
            }
        }

        // TFM R0,R1+: transfer from fixed R0 to incrementing R1
        private void TFM_IncrDst()
        {
            var immediate = this.Bus.Data;

            this.SwallowRead(6);
            while (this.W.NonZero)
            {
                var sourceSpecifier = HighNibble(immediate);
                var source = this.TFMRegister(sourceSpecifier);
                this.MemoryRead(source);

                var destinationSpecifier = LowNibble(immediate);
                var destination = this.TFMRegister(destinationSpecifier);
                this.MemoryWrite(destination, this.Bus.Data);
                destination.Increment();

                this.W.Decrement();

                this.SwallowRead(3);
            }
        }

        #endregion

        #region Extended arithmetic (MULD, DIVD)

        private void MULD()
        {
            // Q = D * operand (signed 16x16 → 32-bit)
            this.SwallowRead(26);
            this.Q = (uint)((short)this.D.Joined * (short)this.Intermediate.Joined);
            this.Through(this.D);
            this.CC = ClearBit(this.CC, (byte)StatusBits.CF);
        }

        private void DIVD()
        {
            // D / operand (byte): B = quotient, A = remainder
            this.SwallowRead(22);
            var divisor = (sbyte)this.Bus.Data;
            if (divisor == 0)
            {
                this._md |= MD_DIV0;
                return;
            }
            var dividend = (short)this.D.Joined;
            var quotient  = dividend / divisor;
            var remainder = dividend % divisor;
            if (quotient > 127 || quotient < -128)
            {
                this.CC = SetBit(this.CC, (byte)StatusBits.VF);
                return;
            }
            this.B = this.Through((byte)quotient);
            this.A = (byte)remainder;
            this.CC = SetBit(this.CC, (byte)StatusBits.CF, this.B & (byte)Bits.Bit0);
        }

        #endregion

        #region Mode register

        private void LDMD() => this._md = this.Bus.Data;

        #endregion
    }
}
