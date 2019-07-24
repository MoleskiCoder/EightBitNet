// <copyright file="Disassembler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    namespace GameBoy
    {
        public enum IoRegister
        {
            Abbreviated,    // FF00 + dd
            Absolute,       // FFdd
            Register,       // C
            Unused,         // Unused!
        }

        public class Disassembler
        {
            private bool prefixCB = false;

            public Disassembler(Bus bus) => this.Bus = bus;

            public Bus Bus { get; }

            public static string AsFlag(byte value, byte flag, string represents) => (value & flag) != 0 ? represents : "-";

            public static string AsFlag(byte value, StatusBits flag, string represents) => AsFlag(value, (byte)flag, represents);

            public static string AsFlag(byte value, Bits flag, string represents) => AsFlag(value, (byte)flag, represents);

            public static string AsFlags(byte value) =>
                       $"{AsFlag(value, StatusBits.ZF, "Z")}"
                     + $"{AsFlag(value, StatusBits.NF, "N")}"
                     + $"{AsFlag(value, StatusBits.HC, "H")}"
                     + $"{AsFlag(value, StatusBits.CF, "C")}"
                     + $"{AsFlag(value, Bits.Bit3, "+")}"
                     + $"{AsFlag(value, Bits.Bit2, "+")}"
                     + $"{AsFlag(value, Bits.Bit1, "+")}"
                     + $"{AsFlag(value, Bits.Bit0, "+")}";

            public static string State(LR35902 cpu)
            {
                var pc = cpu.PC;
                var sp = cpu.SP;

                var a = cpu.A;
                var f = cpu.F;

                var b = cpu.B;
                var c = cpu.C;

                var d = cpu.D;
                var e = cpu.E;

                var h = cpu.H;
                var l = cpu.L;

                return
                      $"PC={pc.Word:x4} SP={sp.Word:x4} "
                    + $"A={a:x2} F={AsFlags(f)} "
                    + $"B={b:x2} C={c:x2} "
                    + $"D={d:x2} E={e:x2} "
                    + $"H={h:x2} L={l:x2}";
            }

            public string Disassemble(LR35902 cpu)
            {
                this.prefixCB = false;
                return this.Disassemble(cpu, cpu.PC.Word);
            }

            private static string RP(int rp)
            {
                switch (rp)
                {
                    case 0:
                        return "BC";
                    case 1:
                        return "DE";
                    case 2:
                        return "HL";
                    case 3:
                        return "SP";
                }

                throw new System.ArgumentOutOfRangeException(nameof(rp));
            }

            private static string RP2(int rp)
            {
                switch (rp)
                {
                    case 0:
                        return "BC";
                    case 1:
                        return "DE";
                    case 2:
                        return "HL";
                    case 3:
                        return "AF";
                }

                throw new System.ArgumentOutOfRangeException(nameof(rp));
            }

            private static string R(int r)
            {
                switch (r)
                {
                    case 0:
                        return "B";
                    case 1:
                        return "C";
                    case 2:
                        return "D";
                    case 3:
                        return "E";
                    case 4:
                        return "H";
                    case 5:
                        return "L";
                    case 6:
                        return "(HL)";
                    case 7:
                        return "A";
                }

                throw new System.ArgumentOutOfRangeException(nameof(r));
            }

            private static string IO(byte value)
            {
                switch (value)
                {
                    // Port/Mode Registers
                    case IoRegisters.P1:
                        return "P1";
                    case IoRegisters.SB:
                        return "SB";
                    case IoRegisters.SC:
                        return "SC";
                    case IoRegisters.DIV:
                        return "DIV";
                    case IoRegisters.TIMA:
                        return "TIMA";
                    case IoRegisters.TMA:
                        return "TMA";
                    case IoRegisters.TAC:
                        return "TAC";

                    // Interrupt Flags
                    case IoRegisters.IF:
                        return "IF";
                    case IoRegisters.IE:
                        return "IE";

                    // LCD Display Registers
                    case IoRegisters.LCDC:
                        return "LCDC";
                    case IoRegisters.STAT:
                        return "STAT";
                    case IoRegisters.SCY:
                        return "SCY";
                    case IoRegisters.SCX:
                        return "SCX";
                    case IoRegisters.LY:
                        return "LY";
                    case IoRegisters.LYC:
                        return "LYC";
                    case IoRegisters.DMA:
                        return "DMA";
                    case IoRegisters.BGP:
                        return "BGP";
                    case IoRegisters.OBP0:
                        return "OBP0";
                    case IoRegisters.OBP1:
                        return "OBP1";
                    case IoRegisters.WY:
                        return "WY";
                    case IoRegisters.WX:
                        return "WX";

                    // Sound Registers
                    case IoRegisters.NR10:
                        return "NR10";
                    case IoRegisters.NR11:
                        return "NR11";
                    case IoRegisters.NR12:
                        return "NR12";
                    case IoRegisters.NR13:
                        return "NR13";
                    case IoRegisters.NR14:
                        return "NR14";
                    case IoRegisters.NR21:
                        return "NR21";
                    case IoRegisters.NR22:
                        return "NR22";
                    case IoRegisters.NR23:
                        return "NR23";
                    case IoRegisters.NR24:
                        return "NR24";
                    case IoRegisters.NR30:
                        return "NR30";
                    case IoRegisters.NR31:
                        return "NR31";
                    case IoRegisters.NR32:
                        return "NR32";
                    case IoRegisters.NR33:
                        return "NR33";
                    case IoRegisters.NR34:
                        return "NR34";
                    case IoRegisters.NR41:
                        return "NR41";
                    case IoRegisters.NR42:
                        return "NR42";
                    case IoRegisters.NR43:
                        return "NR43";
                    case IoRegisters.NR44:
                        return "NR44";
                    case IoRegisters.NR50:
                        return "NR50";
                    case IoRegisters.NR51:
                        return "NR51";
                    case IoRegisters.NR52:
                        return "NR52";

                    case IoRegisters.WAVE_PATTERN_RAM_START:
                        return "WAVE_PATTERN_RAM_START";
                    case IoRegisters.WAVE_PATTERN_RAM_END:
                        return "WAVE_PATTERN_RAM_END";

                    // Boot rom control
                    case IoRegisters.BOOT_DISABLE:
                        return "BOOT_DISABLE";

                    default:
                        return $"{value:x2}";
                }
            }

            private static string CC(int flag)
            {
                switch (flag)
                {
                    case 0:
                        return "NZ";
                    case 1:
                        return "Z";
                    case 2:
                        return "NC";
                    case 3:
                        return "C";
                    case 4:
                        return "PO";
                    case 5:
                        return "PE";
                    case 6:
                        return "P";
                    case 7:
                        return "M";
                }

                throw new System.ArgumentOutOfRangeException(nameof(flag));
            }

            private static string ALU(int which)
            {
                switch (which)
                {
                    case 0: // ADD A,n
                        return "ADD";
                    case 1: // ADC
                        return "ADC";
                    case 2: // SUB n
                        return "SUB";
                    case 3: // SBC A,n
                        return "SBC";
                    case 4: // AND n
                        return "AND";
                    case 5: // XOR n
                        return "XOR";
                    case 6: // OR n
                        return "OR";
                    case 7: // CP n
                        return "CP";
                }

                throw new System.ArgumentOutOfRangeException(nameof(which));
            }

            private static string DisassembleCB(ref string specification, int x, int y, int z)
            {
                var output = string.Empty;
                switch (x)
                {
                    case 0: // rot[y] r[z]
                        switch (y)
                        {
                            case 0:
                                specification = $"RLC {R(z)}";
                                break;
                            case 1:
                                specification = $"RRC {R(z)}";
                                break;
                            case 2:
                                specification = $"RL {R(z)}";
                                break;
                            case 3:
                                specification = $"RR {R(z)}";
                                break;
                            case 4:
                                specification = $"SLA {R(z)}";
                                break;
                            case 5:
                                specification = $"SRA {R(z)}";
                                break;
                            case 6:
                                specification = $"SWAP {R(z)}";
                                break;
                            case 7:
                                specification = $"SRL {R(z)}";
                                break;
                        }

                        break;
                    case 1: // BIT y, r[z]
                        specification = $"BIT {y},{R(z)}";
                        break;
                    case 2: // RES y, r[z]
                        specification = $"RES {y},{R(z)}";
                        break;
                    case 3: // SET y, r[z]
                        specification = $"SET {y},{R(z)}";
                        break;
                }

                return output;
            }

            private string Disassemble(LR35902 cpu, ushort pc)
            {
                var opCode = this.Bus.Peek(pc);

                var decoded = cpu.GetDecodedOpCode(opCode);

                var x = decoded.X;
                var y = decoded.Y;
                var z = decoded.Z;

                var p = decoded.P;
                var q = decoded.Q;

                var immediate = this.Bus.Peek((ushort)(pc + 1));
                var absolute = cpu.PeekWord((ushort)(pc + 1)).Word;
                var displacement = (sbyte)immediate;
                var relative = pc + displacement + 2;
                var indexedImmediate = this.Bus.Peek((ushort)(pc + 1));

                var dumpCount = 0;
                var ioRegister = IoRegister.Unused;

                var output = $"{opCode:x2}";

                var specification = string.Empty;
                if (this.prefixCB)
                {
                    output += DisassembleCB(ref specification, x, y, z);
                }
                else
                {
                    output += this.DisassembleOther(cpu, pc, ref specification, ref dumpCount, ref ioRegister, x, y, z, p, q);
                }

                for (var i = 0; i < dumpCount; ++i)
                {
                    output += $"{this.Bus.Peek((ushort)(pc + i + 1)):x2}";
                }

                output += '\t';
                output += string.Format(specification, (int)immediate, (int)absolute, relative, (int)displacement, indexedImmediate);

                switch (ioRegister)
                {
                    case IoRegister.Abbreviated:
                        output += $"; register {IO(immediate)}";
                        break;
                    case IoRegister.Absolute:
                        output += "; register (Absolute)";
                        break;
                    case IoRegister.Register:
                        output += $"; register C:{IO(cpu.C)}";
                        break;
                    case IoRegister.Unused:
                        break;
                }

                return output;
            }

            private string DisassembleOther(LR35902 cpu, ushort pc, ref string specification, ref int dumpCount, ref IoRegister ioRegister, int x, int y, int z, int p, int q)
            {
                var output = string.Empty;
                switch (x)
                {
                    case 0:
                        switch (z)
                        {
                            case 0: // Relative jumps and assorted ops
                                switch (y)
                                {
                                    case 0: // NOP
                                        specification = "NOP";
                                        break;
                                    case 1: // GB: LD (nn),SP
                                        specification = "LD ({1:X4}H),SP";
                                        dumpCount += 2;
                                        break;
                                    case 2: // GB: STOP
                                        specification = "STOP";
                                        break;
                                    case 3: // JR d
                                        specification = "JR {2:X4}H";
                                        dumpCount++;
                                        break;
                                    default: // JR cc,d
                                        specification = $"JR {CC(y - 4)}" + ",{2:X4}H";
                                        dumpCount++;
                                        break;
                                }

                                break;
                            case 1: // 16-bit load immediate/add
                                switch (q)
                                {
                                    case 0: // LD rp,nn
                                        specification = $"LD {RP(p)}," + "{1:X4}H";
                                        dumpCount += 2;
                                        break;
                                    case 1: // ADD HL,rp
                                        specification = $"ADD HL,{RP(p)}";
                                        break;
                                }

                                break;
                            case 2: // Indirect loading
                                switch (q)
                                {
                                    case 0:
                                        switch (p)
                                        {
                                            case 0: // LD (BC),A
                                                specification = "LD (BC),A";
                                                break;
                                            case 1: // LD (DE),A
                                                specification = "LD (DE),A";
                                                break;
                                            case 2: // GB: LDI (HL),A
                                                specification = "LDI (HL),A";
                                                break;
                                            case 3: // GB: LDD (HL),A
                                                specification = "LDD (HL),A";
                                                break;
                                        }

                                        break;
                                    case 1:
                                        switch (p)
                                        {
                                            case 0: // LD A,(BC)
                                                specification = "LD A,(BC)";
                                                break;
                                            case 1: // LD A,(DE)
                                                specification = "LD A,(DE)";
                                                break;
                                            case 2: // GB: LDI A,(HL)
                                                specification = "LDI A,(HL)";
                                                break;
                                            case 3: // GB: LDD A,(HL)
                                                specification = "LDD A,(HL)";
                                                break;
                                        }

                                        break;
                                }

                                break;
                            case 3: // 16-bit INC/DEC
                                switch (q)
                                {
                                    case 0: // INC rp
                                        specification = $"INC {RP(p)}";
                                        break;
                                    case 1: // DEC rp
                                        specification = $"DEC {RP(p)}";
                                        break;
                                }

                                break;
                            case 4: // 8-bit INC
                                specification = $"INC {R(y)}";
                                break;
                            case 5: // 8-bit DEC
                                specification = $"DEC {R(y)}";
                                break;
                            case 6: // 8-bit load immediate
                                specification = $"LD {R(y)}," + "{0:X2}H";
                                dumpCount++;
                                break;
                            case 7: // Assorted operations on accumulator/flags
                                switch (y)
                                {
                                    case 0:
                                        specification = "RLCA";
                                        break;
                                    case 1:
                                        specification = "RRCA";
                                        break;
                                    case 2:
                                        specification = "RLA";
                                        break;
                                    case 3:
                                        specification = "RRA";
                                        break;
                                    case 4:
                                        specification = "DAA";
                                        break;
                                    case 5:
                                        specification = "CPL";
                                        break;
                                    case 6:
                                        specification = "SCF";
                                        break;
                                    case 7:
                                        specification = "CCF";
                                        break;
                                }

                                break;
                        }

                        break;
                    case 1: // 8-bit loading
                        if (z == 6 && y == 6)
                        {
                            specification = "HALT"; // Exception (replaces LD (HL), (HL))
                        }
                        else
                        {
                            specification = $"LD {R(y)},{R(z)}";
                        }

                        break;
                    case 2: // Operate on accumulator and register/memory location
                        specification = $"{ALU(y)} A,{R(z)}";
                        break;
                    case 3:
                        switch (z)
                        {
                            case 0: // Conditional return
                                switch (y)
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                    case 3:
                                        specification = $"RET {CC(y)}";
                                        break;
                                    case 4:
                                        specification = "LD (FF00H+{1:X2}H),A";
                                        ioRegister = IoRegister.Abbreviated;
                                        dumpCount++;
                                        break;
                                    case 5:
                                        specification = "ADD SP,{4:X4}H";
                                        dumpCount++;
                                        break;
                                    case 6:
                                        specification = "LD A,(FF00H+{1:X2}H)";
                                        ioRegister = IoRegister.Abbreviated;
                                        dumpCount++;
                                        break;
                                    case 7:
                                        specification = "LD HL,SP+{4}";
                                        dumpCount++;
                                        break;
                                }

                                break;
                            case 1: // POP & various ops
                                switch (q)
                                {
                                    case 0: // POP rp2[p]
                                        specification = $"POP {RP2(p)}";
                                        break;
                                    case 1:
                                        switch (p)
                                        {
                                            case 0: // RET
                                                specification = "RET";
                                                break;
                                            case 1: // GB: RETI
                                                specification = "RETI";
                                                break;
                                            case 2: // JP (HL)
                                                specification = "JP (HL)";
                                                break;
                                            case 3: // LD SP,HL
                                                specification = "LD SP,HL";
                                                break;
                                        }

                                        break;
                                }

                                break;
                            case 2: // Conditional jump
                                switch (y)
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                    case 3:
                                        specification = $"JP {CC(y)}" + ",{1:X4}H";
                                        dumpCount += 2;
                                        break;
                                    case 4:
                                        specification = "LD (FF00H+C),A";
                                        ioRegister = IoRegister.Register;
                                        break;
                                    case 5:
                                        specification = "LD (%2$04XH),A";
                                        dumpCount += 2;
                                        break;
                                    case 6:
                                        specification = "LD A,(FF00H+C)";
                                        ioRegister = IoRegister.Register;
                                        break;
                                    case 7:
                                        specification = "LD A,(%2$04XH)";
                                        dumpCount += 2;
                                        break;
                                }

                                break;
                            case 3: // Assorted operations
                                switch (y)
                                {
                                    case 0: // JP nn
                                        specification = "JP {1:X4}H";
                                        dumpCount += 2;
                                        break;
                                    case 1: // CB prefix
                                        this.prefixCB = true;
                                        output += this.Disassemble(cpu, ++pc);
                                        break;
                                    case 6: // DI
                                        specification = "DI";
                                        break;
                                    case 7: // EI
                                        specification = "EI";
                                        break;
                                }

                                break;
                            case 4: // Conditional call: CALL cc[y], nn
                                specification = $"CALL {CC(y)}" + ",{1:X4}H";
                                dumpCount += 2;
                                break;
                            case 5: // PUSH & various ops
                                switch (q)
                                {
                                    case 0: // PUSH rp2[p]
                                        specification = $"PUSH {RP2(p)}";
                                        break;
                                    case 1:
                                        switch (p)
                                        {
                                            case 0: // CALL nn
                                                specification = "CALL {1:X4}H";
                                                dumpCount += 2;
                                                break;
                                        }

                                        break;
                                }

                                break;
                            case 6: // Operate on accumulator and immediate operand: alu[y] n
                                specification = ALU(y) + " A,{0:X2}H";
                                dumpCount++;
                                break;
                            case 7: // Restart: RST y * 8
                                specification = $"RST {y * 8:X2}";
                                break;
                        }

                        break;
                }

                return output;
            }
        }
    }
}
