﻿// <copyright file="Disassembler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80
{
    using EightBit;
    using System.Globalization;

    public class Disassembler(Bus bus)
    {
        private bool _prefixCB;
        private bool _prefixDD;
        private bool _prefixED;
        private bool _prefixFD;

        public Bus Bus { get; } = bus;

        public static string AsFlag(byte value, StatusBits flag, string represents) => (value & (byte)flag) != 0 ? represents : "-";

        public static string AsFlags(byte value) =>
                   $"{AsFlag(value, StatusBits.SF, "S")}"
                 + $"{AsFlag(value, StatusBits.ZF, "Z")}"
                 + $"{AsFlag(value, StatusBits.YF, "Y")}"
                 + $"{AsFlag(value, StatusBits.HC, "H")}"
                 + $"{AsFlag(value, StatusBits.XF, "X")}"
                 + $"{AsFlag(value, StatusBits.PF, "P")}"
                 + $"{AsFlag(value, StatusBits.NF, "N")}"
                 + $"{AsFlag(value, StatusBits.CF, "C")}";

        public static string State(Z80 cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu);

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

            var i = cpu.IV;
            var r = cpu.REFRESH;

            var im = cpu.IM;

            return
                  $"PC={pc.Word:x4} SP={sp.Word:x4} "
                + $"A={a:x2} F={AsFlags(f)} "
                + $"B={b:x2} C={c:x2} "
                + $"D={d:x2} E={e:x2} "
                + $"H={h:x2} L={l:x2} "
                + $"IX={cpu.IX.Word:x4} "
                + $"IY={cpu.IY.Word:x4} "
                + $"I={i:x2} R={(byte)r:x2} "
                + $"IM={im} "
                + $"IFF1={(cpu.IFF1 ? 1 : 0)} "
                + $"{(cpu.RESET.Lowered() ? "R" : "-")}"
                + $"{(cpu.INT.Lowered() ? "I" : "-")}"
                + $"{(cpu.HALT.Lowered() ? "H" : "-")}"
                + $"{(cpu.NMI.Lowered() ? "N" : "-")}";
        }

        public string Disassemble(Z80 cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            this._prefixCB = this._prefixDD = this._prefixED = this._prefixFD = false;
            return this.Disassemble(cpu, cpu.PC.Word);
        }

        private static string CC(int flag) => flag switch
        {
            0 => "NZ",
            1 => "Z",
            2 => "NC",
            3 => "C",
            4 => "PO",
            5 => "PE",
            6 => "P",
            7 => "M",
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        private static string ALU(int which) => which switch
        {
            0 => "ADD",
            1 => "ADC",
            2 => "SUB",
            3 => "SBC",
            4 => "AND",
            5 => "XOR",
            6 => "OR",
            7 => "CP",
            _ => throw new ArgumentOutOfRangeException(nameof(which)),
        };

        public string Disassemble(Z80 cpu, ushort pc)
        {
            ArgumentNullException.ThrowIfNull(cpu);

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

            var output = $"{opCode:x2}";

            var specification = string.Empty;
            if (this._prefixCB)
            {
                output += this.DisassembleCB(ref specification, x, y, z);
            }
            else if (this._prefixED)
            {
                output += this.DisassembleED(ref specification, ref dumpCount, x, y, z, p, q);
            }
            else
            {
                output += this.DisassembleOther(cpu, pc, ref specification, ref dumpCount, x, y, z, p, q);
            }

            for (var i = 0; i < dumpCount; ++i)
            {
                output += $"{this.Bus.Peek((ushort)(pc + i + 1)):x2}";
            }

            var outputFormatSpecification = !this._prefixDD;
            if (this._prefixDD)
            {
                if (opCode != 0xdd)
                {
                    outputFormatSpecification = true;
                }
            }

            if (outputFormatSpecification)
            {
                output += '\t';
                output += string.Format(CultureInfo.InvariantCulture, specification, (int)immediate, (int)absolute, relative, (int)displacement, indexedImmediate);
            }

            return output;
        }

        private string DisassembleCB(ref string specification, int x, int y, int z)
        {
            var output = string.Empty;
            switch (x)
            {
                case 0: // rot[y] r[z]
                    switch (y)
                    {
                        case 0:
                            specification = $"RLC {this.R(z)}";
                            break;
                        case 1:
                            specification = $"RRC {this.R(z)}";
                            break;
                        case 2:
                            specification = $"RL {this.R(z)}";
                            break;
                        case 3:
                            specification = $"RR {this.R(z)}";
                            break;
                        case 4:
                            specification = $"SLA {this.R(z)}";
                            break;
                        case 5:
                            specification = $"SRA {this.R(z)}";
                            break;
                        case 6:
                            specification = $"SWAP {this.R(z)}";
                            break;
                        case 7:
                            specification = $"SRL {this.R(z)}";
                            break;
                        default:
                            break;
                    }

                    break;
                case 1: // BIT y, r[z]
                    specification = $"BIT {y},{this.R(z)}";
                    break;
                case 2: // RES y, r[z]
                    specification = $"RES {y},{this.R(z)}";
                    break;
                case 3: // SET y, r[z]
                    specification = $"SET {y},{this.R(z)}";
                    break;
                default:
                    break;
            }

            return output;
        }

        private string DisassembleED(ref string specification, ref int dumpCount, int x, int y, int z, int p, int q)
        {
            var output = string.Empty;
            switch (x)
            {
                case 0:
                case 3:
                    specification = "NONI NOP";
                    break;
                case 1:
                    switch (z)
                    {
                        case 0:
                            specification = $"IN {this.R(y)}, (C)";
                            break;

                        case 1:
                            specification = $"OUT (C), {this.R(y)}";
                            break;

                        case 2:
                            switch (q)
                            {
                                case 0: // SBC HL,rp
                                    specification = $"SBC HL,{this.RP(p)}";
                                    break;
                                case 1: // ADC HL,rp
                                    specification = $"ADC HL,{this.RP(p)}";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 3:
                            switch (q)
                            {
                                case 0: // LD (nn),rp
                                    specification = "LD ({1:X4}H)," + this.RP(p);
                                    break;
                                case 1: // LD rp,(nn)
                                    specification = "LD " + this.RP(p) + ",(%2$04XH)";
                                    break;
                                default:
                                    break;
                            }

                            dumpCount += 2;
                            break;
                        case 4: // Negate accumulator
                            specification = "NEG";
                            break;
                        case 5: // Return from interrupt
                            specification = y switch
                            {
                                1 => "RETI",
                                _ => "RETN",
                            };
                            break;
                        case 6: // Set interrupt mode
                            switch (y)
                            {
                                case 0:
                                case 1:
                                case 4:
                                case 5:
                                    specification = "IM 0";
                                    break;
                                case 2:
                                case 6:
                                    specification = "IM 1";
                                    break;
                                case 3:
                                case 7:
                                    specification = "IM 2";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 7:
                            switch (y)
                            {
                                case 0:
                                    specification = "LD I,A";
                                    break;
                                case 1:
                                    specification = "LD R,A";
                                    break;
                                case 2:
                                    specification = "LD A,I";
                                    break;
                                case 3:
                                    specification = "LD A,R";
                                    break;
                                case 4:
                                    specification = "RRD";
                                    break;
                                case 5:
                                    specification = "RLD";
                                    break;
                                case 6:
                                case 7:
                                    specification = "NOP";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }

                    break;
                case 2:
                    switch (z)
                    {
                        case 0: // LD
                            switch (y)
                            {
                                case 4: // LDI
                                    specification = "LDI";
                                    break;
                                case 5: // LDD
                                    specification = "LDD";
                                    break;
                                case 6: // LDIR
                                    specification = "LDIR";
                                    break;
                                case 7: // LDDR
                                    specification = "LDDR";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 1: // CP
                            switch (y)
                            {
                                case 4: // CPI
                                    specification = "CPI";
                                    break;
                                case 5: // CPD
                                    specification = "CPD";
                                    break;
                                case 6: // CPIR
                                    specification = "CPIR";
                                    break;
                                case 7: // CPDR
                                    specification = "CPDR";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 2: // IN
                            switch (y)
                            {
                                case 4: // INI
                                    specification = "INI";
                                    break;
                                case 5: // IND
                                    specification = "IND";
                                    break;
                                case 6: // INIR
                                    specification = "INIR";
                                    break;
                                case 7: // INDR
                                    specification = "INDR";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 3: // OUT
                            switch (y)
                            {
                                case 4: // OUTI
                                    specification = "OUTI";
                                    break;
                                case 5: // OUTD
                                    specification = "OUTD";
                                    break;
                                case 6: // OTIR
                                    specification = "OTIR";
                                    break;
                                case 7: // OTDR
                                    specification = "OTDR";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            return output;
        }

        private string DisassembleOther(Z80 cpu, ushort pc, ref string specification, ref int dumpCount, int x, int y, int z, int p, int q)
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
                                case 1: // EX AF AF'
                                    specification = "EX AF AF'";
                                    break;
                                case 2: // DJNZ d
                                    specification = "DJNZ {2:X4}H";
                                    dumpCount += 2;
                                    break;
                                case 3: // JR d
                                    specification = "JR {2:X4}H";
                                    dumpCount++;
                                    break;
                                default: // JR cc,d
                                    specification = "JR " + CC(y - 4) + ",{2:X4}H";
                                    dumpCount++;
                                    break;
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    specification = "LD " + this.RP(p) + ",{1:X4}H";
                                    dumpCount += 2;
                                    break;
                                case 1: // ADD HL,rp
                                    specification = $"ADD HL,{this.RP(p)}";
                                    break;
                                default:
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
                                        case 2: // LD (nn),HL
                                            specification = "LD ({1:X4}H),HL";
                                            dumpCount += 2;
                                            break;
                                        case 3: // LD (nn),A
                                            specification = "LD ({1:X4}H),A";
                                            dumpCount += 2;
                                            break;
                                        default:
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
                                        case 2: // LD HL,(nn)
                                            specification = "LD HL,({1:X4}H)";
                                            dumpCount += 2;
                                            break;
                                        case 3: // LD A,(nn)
                                            specification = "LD A,({1:X4}H)";
                                            dumpCount += 2;
                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 3: // 16-bit INC/DEC
                            switch (q)
                            {
                                case 0: // INC rp
                                    specification = $"INC {this.RP(p)}";
                                    break;
                                case 1: // DEC rp
                                    specification = $"DEC {this.RP(p)}";
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 4: // 8-bit INC
                            specification = $"INC {this.R(y)}";
                            break;
                        case 5: // 8-bit DEC
                            specification = $"DEC {this.R(y)}";
                            break;
                        case 6: // 8-bit load immediate
                            specification = $"LD {this.R(y)}";
                            if (y == 6 && (this._prefixDD || this._prefixFD))
                            {
                                specification += ",{4:X2}H";
                                dumpCount++;
                            }
                            else
                            {
                                specification += ",{0:X2}H";
                            }

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
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }

                    break;
                case 1: // 8-bit loading
                    specification = z == 6 && y == 6 ? "HALT" : $"LD {this.R(y)},{this.R(z)}";
                    break;
                case 2: // Operate on accumulator and register/memory location
                    specification = $"{ALU(y)} A,{this.R(z)}";
                    break;
                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            specification = $"RET {CC(y)}";
                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    specification = $"POP {this.RP2(p)}";
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            specification = "RET";
                                            break;
                                        case 1: // EXX
                                            specification = "EXX";
                                            break;
                                        case 2: // JP (HL)
                                            specification = "JP (HL)";
                                            break;
                                        case 3: // LD SP,HL
                                            specification = "LD SP,HL";
                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 2: // Conditional jump
                            specification = $"JP {CC(y)}" + ",{1:X4}H";
                            dumpCount += 2;
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    specification = "JP {1:X4}H";
                                    dumpCount += 2;
                                    break;
                                case 1: // CB prefix
                                    this._prefixCB = true;
                                    output += this.Disassemble(cpu, ++pc);
                                    break;
                                case 2: // OUT (n),A
                                    specification = "OUT ({0:X2}H),A";
                                    dumpCount++;
                                    break;
                                case 3: // IN A,(n)
                                    specification = "IN A,({0:X2}H)";
                                    dumpCount++;
                                    break;
                                case 4: // EX (SP),HL
                                    specification = "EX (SP),HL";
                                    break;
                                case 5: // EX DE,HL
                                    specification = "EX DE,HL";
                                    break;
                                case 6: // DI
                                    specification = "DI";
                                    break;
                                case 7: // EI
                                    specification = "EI";
                                    break;
                                default:
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
                                    specification = $"PUSH {this.RP2(p)}";
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            specification = "CALL {1:X4}H";
                                            dumpCount += 2;
                                            break;
                                        case 1: // DD prefix
                                            this._prefixDD = true;
                                            output += this.Disassemble(cpu, ++pc);
                                            break;
                                        case 2: // ED prefix
                                            this._prefixED = true;
                                            output += this.Disassemble(cpu, ++pc);
                                            break;
                                        case 3: // FD prefix
                                            this._prefixFD = true;
                                            output += this.Disassemble(cpu, ++pc);
                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                default:
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
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            return output;
        }

        private string RP(int rp)
        {
            switch (rp)
            {
                case 0:
                    return "BC";
                case 1:
                    return "DE";
                case 2:
                    if (this._prefixDD)
                    {
                        return "IX";
                    }

                    if (this._prefixFD)
                    {
                        return "IY";
                    }

                    return "HL";
                case 3:
                    return "SP";
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(rp));
        }

        private string RP2(int rp)
        {
            switch (rp)
            {
                case 0:
                    return "BC";
                case 1:
                    return "DE";
                case 2:
                    if (this._prefixDD)
                    {
                        return "IX";
                    }

                    if (this._prefixFD)
                    {
                        return "IY";
                    }

                    return "HL";
                case 3:
                    return "AF";
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(rp));
        }

        private string R(int r)
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
                    if (this._prefixDD)
                    {
                        return "IXH";
                    }

                    if (this._prefixFD)
                    {
                        return "IYH";
                    }

                    return "H";
                case 5:
                    if (this._prefixDD)
                    {
                        return "IXL";
                    }

                    if (this._prefixFD)
                    {
                        return "IYL";
                    }

                    return "L";
                case 6:
                    if (this._prefixDD || this._prefixFD)
                    {
                        if (this._prefixDD)
                        {
                            return "IX+{4}";
                        }

                        if (this._prefixFD)
                        {
                            return "IY+{4}";
                        }
                    }
                    else
                    {
                        return "(HL)";
                    }

                    break;
                case 7:
                    return "A";
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(r));
        }
    }
}
