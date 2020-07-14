// <copyright file="Disassembler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Text;

    public class Disassembler
    {
        private readonly Bus bus;
        private readonly M6502 processor;
        private readonly Symbols symbols;
        private ushort address;

        public Disassembler(Bus bus, M6502 processor, Symbols symbols)
        {
            this.bus = bus;
            this.processor = processor;
            this.symbols = symbols;
        }

        public static string DumpFlags(byte value)
        {
            var returned = new StringBuilder();
            returned.Append((value & (byte)StatusBits.NF) != 0 ? "N" : "-");
            returned.Append((value & (byte)StatusBits.VF) != 0 ? "O" : "-");
            returned.Append((value & (byte)StatusBits.RF) != 0 ? "R" : "-");
            returned.Append((value & (byte)StatusBits.BF) != 0 ? "B" : "-");
            returned.Append((value & (byte)StatusBits.DF) != 0 ? "D" : "-");
            returned.Append((value & (byte)StatusBits.IF) != 0 ? "I" : "-");
            returned.Append((value & (byte)StatusBits.ZF) != 0 ? "Z" : "-");
            returned.Append((value & (byte)StatusBits.CF) != 0 ? "C" : "-");
            return returned.ToString();
        }

        public static string DumpByteValue(byte value) => value.ToString("X2");

        public static string DumpWordValue(ushort value) => value.ToString("X4");

        public string Disassemble(ushort current)
        {
            this.address = current;

            var output = new StringBuilder();

            var cell = this.bus.Peek(current);

            output.Append(DumpByteValue(cell));
            output.Append(" ");

            var next = this.bus.Peek((ushort)(current + 1));
            var relative = (ushort)(this.processor.PC.Word + 2 + (sbyte)next);

            var aaa = (cell & 0b11100000) >> 5;
            var bbb = (cell & 0b00011100) >> 2;
            var cc = cell & 0b00000011;

            switch (cc)
            {
                case 0b00:
                    switch (aaa)
                    {
                        case 0b000:
                            switch (bbb)
                            {
                                case 0b000: // BRK
                                    output.Append(Disassemble_Implied("BRK"));
                                    break;
                                case 0b001: // DOP/NOP (0x04)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b010: // PHP
                                    output.Append(Disassemble_Implied("PHP"));
                                    break;
                                case 0b011: // TOP/NOP (0b00001100, 0x0c)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b100: // BPL
                                    output.Append(this.Disassemble_Relative("BPL", relative));
                                    break;
                                case 0b101: // DOP/NOP (0x14)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // CLC
                                    output.Append(Disassemble_Implied("CLC"));
                                    break;
                                case 0b111: // TOP/NOP (0b00011100, 0x1c)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default:
                                    throw new System.InvalidOperationException("Illegal instruction");
                            }

                            break;
                        case 0b001:
                            switch (bbb)
                            {
                                case 0b000: // JSR
                                    output.Append(this.Disassemble_Absolute("JSR"));
                                    break;
                                case 0b010: // PLP
                                    output.Append(Disassemble_Implied("PLP"));
                                    break;
                                case 0b100: // BMI
                                    output.Append(this.Disassemble_Relative("BMI", relative));
                                    break;
                                case 0b101: // DOP/NOP (0x34)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // SEC
                                    output.Append(Disassemble_Implied("SEC"));
                                    break;
                                case 0b111: // TOP/NOP (0b00111100, 0x3c)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default: // BIT
                                    output.Append(this.Disassemble_AM_00(bbb, "BIT"));
                                    break;
                            }

                            break;
                        case 0b010:
                            switch (bbb)
                            {
                                case 0b000: // RTI
                                    output.Append(Disassemble_Implied("RTI"));
                                    break;
                                case 0b001: // DOP/NOP (0x44)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b010: // PHA
                                    output.Append(Disassemble_Implied("PHA"));
                                    break;
                                case 0b011: // JMP
                                    output.Append(this.Disassemble_Absolute("JMP"));
                                    break;
                                case 0b100: // BVC
                                    output.Append(this.Disassemble_Relative("BVC", relative));
                                    break;
                                case 0b101: // DOP/NOP (0x54)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // CLI
                                    output.Append(Disassemble_Implied("CLI"));
                                    break;
                                case 0b111: // TOP/NOP (0b01011100, 0x5c)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default:
                                    throw new System.InvalidOperationException("Illegal addressing mode");
                            }

                            break;
                        case 0b011:
                            switch (bbb)
                            {
                                case 0b000: // RTS
                                    output.Append(Disassemble_Implied("RTS"));
                                    break;
                                case 0b001: // DOP/NOP (0x64)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b010: // PLA
                                    output.Append(Disassemble_Implied("PLA"));
                                    break;
                                case 0b011: // JMP (abs)
                                    output.Append(this.Disassemble_Indirect("JMP"));
                                    break;
                                case 0b100: // BVS
                                    output.Append(this.Disassemble_Relative("BVS", relative));
                                    break;
                                case 0b101: // DOP/NOP (0x74)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // SEI
                                    output.Append(Disassemble_Implied("SEI"));
                                    break;
                                case 0b111: // TOP/NOP (0b01111100, 0x7c)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default:
                                    throw new System.InvalidOperationException("Illegal addressing mode");
                            }

                            break;
                        case 0b100:
                            switch (bbb)
                            {
                                case 0b000: // DOP/NOP (0x80)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b010: // DEY
                                    output.Append(Disassemble_Implied("DEY"));
                                    break;
                                case 0b100: // BCC
                                    output.Append(this.Disassemble_Relative("BCC", relative));
                                    break;
                                case 0b110: // TYA
                                    output.Append(Disassemble_Implied("TYA"));
                                    break;
                                default: // STY
                                    output.Append(this.Disassemble_AM_00(bbb, "STY"));
                                    break;
                            }

                            break;
                        case 0b101:
                            switch (bbb)
                            {
                                case 0b010: // TAY
                                    output.Append(Disassemble_Implied("TAY"));
                                    break;
                                case 0b100: // BCS
                                    output.Append(this.Disassemble_Relative("BCS", relative));
                                    break;
                                case 0b110: // CLV
                                    output.Append(Disassemble_Implied("CLV"));
                                    break;
                                default: // LDY
                                    output.Append(this.Disassemble_AM_00(bbb, "LDY"));
                                    break;
                            }

                            break;
                        case 0b110:
                            switch (bbb)
                            {
                                case 0b010: // INY
                                    output.Append(Disassemble_Implied("INY"));
                                    break;
                                case 0b100: // BNE
                                    output.Append(this.Disassemble_Relative("BNE", relative));
                                    break;
                                case 0b101: // DOP/NOP (0xd4)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // CLD
                                    output.Append(Disassemble_Implied("CLD"));
                                    break;
                                case 0b111: // TOP/NOP (0b11011100, 0xdc)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default: // CPY
                                    output.Append(this.Disassemble_AM_00(bbb, "CPY"));
                                    break;
                            }

                            break;
                        case 0b111:
                            switch (bbb)
                            {
                                case 0b010: // INX
                                    output.Append(Disassemble_Implied("INX"));
                                    break;
                                case 0b100: // BEQ
                                    output.Append(this.Disassemble_Relative("BEQ", relative));
                                    break;
                                case 0b101: // DOP/NOP (0xf4)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                case 0b110: // SED
                                    output.Append(Disassemble_Implied("SED"));
                                    break;
                                case 0b111: // TOP/NOP (0b11111100, 0xfc)
                                    output.Append(this.Disassemble_AM_00(bbb, "*NOP"));
                                    break;
                                default: // CPX
                                    output.Append(this.Disassemble_AM_00(bbb, "CPX"));
                                    break;
                            }

                            break;
                    }

                    break;
                case 0b01:
                    switch (aaa)
                    {
                        case 0b000: // ORA
                            output.Append(this.Disassemble_AM_01(bbb, "ORA"));
                            break;
                        case 0b001: // AND
                            output.Append(this.Disassemble_AM_01(bbb, "AND"));
                            break;
                        case 0b010: // EOR
                            output.Append(this.Disassemble_AM_01(bbb, "EOR"));
                            break;
                        case 0b011: // ADC
                            output.Append(this.Disassemble_AM_01(bbb, "ADC"));
                            break;
                        case 0b100: // STA
                            output.Append(this.Disassemble_AM_01(bbb, "STA"));
                            break;
                        case 0b101: // LDA
                            output.Append(this.Disassemble_AM_01(bbb, "LDA"));
                            break;
                        case 0b110: // CMP
                            output.Append(this.Disassemble_AM_01(bbb, "CMP"));
                            break;
                        case 0b111: // SBC
                            output.Append(this.Disassemble_AM_01(bbb, "SBC"));
                            break;
                        default:
                            throw new System.InvalidOperationException("Illegal addressing mode");
                    }

                    break;
                case 0b10:
                    switch (aaa)
                    {
                        case 0b000: // ASL
                            switch (bbb)
                            {
                                case 0b110: // 0x1a
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_10(bbb, "ASL"));
                                    break;
                            }

                            break;
                        case 0b001: // ROL
                            switch (bbb)
                            {
                                case 0b110: // 0x3a
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_10(bbb, "ROL"));
                                    break;
                            }

                            break;
                        case 0b010: // LSR
                            switch (bbb)
                            {
                                case 0b110: // 0x5a
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_10(bbb, "LSR"));
                                    break;
                            }

                            break;
                        case 0b011: // ROR
                            switch (bbb)
                            {
                                case 0b110: // 0x7a
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_10(bbb, "ROR"));
                                    break;
                            }

                            break;
                        case 0b100:
                            switch (bbb)
                            {
                                case 0b010: // TXA
                                    output.Append(Disassemble_Implied("TXA"));
                                    break;
                                case 0b110: // TXS
                                    output.Append(Disassemble_Implied("TXS"));
                                    break;
                                default: // STX
                                    output.Append(this.Disassemble_AM_10_x(bbb, "STX"));
                                    break;
                            }

                            break;
                        case 0b101:
                            switch (bbb)
                            {
                                case 0b010: // TAX
                                    output.Append(Disassemble_Implied("TAX"));
                                    break;
                                case 0b110: // TSX
                                    output.Append(Disassemble_Implied("TSX"));
                                    break;
                                default: // LDX
                                    output.Append(this.Disassemble_AM_10_x(bbb, "LDX"));
                                    break;
                            }

                            break;
                        case 0b110:
                            switch (bbb)
                            {
                                case 0b010: // DEX
                                    output.Append(Disassemble_Implied("DEX"));
                                    break;
                                case 0b110: // 0xda
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default: // DEC
                                    output.Append(this.Disassemble_AM_10(bbb, "DEC"));
                                    break;
                            }

                            break;
                        case 0b111:
                            switch (bbb)
                            {
                                case 0b010: // NOP
                                    output.Append(Disassemble_Implied("NOP"));
                                    break;
                                case 0b110: // 0xfa
                                    output.Append(Disassemble_Implied("*NOP"));
                                    break;
                                default: // INC
                                    output.Append(this.Disassemble_AM_10(bbb, "INC"));
                                    break;
                            }

                            break;
                        default:
                            throw new System.InvalidOperationException("Illegal instruction");
                    }

                    break;
                case 0b11:
                    switch (aaa)
                    {
                        case 0b000:
                            switch (bbb)
                            {
                                case 0b010:
                                    output.Append(this.Disassemble_Immediate("*AAC"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_01(bbb, "*SLO"));
                                    break;
                            }

                            break;
                        case 0b001:

                            switch (bbb)
                            {
                                case 0b010:
                                    output.Append(this.Disassemble_Immediate("*AAC"));
                                    break;
                                default:
                                    output.Append(this.Disassemble_AM_01(bbb, "*RLA"));
                                    break;
                            }

                            break;
                        case 0b010:
                            output.Append(this.Disassemble_AM_01(bbb, "*SRE"));
                            break;
                        case 0b011:
                            output.Append(this.Disassemble_AM_01(bbb, "*RRA"));
                            break;
                        case 0b100:
                            output.Append(this.Disassemble_AM_11(bbb, "*SAX"));
                            break;
                        case 0b101:
                            output.Append(this.Disassemble_AM_11(bbb, "*LAX"));
                            break;
                        case 0b110:
                            output.Append(this.Disassemble_AM_11_x(bbb, "*DCP"));
                            break;
                        case 0b111:
                            switch (bbb)
                            {
                                case 0b000: // *ISB
                                case 0b001:
                                case 0b011:
                                case 0b100:
                                case 0b101:
                                case 0b110:
                                case 0b111:
                                    output.Append(this.Disassemble_AM_01(bbb, "*ISB"));
                                    break;
                                case 0b010:
                                    output.Append(this.Disassemble_AM_11(bbb, "*SBC"));
                                    break;
                                default:
                                    throw new System.InvalidOperationException("Impossible addressing mode");
                            }

                            break;
                        default:
                            throw new System.InvalidOperationException("Illegal instruction group");
                    }

                    break;
                default:
                    throw new System.InvalidOperationException("Impossible instruction");
            }

            return output.ToString();
        }

        private string ConvertAddress(ushort absolute) => this.symbols.Labels.TryGetValue(absolute, out var label) ? label : "$" + DumpWordValue(absolute);

        private string ConvertAddress(byte absolute) => this.symbols.Labels.TryGetValue(absolute, out var label) ? label : "$" + DumpByteValue(absolute);

        private string ConvertConstant(ushort constant) => this.symbols.Constants.TryGetValue(constant, out var label) ? label : this.Dump_DByte(constant);

        private string ConvertConstant(byte constant) => this.symbols.Constants.TryGetValue(constant, out var label) ? label : DumpByteValue(constant);

        private byte GetByte(ushort absolute) => this.bus.Peek(absolute);

        private ushort GetWord(ushort absolute) => this.processor.PeekWord(absolute).Word;

        private string Dump_Byte(ushort absolute) => Disassembler.DumpByteValue(this.GetByte(absolute));

        private string Dump_DByte(ushort absolute) => this.Dump_Byte(absolute) + " " + this.Dump_Byte(++absolute);

        private string Dump_Word(ushort absolute) => Disassembler.DumpWordValue(this.GetWord(absolute));

        private static string Disassemble_Implied(string instruction) => "\t" + instruction;

        private string Disassemble_Absolute(string instruction) => this.AM_Absolute_dump() + "\t" + instruction + " " + this.AM_Absolute();

        private string Disassemble_Indirect(string instruction) => this.AM_Absolute_dump() + "\t" + instruction + " (" + this.AM_Absolute() + ")";

        private string Disassemble_Relative(string instruction, ushort absolute) => this.AM_Immediate_dump() + "\t" + instruction + " $" + Disassembler.DumpWordValue(absolute);

        private string Disassemble_Immediate(string instruction) => this.AM_Immediate_dump() + "\t" + instruction + " " + this.AM_Immediate();

        private string Disassemble_AM_00(int bbb, string instruction) => this.AM_00_dump(bbb) + "\t" + instruction + " " + this.AM_00(bbb);

        private string Disassemble_AM_01(int bbb, string instruction) => this.AM_01_dump(bbb) + "\t" + instruction + " " + this.AM_01(bbb);

        private string Disassemble_AM_10(int bbb, string instruction) => this.AM_10_dump(bbb) + "\t" + instruction + " " + this.AM_10(bbb);

        private string Disassemble_AM_10_x(int bbb, string instruction) => this.AM_10_x_dump(bbb) + "\t" + instruction + " " + this.AM_10_x(bbb);

        private string Disassemble_AM_11(int bbb, string instruction) => this.AM_11_dump(bbb) + "\t" + instruction + " " + this.AM_11(bbb);

        private string Disassemble_AM_11_x(int bbb, string instruction) => this.AM_11_x_dump(bbb) + "\t" + instruction + " " + this.AM_11_x(bbb);

        private string AM_Immediate_dump() => this.Dump_Byte((ushort)(this.address + 1));

        private string AM_Immediate() => "#$" + this.AM_Immediate_dump();

        private string AM_Absolute_dump() => this.Dump_DByte((ushort)(this.address + 1));

        private string AM_Absolute() => "$" + this.Dump_Word((ushort)(this.address + 1));

        private string AM_ZeroPage_dump() => this.Dump_Byte((ushort)(this.address + 1));

        private string AM_ZeroPage() => "$" + this.Dump_Byte((ushort)(this.address + 1));

        private string AM_ZeroPageX_dump() => this.AM_ZeroPage_dump();

        private string AM_ZeroPageX() => this.AM_ZeroPage() + ",X";

        private string AM_ZeroPageY_dump() => this.AM_ZeroPage_dump();

        private string AM_ZeroPageY() => this.AM_ZeroPage() + ",Y";

        private string AM_AbsoluteX_dump() => this.AM_Absolute_dump();

        private string AM_AbsoluteX() => this.AM_Absolute() + ",X";

        private string AM_AbsoluteY_dump() => this.AM_Absolute_dump();

        private string AM_AbsoluteY() => this.AM_Absolute() + ",Y";

        private string AM_IndexedIndirectX_dump() => this.AM_ZeroPage_dump();

        private string AM_IndexedIndirectX() => "($" + this.Dump_Byte((ushort)(this.address + 1)) + ",X)";

        private string AM_IndirectIndexedY_dump() => this.AM_ZeroPage_dump();

        private string AM_IndirectIndexedY() => "($" + this.Dump_Byte((ushort)(this.address + 1)) + "),Y";

        private string AM_00_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b011 => this.AM_Absolute_dump(),
            0b101 => this.AM_ZeroPageX_dump(),
            0b111 => this.AM_AbsoluteX_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_00(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate(),
            0b001 => this.AM_ZeroPage(),
            0b011 => this.AM_Absolute(),
            0b101 => this.AM_ZeroPageX(),
            0b111 => this.AM_AbsoluteX(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_01_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b010 => this.AM_Immediate_dump(),
            0b011 => this.AM_Absolute_dump(),
            0b100 => this.AM_IndirectIndexedY_dump(),
            0b101 => this.AM_ZeroPageX_dump(),
            0b110 => this.AM_AbsoluteY_dump(),
            0b111 => this.AM_AbsoluteX_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_01(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX(),
            0b001 => this.AM_ZeroPage(),
            0b010 => this.AM_Immediate(),
            0b011 => this.AM_Absolute(),
            0b100 => this.AM_IndirectIndexedY(),
            0b101 => this.AM_ZeroPageX(),
            0b110 => this.AM_AbsoluteY(),
            0b111 => this.AM_AbsoluteX(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_10_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b010 => string.Empty,
            0b011 => this.AM_Absolute_dump(),
            0b101 => this.AM_ZeroPageX_dump(),
            0b111 => this.AM_AbsoluteX_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_10(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate(),
            0b001 => this.AM_ZeroPage(),
            0b010 => "A",
            0b011 => this.AM_Absolute(),
            0b101 => this.AM_ZeroPageX(),
            0b111 => this.AM_AbsoluteX(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_10_x_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b010 => string.Empty,
            0b011 => this.AM_Absolute_dump(),
            0b101 => this.AM_ZeroPageY_dump(),
            0b111 => this.AM_AbsoluteY_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_10_x(int bbb) => bbb switch
        {
            0b000 => this.AM_Immediate(),
            0b001 => this.AM_ZeroPage(),
            0b010 => "A",
            0b011 => this.AM_Absolute(),
            0b101 => this.AM_ZeroPageY(),
            0b111 => this.AM_AbsoluteY(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_11_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b010 => this.AM_Immediate_dump(),
            0b011 => this.AM_Absolute_dump(),
            0b100 => this.AM_IndirectIndexedY_dump(),
            0b101 => this.AM_ZeroPageY_dump(),
            0b111 => this.AM_AbsoluteY_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_11_x_dump(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX_dump(),
            0b001 => this.AM_ZeroPage_dump(),
            0b010 => this.AM_Immediate_dump(),
            0b011 => this.AM_Absolute_dump(),
            0b100 => this.AM_IndirectIndexedY_dump(),
            0b101 => this.AM_ZeroPageX_dump(),
            0b110 => this.AM_AbsoluteY_dump(),
            0b111 => this.AM_AbsoluteX_dump(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_11(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX(),
            0b001 => this.AM_ZeroPage(),
            0b010 => this.AM_Immediate(),
            0b011 => this.AM_Absolute(),
            0b100 => this.AM_IndirectIndexedY(),
            0b101 => this.AM_ZeroPageY(),
            0b111 => this.AM_AbsoluteY(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };

        private string AM_11_x(int bbb) => bbb switch
        {
            0b000 => this.AM_IndexedIndirectX(),
            0b001 => this.AM_ZeroPage(),
            0b010 => this.AM_Immediate(),
            0b011 => this.AM_Absolute(),
            0b100 => this.AM_IndirectIndexedY(),
            0b101 => this.AM_ZeroPageX(),
            0b110 => this.AM_AbsoluteY(),
            0b111 => this.AM_AbsoluteX(),
            _ => throw new System.InvalidOperationException("Illegal addressing mode"),
        };
    }
}
