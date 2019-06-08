namespace EightBit
{
    using System;
    using System.Collections.Generic;

    public sealed class Disassembler
    {
        private ushort address = 0xffff;

        private bool prefix10 = false;
        private bool prefix11 = false;

        public Disassembler(Bus bus, MC6809 targetProcessor)
        {
            this.BUS = bus;
            this.CPU = targetProcessor;
        }

        public bool Pause { get; set;  } = false;

        public bool Ignore => this.CPU.HALT.Lowered()
                                || this.CPU.RESET.Lowered()
                                || this.CPU.NMI.Lowered()
                                || (this.CPU.FIRQ.Lowered() && this.CPU.FastInterruptMasked == 0)
                                || (this.CPU.INT.Lowered() && this.CPU.InterruptMasked == 0);

        private Bus BUS { get; }

        private MC6809 CPU { get; }

        public string Trace(ushort current)
        {
            var disassembled = this.Disassemble(current);
            var cc = this.CPU.CC;
            var a = this.CPU.A;
            var b = this.CPU.B;
            var dp = this.CPU.DP;
            var x = this.CPU.X.Word;
            var y = this.CPU.Y.Word;
            var u = this.CPU.U.Word;
            var s = this.CPU.S.Word;
            return $"{current:x4}|{disassembled}\t\tcc={cc:x2} a={a:x2} b={b:x2} dp={dp:x2} x={x:x4} y={y:x4} u={u:x4} s={s:x4}";
        }

        public string Trace(Register16 current) => this.Trace(current.Word);

        public string Trace() => this.Trace(this.CPU.PC);

        ////private static string Dump_RelativeValue(sbyte value) => value.ToString("D");

        ////private static string Dump_RelativeValue(short value) => value.ToString("D");

        ////private static string Dump_RelativeValue(Register16 value) => Dump_RelativeValue(value);

        public string Disassemble(ushort current)
        {
            this.address = current;
            if (this.prefix10)
            {
                return this.Disassemble10();
            }

            if (this.prefix11)
            {
                return this.Disassemble11();
            }

            return this.DisassembleUnprefixed();
        }

        private string Disassemble(int current) => this.Disassemble((ushort)current);

        ////private string Dump_Flags()
        ////{
        ////    var returned = string.Empty;
        ////    returned += this.CPU.EntireRegisterSet != 0 ? "E" : "-";
        ////    returned += this.CPU.FastInterruptMasked != 0 ? "F" : "-";
        ////    returned += this.CPU.HalfCarry != 0 ? "H" : "-";
        ////    returned += this.CPU.InterruptMasked != 0 ? "I" : "-";
        ////    returned += this.CPU.Negative != 0 ? "N" : "-";
        ////    returned += this.CPU.Zero != 0 ? "Z" : "-";
        ////    returned += this.CPU.Overflow != 0 ? "V" : "-";
        ////    returned += this.CPU.Carry != 0 ? "C" : "-";
        ////    return returned;
        ////}

        ////private string Disassemble(Register16 current) => this.Disassemble(current.Word);

        ////private string Disassemble() => this.Disassemble(this.CPU.PC);

        private string DisassembleUnprefixed()
        {
            var opcode = this.GetByte(this.address);
            var output = $"{opcode:x2}";

            switch (opcode)
            {
                case 0x10: this.prefix10 = true; output += this.Disassemble(this.address + 1); break;
                case 0x11: this.prefix11 = true; output += this.Disassemble(this.address + 1); break;

                // ABX
                case 0x3a: output += "\tABX"; break;                            // ABX (inherent)

                // ADC
                case 0x89: output += this.AM_immediate_byte("ADCA"); break;     // ADC (ADCA immediate)
                case 0x99: output += this.Address_direct("ADCA"); break;        // ADC (ADCA direct)
                case 0xa9: output += this.Address_indexed("ADCA"); break;       // ADC (ADCA indexed)
                case 0xb9: output += this.Address_extended("ADCA"); break;      // ADC (ADCA extended)

                case 0xc9: output += this.AM_immediate_byte("ADCB"); break;     // ADC (ADCB immediate)
                case 0xd9: output += this.Address_direct("ADCB"); break;        // ADC (ADCB direct)
                case 0xe9: output += this.Address_indexed("ADCB"); break;       // ADC (ADCB indexed)
                case 0xf9: output += this.Address_extended("ADCB"); break;      // ADC (ADCB extended)

                // ADD
                case 0x8b: output += this.AM_immediate_byte("ADDA"); break;      // ADD (ADDA immediate)
                case 0x9b: output += this.Address_direct("ADDA"); break;        // ADD (ADDA direct)
                case 0xab: output += this.Address_indexed("ADDA"); break;       // ADD (ADDA indexed)
                case 0xbb: output += this.Address_extended("ADDA"); break;      // ADD (ADDA extended)

                case 0xcb: output += this.AM_immediate_byte("ADDB"); break;      // ADD (ADDB immediate)
                case 0xdb: output += this.Address_direct("ADDB"); break;        // ADD (ADDB direct)
                case 0xeb: output += this.Address_indexed("ADDB"); break;       // ADD (ADDB indexed)
                case 0xfb: output += this.Address_extended("ADDB"); break;      // ADD (ADDB extended)

                case 0xc3: output += this.AM_immediate_word("ADDD"); break;     // ADD (ADDD immediate)
                case 0xd3: output += this.Address_direct("ADDD"); break;        // ADD (ADDD direct)
                case 0xe3: output += this.Address_indexed("ADDD"); break;       // ADD (ADDD indexed)
                case 0xf3: output += this.Address_extended("ADDD"); break;      // ADD (ADDD extended)

                // AND
                case 0x84: output += this.AM_immediate_byte("ANDA"); break;     // AND (ANDA immediate)
                case 0x94: output += this.Address_direct("ANDA"); break;        // AND (ANDA direct)
                case 0xa4: output += this.Address_indexed("ANDA"); break;       // AND (ANDA indexed)
                case 0xb4: output += this.Address_extended("ANDA"); break;      // AND (ANDA extended)

                case 0xc4: output += this.AM_immediate_byte("ANDB"); break;     // AND (ANDB immediate)
                case 0xd4: output += this.Address_direct("ANDB"); break;        // AND (ANDB direct)
                case 0xe4: output += this.Address_indexed("ANDB"); break;       // AND (ANDB indexed)
                case 0xf4: output += this.Address_extended("ANDB"); break;      // AND (ANDB extended)

                case 0x1c: output += this.AM_immediate_byte("ANDCC"); break;    // AND (ANDCC immediate)

                // ASL/LSL
                case 0x08: output += this.Address_direct("ASL"); break;         // ASL (direct)
                case 0x48: output += "\tASLA\t"; break;                         // ASL (ASLA inherent)
                case 0x58: output += "\tASLB\t"; break;                         // ASL (ASLB inherent)
                case 0x68: output += this.Address_indexed("ASL"); break;        // ASL (indexed)
                case 0x78: output += this.Address_extended("ASL"); break;       // ASL (extended)

                // ASR
                case 0x07: output += this.Address_direct("ASR"); break;         // ASR (direct)
                case 0x47: output += "\tASRA\t"; break;                         // ASR (ASRA inherent)
                case 0x57: output += "\tASRB\t"; break;                         // ASR (ASRB inherent)
                case 0x67: output += this.Address_indexed("ASR"); break;        // ASR (indexed)
                case 0x77: output += this.Address_extended("ASR"); break;       // ASR (extended)

                // BIT
                case 0x85: output += this.AM_immediate_byte("BITA"); break;     // BIT (BITA immediate)
                case 0x95: output += this.Address_direct("BITA"); break;        // BIT (BITA direct)
                case 0xa5: output += this.Address_indexed("BITA"); break;       // BIT (BITA indexed)
                case 0xb5: output += this.Address_extended("BITA"); break;      // BIT (BITA extended)

                case 0xc5: output += this.AM_immediate_byte("BITB"); break;     // BIT (BITB immediate)
                case 0xd5: output += this.Address_direct("BITB"); break;        // BIT (BITB direct)
                case 0xe5: output += this.Address_indexed("BITB"); break;       // BIT (BITB indexed)
                case 0xf5: output += this.Address_extended("BITB"); break;      // BIT (BITB extended)

                // CLR
                case 0x0f: output += this.Address_direct("CLR"); break;         // CLR (direct)
                case 0x4f: output += "\tCLRA\t"; break;                         // CLR (CLRA implied)
                case 0x5f: output += "\tCLRB\t"; break;                         // CLR (CLRB implied)
                case 0x6f: output += this.Address_indexed("CLR"); break;        // CLR (indexed)
                case 0x7f: output += this.Address_extended("CLR"); break;       // CLR (extended)

                // CMP

                // CMPA
                case 0x81: output += this.AM_immediate_byte("CMPA"); break;     // CMP (CMPA, immediate)
                case 0x91: output += this.Address_direct("CMPA"); break;        // CMP (CMPA, direct)
                case 0xa1: output += this.Address_indexed("CMPA"); break;       // CMP (CMPA, indexed)
                case 0xb1: output += this.Address_extended("CMPA"); break;      // CMP (CMPA, extended)

                // CMPB
                case 0xc1: output += this.AM_immediate_byte("CMPB"); break;     // CMP (CMPB, immediate)
                case 0xd1: output += this.Address_direct("CMPB"); break;        // CMP (CMPB, direct)
                case 0xe1: output += this.Address_indexed("CMPB"); break;       // CMP (CMPB, indexed)
                case 0xf1: output += this.Address_extended("CMPB"); break;      // CMP (CMPB, extended)

                // CMPX
                case 0x8c: output += this.AM_immediate_word("CMPX"); break;     // CMP (CMPX, immediate)
                case 0x9c: output += this.Address_direct("CMPX"); break;        // CMP (CMPX, direct)
                case 0xac: output += this.Address_indexed("CMPX"); break;       // CMP (CMPX, indexed)
                case 0xbc: output += this.Address_extended("CMPX"); break;      // CMP (CMPX, extended)

                // COM
                case 0x03: output += this.Address_direct("COM"); break;         // COM (direct)
                case 0x43: output += "\tCOMA\t"; break;                         // COM (COMA inherent)
                case 0x53: output += "\tCOMB\t"; break;                         // COM (COMB inherent)
                case 0x63: output += this.Address_indexed("COM"); break;        // COM (indexed)
                case 0x73: output += this.Address_extended("COM"); break;       // COM (extended)

                // CWAI
                case 0x3c: output += this.Address_direct("CWAI"); break;        // CWAI (direct)

                // DAA
                case 0x19: output += "\tDAA"; break;        // DAA (inherent)

                // DEC
                case 0x0a: output += this.Address_direct("DEC"); break;         // DEC (direct)
                case 0x4a: output += "\tDECA\t"; break;                         // DEC (DECA inherent)
                case 0x5a: output += "\tDECB\t"; break;                         // DEC (DECB inherent)
                case 0x6a: output += this.Address_indexed("DEC"); break;        // DEC (indexed)
                case 0x7a: output += this.Address_extended("DEC"); break;       // DEC (extended)

                // EOR

                // EORA
                case 0x88: output += this.AM_immediate_byte("EORA"); break;     // EOR (EORA immediate)
                case 0x98: output += this.Address_direct("EORA"); break;        // EOR (EORA direct)
                case 0xa8: output += this.Address_indexed("EORA"); break;       // EOR (EORA indexed)
                case 0xb8: output += this.Address_extended("EORA"); break;      // EOR (EORA extended)

                // EORB
                case 0xc8: output += this.AM_immediate_byte("EORB"); break;     // EOR (EORB immediate)
                case 0xd8: output += this.Address_direct("EORB"); break;        // EOR (EORB direct)
                case 0xe8: output += this.Address_indexed("EORB"); break;       // EOR (EORB indexed)
                case 0xf8: output += this.Address_extended("EORB"); break;      // EOR (EORB extended)

                // EXG
                case 0x1e: output += this.TFR("EXG"); break;                    // EXG (R1,R2 immediate)

                // INC
                case 0x0c: output += this.Address_direct("INC"); break;         // INC (direct)
                case 0x4c: output += "\tINCA\t"; break;                         // INC (INCA inherent)
                case 0x5c: output += "\tINCB\t"; break;                         // INC (INCB inherent)
                case 0x6c: output += this.Address_indexed("INC"); break;        // INC (indexed)
                case 0x7c: output += this.Address_extended("INC"); break;       // INC (extended)

                       // JMP
                case 0x0e: output += this.Address_direct("JMP"); break;         // JMP (direct)
                case 0x6e: output += this.Address_indexed("JMP"); break;        // JMP (indexed)
                case 0x7e: output += this.Address_extended("JMP"); break;       // JMP (extended)

                // JSR
                case 0x9d: output += this.Address_direct("JSR"); break;         // JSR (direct)
                case 0xad: output += this.Address_indexed("JSR"); break;        // JSR (indexed)
                case 0xbd: output += this.Address_extended("JSR"); break;       // JSR (extended)

                // LD

                // LDA
                case 0x86: output += this.AM_immediate_byte("LDA"); break;      // LD (LDA immediate)
                case 0x96: output += this.Address_direct("LDA"); break;         // LD (LDA direct)
                case 0xa6: output += this.Address_indexed("LDA"); break;        // LD (LDA indexed)
                case 0xb6: output += this.Address_extended("LDA"); break;       // LD (LDA extended)

                // LDB
                case 0xc6: output += this.AM_immediate_byte("LDB"); break;      // LD (LDB immediate)
                case 0xd6: output += this.Address_direct("LDB"); break;         // LD (LDB direct)
                case 0xe6: output += this.Address_indexed("LDB"); break;        // LD (LDB indexed)
                case 0xf6: output += this.Address_extended("LDB"); break;       // LD (LDB extended)

                // LDD
                case 0xcc: output += this.AM_immediate_word("LDD"); break;      // LD (LDD immediate)
                case 0xdc: output += this.Address_direct("LDD"); break;         // LD (LDD direct)
                case 0xec: output += this.Address_indexed("LDD"); break;        // LD (LDD indexed)
                case 0xfc: output += this.Address_extended("LDD"); break;       // LD (LDD extended)

                // LDU
                case 0xce: output += this.AM_immediate_word("LDU"); break;      // LD (LDU immediate)
                case 0xde: output += this.Address_direct("LDU"); break;         // LD (LDU direct)
                case 0xee: output += this.Address_indexed("LDU"); break;        // LD (LDU indexed)
                case 0xfe: output += this.Address_extended("LDU"); break;       // LD (LDU extended)

                // LDX
                case 0x8e: output += this.AM_immediate_word("LDX"); break;      // LD (LDX immediate)
                case 0x9e: output += this.Address_direct("LDX"); break;         // LD (LDX direct)
                case 0xae: output += this.Address_indexed("LDX"); break;        // LD (LDX indexed)
                case 0xbe: output += this.Address_extended("LDX"); break;       // LD (LDX extended)

                // LEA
                case 0x30: output += this.Address_indexed("LEAX"); break;       // LEA (LEAX indexed)
                case 0x31: output += this.Address_indexed("LEAY"); break;       // LEA (LEAY indexed)
                case 0x32: output += this.Address_indexed("LEAS"); break;       // LEA (LEAS indexed)
                case 0x33: output += this.Address_indexed("LEAU"); break;       // LEA (LEAU indexed)

                // LSR
                case 0x04: output += this.Address_direct("LSR"); break;         // LSR (direct)
                case 0x44: output += "\tLSRA\t"; break;                         // LSR (LSRA inherent)
                case 0x54: output += "\tLSRB\t"; break;                         // LSR (LSRB inherent)
                case 0x64: output += this.Address_indexed("LSR"); break;        // LSR (indexed)
                case 0x74: output += this.Address_extended("LSR"); break;       // LSR (extended)

                // MUL
                case 0x3d: output += "\tMUL\t"; break;                          // MUL (inherent)

                // NEG
                case 0x00: output += this.Address_direct("NEG"); break;         // NEG (direct)
                case 0x40: output += "\tNEGA\t"; break;                         // NEG (NEGA, inherent)
                case 0x50: output += "\tNEGB\t"; break;                         // NEG (NEGB, inherent)
                case 0x60: output += this.Address_indexed("NEG"); break;        // NEG (indexed)
                case 0x70: output += this.Address_extended("NEG"); break;       // NEG (extended)

                // NOP
                case 0x12: output += "\tNOP\t"; break;                          // NOP (inherent)

                // OR

                // ORA
                case 0x8a: output += this.AM_immediate_byte("ORA"); break;      // OR (ORA immediate)
                case 0x9a: output += this.Address_direct("ORA"); break;         // OR (ORA direct)
                case 0xaa: output += this.Address_indexed("ORA"); break;        // OR (ORA indexed)
                case 0xba: output += this.Address_extended("ORA"); break;       // OR (ORA extended)

                // ORB
                case 0xca: output += this.AM_immediate_byte("ORB"); break;      // OR (ORB immediate)
                case 0xda: output += this.Address_direct("ORB"); break;         // OR (ORB direct)
                case 0xea: output += this.Address_indexed("ORB"); break;        // OR (ORB indexed)
                case 0xfa: output += this.Address_extended("ORB"); break;       // OR (ORB extended)

                // ORCC
                case 0x1a: output += this.AM_immediate_byte("ORCC"); break;     // OR (ORCC immediate)

                // PSH
                case 0x34: output += this.PshS(); break;                        // PSH (PSHS immediate)
                case 0x36: output += this.PshU(); break;                        // PSH (PSHU immediate)

                // PUL
                case 0x35: output += this.PulS(); break;                        // PUL (PULS immediate)
                case 0x37: output += this.PulU(); break;                        // PUL (PULU immediate)

                // ROL
                case 0x09: output += this.Address_direct("ROL"); break;         // ROL (direct)
                case 0x49: output += "\tROLA\t"; break;                         // ROL (ROLA inherent)
                case 0x59: output += "\tROLB\t"; break;                         // ROL (ROLB inherent)
                case 0x69: output += this.Address_indexed("ROL"); break;        // ROL (indexed)
                case 0x79: output += this.Address_extended("ROL"); break;       // ROL (extended)

                // ROR
                case 0x06: output += this.Address_direct("ROR"); break;         // ROR (direct)
                case 0x46: output += "\tRORA\t"; break;                         // ROR (RORA inherent)
                case 0x56: output += "\tRORB\t"; break;                         // ROR (RORB inherent)
                case 0x66: output += this.Address_indexed("ROR"); break;        // ROR (indexed)
                case 0x76: output += this.Address_extended("ROR"); break;       // ROR (extended)

                // RTI
                case 0x3B: output += "\tRTI\t"; break;                          // RTI (inherent)

                // RTS
                case 0x39: output += "\tRTS\t"; break;                          // RTS (inherent)

                // SBC

                // SBCA
                case 0x82: output += this.AM_immediate_byte("SBCA"); break;     // SBC (SBCA immediate)
                case 0x92: output += this.Address_direct("SBCA"); break;        // SBC (SBCA direct)
                case 0xa2: output += this.Address_indexed("SBCA"); break;       // SBC (SBCA indexed)
                case 0xb2: output += this.Address_extended("SBCA"); break;      // SBC (SBCA extended)

                // SBCB
                case 0xc2: output += this.AM_immediate_byte("SBCB"); break;     // SBC (SBCB immediate)
                case 0xd2: output += this.Address_direct("SBCB"); break;        // SBC (SBCB direct)
                case 0xe2: output += this.Address_indexed("SBCB"); break;       // SBC (SBCB indexed)
                case 0xf2: output += this.Address_extended("SBCB"); break;      // SBC (SBCB extended)

                // SEX
                case 0x1d: output += "\tSEX\t"; break;                          // SEX (inherent)

                // ST

                // STA
                case 0x97: output += this.Address_direct("STA"); break;         // ST (STA direct)
                case 0xa7: output += this.Address_indexed("STA"); break;        // ST (STA indexed)
                case 0xb7: output += this.Address_extended("STA"); break;       // ST (STA extended)

                // STB
                case 0xd7: output += this.Address_direct("STB"); break;         // ST (STB direct)
                case 0xe7: output += this.Address_indexed("STB"); break;        // ST (STB indexed)
                case 0xf7: output += this.Address_extended("STB"); break;       // ST (STB extended)

                // STD
                case 0xdd: output += this.Address_direct("STD"); break;         // ST (STD direct)
                case 0xed: output += this.Address_indexed("STD"); break;        // ST (STD indexed)
                case 0xfd: output += this.Address_extended("STD"); break;       // ST (STD extended)

                // STU
                case 0xdf: output += this.Address_direct("STU"); break;         // ST (STU direct)
                case 0xef: output += this.Address_indexed("STU"); break;        // ST (STU indexed)
                case 0xff: output += this.Address_extended("STU"); break;       // ST (STU extended)

                // STX
                case 0x9f: output += this.Address_direct("STX"); break;         // ST (STX direct)
                case 0xaf: output += this.Address_indexed("STX"); break;        // ST (STX indexed)
                case 0xbf: output += this.Address_extended("STX"); break;       // ST (STX extended)

                // SUB

                // SUBA
                case 0x80: output += this.AM_immediate_byte("SUBA"); break;     // SUB (SUBA immediate)
                case 0x90: output += this.Address_direct("SUBA"); break;        // SUB (SUBA direct)
                case 0xa0: output += this.Address_indexed("SUBA"); break;       // SUB (SUBA indexed)
                case 0xb0: output += this.Address_extended("SUBA"); break;      // SUB (SUBA extended)

                // SUBB
                case 0xc0: output += this.AM_immediate_byte("SUBB"); break;     // SUB (SUBB immediate)
                case 0xd0: output += this.Address_direct("SUBB"); break;        // SUB (SUBB direct)
                case 0xe0: output += this.Address_indexed("SUBB"); break;       // SUB (SUBB indexed)
                case 0xf0: output += this.Address_extended("SUBB"); break;      // SUB (SUBB extended)

                // SUBD
                case 0x83: output += this.AM_immediate_word("SUBD"); break;     // SUB (SUBD immediate)
                case 0x93: output += this.Address_direct("SUBD"); break;        // SUB (SUBD direct)
                case 0xa3: output += this.Address_indexed("SUBD"); break;       // SUB (SUBD indexed)
                case 0xb3: output += this.Address_extended("SUBD"); break;      // SUB (SUBD extended)

                // SWI
                case 0x3f: output += "\tSWI\t"; break;                          // SWI (inherent)

                // SYNC
                case 0x13: output += "\tSYNC\t"; break;                         // SYNC (inherent)

                // TFR
                case 0x1f: output += this.TFR("TFR"); break;                    // TFR (immediate)

                // TST
                case 0x0d: output += this.Address_direct("TST"); break;         // TST (direct)
                case 0x4d: output += "\tTSTA\t"; break;                         // TST (TSTA inherent)
                case 0x5d: output += "\tTSTB\t"; break;                         // TST (TSTB inherent)
                case 0x6d: output += this.Address_indexed("TST"); break;        // TST (indexed)
                case 0x7d: output += this.Address_extended("TST"); break;       // TST (extended)

                // Branching

                case 0x16: output += this.BranchLong("LBRA"); break;            // BRA (LBRA relative)
                case 0x17: output += this.BranchLong("LBSR"); break;            // BSR (LBSR relative)
                case 0x20: output += this.BranchShort("BRA"); break;            // BRA (relative)
                case 0x21: output += this.BranchShort("BRN"); break;            // BRN (relative)
                case 0x22: output += this.BranchShort("BHI"); break;            // BHI (relative)
                case 0x23: output += this.BranchShort("BLS"); break;            // BLS (relative)
                case 0x24: output += this.BranchShort("BCC"); break;            // BCC (relative)
                case 0x25: output += this.BranchShort("BCS"); break;            // BCS (relative)
                case 0x26: output += this.BranchShort("BNE"); break;            // BNE (relative)
                case 0x27: output += this.BranchShort("BEQ"); break;            // BEQ (relative)
                case 0x28: output += this.BranchShort("BVC"); break;            // BVC (relative)
                case 0x29: output += this.BranchShort("BVS"); break;            // BVS (relative)
                case 0x2a: output += this.BranchShort("BPL"); break;            // BPL (relative)
                case 0x2b: output += this.BranchShort("BMI"); break;            // BMI (relative)
                case 0x2c: output += this.BranchShort("BGE"); break;            // BGE (relative)
                case 0x2d: output += this.BranchShort("BLT"); break;            // BLT (relative)
                case 0x2e: output += this.BranchShort("BGT"); break;            // BGT (relative)
                case 0x2f: output += this.BranchShort("BLE"); break;            // BLE (relative)

                case 0x8d: output += this.BranchShort("BSR"); break;            // BSR (relative)

                default:
                    throw new InvalidOperationException("Unknown opcode");
            }

            return output;
        }

        private string Disassemble10()
        {
            var opcode = this.GetByte(this.address);
            var output = $"{opcode:x2}";

            switch (opcode)
            {
                // CMP

                // CMPD
                case 0x83: output += this.AM_immediate_word("CMPD"); break;     // CMP (CMPD, immediate)
                case 0x93: output += this.Address_direct("CMPD"); break;        // CMP (CMPD, direct)
                case 0xa3: output += this.Address_indexed("CMPD"); break;       // CMP (CMPD, indexed)
                case 0xb3: output += this.Address_extended("CMPD"); break;      // CMP (CMPD, extended)

                // CMPY
                case 0x8c: output += this.AM_immediate_word("CMPY"); break;     // CMP (CMPY, immediate)
                case 0x9c: output += this.Address_direct("CMPY"); break;        // CMP (CMPY, direct)
                case 0xac: output += this.Address_indexed("CMPY"); break;       // CMP (CMPY, indexed)
                case 0xbc: output += this.Address_extended("CMPY"); break;      // CMP (CMPY, extended)

                // LD

                // LDS
                case 0xce: output += this.AM_immediate_word("LDS"); break;      // LD (LDS immediate)
                case 0xde: output += this.Address_direct("LDS"); break;         // LD (LDS direct)
                case 0xee: output += this.Address_indexed("LDS"); break;        // LD (LDS indexed)
                case 0xfe: output += this.Address_extended("LDS"); break;       // LD (LDS extended)

                // LDY
                case 0x8e: output += this.AM_immediate_word("LDY"); break;      // LD (LDY immediate)
                case 0x9e: output += this.Address_direct("LDY"); break;         // LD (LDY direct)
                case 0xae: output += this.Address_indexed("LDY"); break;        // LD (LDY indexed)
                case 0xbe: output += this.Address_extended("LDY"); break;       // LD (LDY extended)

                // Branching

                case 0x21: output += this.BranchLong("LBRN"); break;            // BRN (LBRN relative)
                case 0x22: output += this.BranchLong("LBHI"); break;            // BHI (LBHI relative)
                case 0x23: output += this.BranchLong("LBLS"); break;            // BLS (LBLS relative)
                case 0x24: output += this.BranchLong("LBCC"); break;            // BCC (LBCC relative)
                case 0x25: output += this.BranchLong("LBCS"); break;            // BCS (LBCS relative)
                case 0x26: output += this.BranchLong("LBNE"); break;            // BNE (LBNE relative)
                case 0x27: output += this.BranchLong("LBEQ"); break;            // BEQ (LBEQ relative)
                case 0x28: output += this.BranchLong("LBVC"); break;            // BVC (LBVC relative)
                case 0x29: output += this.BranchLong("LBVS"); break;            // BVS (LBVS relative)
                case 0x2a: output += this.BranchLong("LBPL"); break;            // BPL (LBPL relative)
                case 0x2b: output += this.BranchLong("LBMI"); break;            // BMI (LBMI relative)
                case 0x2c: output += this.BranchLong("LBGE"); break;            // BGE (LBGE relative)
                case 0x2d: output += this.BranchLong("LBLT"); break;            // BLT (LBLT relative)
                case 0x2e: output += this.BranchLong("LBGT"); break;            // BGT (LBGT relative)
                case 0x2f: output += this.BranchLong("LBLE"); break;            // BLE (LBLE relative)

                // STS
                case 0xdf: output += this.Address_direct("STS"); break;         // ST (STS direct)
                case 0xef: output += this.Address_indexed("STS"); break;        // ST (STS indexed)
                case 0xff: output += this.Address_extended("STS"); break;       // ST (STS extended)

                // STY
                case 0x9f: output += this.Address_direct("STY"); break;       // ST (STY direct)
                case 0xaf: output += this.Address_indexed("STY"); break;        // ST (STY indexed)
                case 0xbf: output += this.Address_extended("STY"); break;       // ST (STY extended)

                // SWI
                case 0x3f: output += "\tSWI2\t"; break;                         // SWI (SWI2 inherent)

                default:
                    throw new InvalidOperationException("Unknown group 10 opcode");
            }

            this.prefix10 = false;

            return output;
        }

        private string Disassemble11()
        {
            var opcode = this.GetByte(this.address);
            var output = $"{opcode:x2}";

            switch (opcode)
            {
                // CMP

                // CMPU
                case 0x83: output += this.AM_immediate_word("CMPU"); break;     // CMP (CMPU, immediate)
                case 0x93: output += this.Address_direct("CMPU"); break;        // CMP (CMPU, direct)
                case 0xa3: output += this.Address_indexed("CMPU"); break;       // CMP (CMPU, indexed)
                case 0xb3: output += this.Address_extended("CMPU"); break;      // CMP (CMPU, extended)

                // CMPS
                case 0x8c: output += this.AM_immediate_word("CMPS"); break;     // CMP (CMPS, immediate)
                case 0x9c: output += this.Address_direct("CMPS"); break;        // CMP (CMPS, direct)
                case 0xac: output += this.Address_indexed("CMPS"); break;       // CMP (CMPS, indexed)
                case 0xbc: output += this.Address_extended("CMPS"); break;      // CMP (CMPS, extended)

                // SWI
                case 0x3f: output += "\tSWI3\t"; break;                         // SWI (SWI3 inherent)

                default:
                    throw new InvalidOperationException("Unknown group 11 opcode");
            }

            this.prefix11 = false;

            return output;
        }

        //

        private static string RR(int which)
        {
            switch (which)
            {
                case 0b00:
                    return "X";
                case 0b01:
                    return "Y";
                case 0b10:
                    return "U";
                case 0b11:
                    return "S";
                default:
                    throw new ArgumentOutOfRangeException(nameof(which), which, "Register specification is unknown");
            }
        }

        private static string WrapIndirect(string what, bool indirect)
        {
            var open = indirect ? "[" : "";
            var close = indirect ? "]" : "";
            return $"{open}{what}{close}";
        }

        private string Address_direct(string mnemomic)
        {
            var offset = this.GetByte(++this.address);
            return $"{offset:x2}\t{mnemomic}\t${offset:x2}";
        }

        private string Address_indexed(string mnemomic)
        {
            var type = this.GetByte(++this.address);
            var r = RR((type & (byte)(Bits.Bit6 | Bits.Bit5)) >> 5);

            byte byte8 = 0xff;
            ushort word = 0xffff;

            var output = $"{type:x2}";

            if ((type & (byte)Bits.Bit7) != 0)
            {
                var indirect = (type & (byte)Bits.Bit4) != 0;
                switch (type & (byte)Mask.Mask4)
                {
                    case 0b0000:    // ,R+
                        output += $"\t{mnemomic}\t{WrapIndirect($",{r}+", indirect)}";
                        break;
                    case 0b0001:    // ,R++
                        output += $"\t{mnemomic}\t{WrapIndirect($",{r}++", indirect)}";
                        break;
                    case 0b0010:    // ,-R
                        output += $"\t{mnemomic}\t{WrapIndirect($",-{r}", indirect)}";
                        break;
                    case 0b0011:    // ,--R
                        output += $"\t{mnemomic}\t{WrapIndirect($",--{r}", indirect)}";
                        break;
                    case 0b0100:    // ,R
                        output += $"\t{mnemomic}\t{WrapIndirect($",{r}", indirect)}";
                        break;
                    case 0b0101:    // B,R
                        output += $"\t{mnemomic}\t{WrapIndirect($"B,{r}", indirect)}";
                        break;
                    case 0b0110:    // A,R
                        output += $"\t{mnemomic}\t{WrapIndirect($"A,{r}", indirect)}";
                        break;
                    case 0b1000:    // n,R (eight-bit)
                        byte8 = this.GetByte(++this.address);
                        output += $"{byte8:x2}\t{mnemomic}\t{WrapIndirect($"{byte8:x2},{r}", indirect)}";
                        break;
                    case 0b1001:    // n,R (sixteen-bit)
                        word = this.GetWord(++this.address);
                        output += $"{word:x4}\t{mnemomic}\t{WrapIndirect($"{word:x4},{r}", indirect)}";
                        break;
                    case 0b1011:    // D,R
                        output += $"\t{mnemomic}\t{WrapIndirect($"D,{r}", indirect)}";
                        break;
                    case 0b1100:    // n,PCR (eight-bit)
                        byte8 = this.GetByte(++this.address);
                        output += $"{byte8:x2}\t{mnemomic}\t{WrapIndirect("${(byte)byte8:D},PCR", indirect)}";
                        break;
                    case 0b1101:    // n,PCR (sixteen-bit)
                        word = this.GetWord(++this.address);
                        output += $"{word:x4}\t{mnemomic}\t{WrapIndirect("${(short)word:D},PCR", indirect)}";
                        break;
                    case 0b1111:    // [n]
                        if (!indirect)
                        {
                            throw new InvalidOperationException("Index specification cannot be direct");
                        }

                        word = this.GetWord(++this.address);
                        output += $"{word:x4}\t{mnemomic}\t{WrapIndirect("${word:x4}", indirect)}";
                        break;
                    default:
                        throw new InvalidOperationException("Invalid index specification used");
                }
            }
            else
            {
                // EA = ,R + 5-bit offset
                output += $"\t{mnemomic}\t{Processor.SignExtend(5, type & (byte)Mask.Mask5)},{r}";
            }

            return output;
        }

        private string Address_extended(string mnemomic)
        {
            var word = this.GetWord(++this.address);
            return $"{word:x4}\t{mnemomic}\t${word:x4}";
        }

        private string Address_relative_byte(string mnemomic)
        {
            var byte8 = this.GetByte(++this.address);
            return $"{byte8:x2}\t{mnemomic}\t${++this.address + (sbyte)byte8:x4}";
        }

        private string Address_relative_word(string mnemomic)
        {
            var word = this.GetWord(++this.address);
            return $"{word:x4}\t{mnemomic}\t${++this.address + (short)word:x4}";
        }

        private string AM_immediate_byte(string mnemomic)
        {
            var byte8 = this.GetByte(++this.address);
            return $"{byte8:x2}\t{mnemomic}\t#${byte8:x2}";
        }

        private string AM_immediate_word(string mnemomic)
        {
            var word = this.GetWord(++this.address);
            return $"{word:x4}\t{mnemomic}\t#${word:x4}";
        }

        private string BranchShort(string mnemomic) => this.Address_relative_byte(mnemomic);

        private string BranchLong(string mnemomic) => this.Address_relative_word(mnemomic);

        private static string ReferenceTransfer8(int specifier)
        {
            switch (specifier)
            {
                case 0b1000:
                    return "A";
                case 0b1001:
                    return "B";
                case 0b1010:
                    return "CC";
                case 0b1011:
                    return "DP";
                default:
                    throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "8bit register specification is unknown");
            }
        }

        private static string ReferenceTransfer16(int specifier)
        {
            switch (specifier)
            {
                case 0b0000:
                    return "D";
                case 0b0001:
                    return "X";
                case 0b0010:
                    return "Y";
                case 0b0011:
                    return "U";
                case 0b0100:
                    return "S";
                case 0b0101:
                    return "PC";
                default:
                    throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "16bit register specification is unknown");
            }
        }

        private string TFR(string mnemomic)
        {
            var data = this.GetByte(++this.address);
            var reg1 = Chip.HighNibble(data);
            var reg2 = Chip.LowNibble(data);

            var output = $"{data:x2}\t{mnemomic}\t";

            var type8 = (reg1 & (byte)Bits.Bit3) != 0;   // 8 bit?
            return type8
                ? $"{output}{ReferenceTransfer8(reg1)},{ReferenceTransfer8(reg2)}"
                : $"{output}{ReferenceTransfer16(reg1)},{ReferenceTransfer16(reg2)}";
        }

        //

        private string PulS() => this.PulX("PULS", "U");

        private string PulU() => this.PulX("PULU", "S");

        private string PshS() => this.PshX("PSHS", "U");

        private string PshU() => this.PshX("PSHU", "S");

        private string PulX(string mnemomic, string upon)
        {
            var data = this.GetByte(++this.address);
            var output = $"{data:x2}\t{mnemomic}\t";
            var registers = new List<string>();

            if ((data & (byte)Bits.Bit0) != 0)
            {
                registers.Add("CC");
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                registers.Add("A");
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                registers.Add("B");
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                registers.Add("DP");
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                registers.Add("X");
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                registers.Add("Y");
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                registers.Add(upon);
            }

            if ((data & (byte)Bits.Bit7) != 0)
            {
                registers.Add("PC");
            }

            return output + string.Join(",", registers);
        }

        private string PshX(string mnemomic, string upon)
        {
            var data = this.GetByte(++this.address);
            var output = $"{data:x2}\t{mnemomic}\t";
            var registers = new List<string>();

            if ((data & (byte)Bits.Bit7) != 0)
            {
                registers.Add("PC");
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                registers.Add(upon);
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                registers.Add("Y");
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                registers.Add("X");
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                registers.Add("DP");
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                registers.Add("B");
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                registers.Add("A");
            }

            if ((data & (byte)Bits.Bit0) != 0)
            {
                registers.Add("CC");
            }

            return output + string.Join(",", registers);
        }

        private byte GetByte(ushort absolute) => this.BUS.Peek(absolute);

        private ushort GetWord(ushort absolute) => this.CPU.PeekWord(absolute).Word;
    }
}
