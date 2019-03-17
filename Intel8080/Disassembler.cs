// <copyright file="Disassembler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Disassembler
    {
        public Disassembler(Bus bus) => this.Bus = bus;

        public Bus Bus { get; }

        public static string AsFlag(byte value, StatusBits flag, string represents, string off = "-") => (value & (byte)flag) != 0 ? represents : off;

        public static string AsFlags(byte value) =>
                   AsFlag(value, StatusBits.SF, "S")
                 + AsFlag(value, StatusBits.ZF, "Z")
                 + AsFlag(value, (StatusBits)Bits.Bit5, "1", "0")
                 + AsFlag(value, StatusBits.AC, "A")
                 + AsFlag(value, (StatusBits)Bits.Bit3, "1", "0")
                 + AsFlag(value, StatusBits.PF, "P")
                 + AsFlag(value, (StatusBits)Bits.Bit1, "1", "0")
                 + AsFlag(value, StatusBits.CF, "C");

        public static string State(Intel8080 cpu)
        {
            var pc = cpu.PC.Word;
            var sp = cpu.SP.Word;

            var a = cpu.A;
            var f = cpu.F;

            var b = cpu.B;
            var c = cpu.C;

            var d = cpu.D;
            var e = cpu.E;

            var h = cpu.H;
            var l = cpu.L;

            return
                  $"PC={pc:x4} SP={sp:x4} "
                + $"A={a:x2} F={AsFlags(f)} "
                + $"B={b:x2} C={c:x2} "
                + $"D={d:x2} E={e:x2} "
                + $"H={h:x2} L={l:x2}";
        }

        public string Disassemble(Intel8080 cpu) => this.Disassemble(cpu, cpu.PC.Word);

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
                    return "SBB";
                case 4: // AND n
                    return "ANA";
                case 5: // XOR n
                    return "XRA";
                case 6: // OR n
                    return "ORA";
                case 7: // CP n
                    return "CMP";
            }

            throw new System.ArgumentOutOfRangeException(nameof(which));
        }

        private static string ALU2(int which)
        {
            switch (which)
            {
                case 0: // ADD A,n
                    return "ADI";
                case 1: // ADC
                    return "ACI";
                case 2: // SUB n
                    return "SUI";
                case 3: // SBC A,n
                    return "SBI";
                case 4: // AND n
                    return "ANI";
                case 5: // XOR n
                    return "XRI";
                case 6: // OR n
                    return "ORI";
                case 7: // CP n
                    return "CPI";
            }

            throw new System.ArgumentOutOfRangeException(nameof(which));
        }

        private static Tuple<string, int> Disassemble(int x, int y, int z, int p, int q)
        {
            var dumpCount = 0;
            var specification = string.Empty;
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
                                    break;
                                case 2: // DJNZ d
                                    break;
                                case 3: // JR d
                                    break;
                                default: // JR cc,d
                                    break;
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    specification = $"LXI {RP(p)}" + ",{1:X4}H";
                                    dumpCount += 2;
                                    break;
                                case 1: // ADD HL,rp
                                    specification = $"DAD {RP(p)}";
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
                                            specification = "STAX B";
                                            break;
                                        case 1: // LD (DE),A
                                            specification = "STAX D";
                                            break;
                                        case 2: // LD (nn),HL
                                            specification = "SHLD {1:X4}H";
                                            dumpCount += 2;
                                            break;
                                        case 3: // LD (nn),A
                                            specification = "STA {1:X4}H";
                                            dumpCount += 2;
                                            break;
                                    }

                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            specification = "LDAX B";
                                            break;
                                        case 1: // LD A,(DE)
                                            specification = "LDAX D";
                                            break;
                                        case 2: // LD HL,(nn)
                                            specification = "LHLD {1:X4}H";
                                            dumpCount += 2;
                                            break;
                                        case 3: // LD A,(nn)
                                            specification = "LDA {1:X4}H";
                                            dumpCount += 2;
                                            break;
                                    }

                                    break;
                            }

                            break;
                        case 3: // 16-bit INC/DEC
                            switch (q)
                            {
                                case 0: // INC rp
                                    specification = $"INX {RP(p)}";
                                    break;
                                case 1: // DEC rp
                                    specification = $"DCX {RP(p)}";
                                    break;
                            }

                            break;
                        case 4: // 8-bit INC
                            specification = $"INR {R(y)}";
                            break;
                        case 5: // 8-bit DEC
                            specification = $"DCR {R(y)}";
                            break;
                        case 6: // 8-bit load immediate
                            specification = $"MVI {R(y)}" + ",{0:X2}H";
                            dumpCount++;
                            break;
                        case 7: // Assorted operations on accumulator/flags
                            switch (y)
                            {
                                case 0:
                                    specification = "RLC";
                                    break;
                                case 1:
                                    specification = "RRC";
                                    break;
                                case 2:
                                    specification = "RAL";
                                    break;
                                case 3:
                                    specification = "RAR";
                                    break;
                                case 4:
                                    specification = "DAA";
                                    break;
                                case 5:
                                    specification = "CMA";
                                    break;
                                case 6:
                                    specification = "STC";
                                    break;
                                case 7:
                                    specification = "CMC";
                                    break;
                            }

                            break;
                    }

                    break;
                case 1: // 8-bit loading
                    specification = z == 6 && y == 6 ? "HLT" : $"MOV {R(y)},{R(z)}";
                    break;
                case 2: // Operate on accumulator and register/memory location
                    specification = $"{ALU(y)} {R(z)}";
                    break;
                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            specification = $"R{CC(y)}";
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
                                        case 1: // EXX
                                            break;
                                        case 2: // JP (HL)
                                            specification = "PCHL";
                                            break;
                                        case 3: // LD SP,HL
                                            specification = "SPHL";
                                            break;
                                    }

                                    break;
                            }

                            break;
                        case 2: // Conditional jump
                            specification = $"J{CC(y)}" + " {1:X4}H";
                            dumpCount += 2;
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    specification = "JMP {1:X4}H";
                                    dumpCount += 2;
                                    break;
                                case 1: // CB prefix
                                    break;
                                case 2: // OUT (n),A
                                    specification = "OUT {0:X2}H";
                                    dumpCount++;
                                    break;
                                case 3: // IN A,(n)
                                    specification = "IN {0:X2}H";
                                    dumpCount++;
                                    break;
                                case 4: // EX (SP),HL
                                    specification = "XHTL";
                                    break;
                                case 5: // EX DE,HL
                                    specification = "XCHG";
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
                            specification = $"C{CC(y)}" + " {1:X4}H";
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
                                        case 1: // DD prefix
                                            break;
                                        case 2: // ED prefix
                                            break;
                                        case 3: // FD prefix
                                            break;
                                    }

                                    break;
                            }

                            break;
                        case 6: // Operate on accumulator and immediate operand: alu[y] n
                            specification = ALU2(y) + " {0:X2}H";
                            dumpCount++;
                            break;
                        case 7: // Restart: RST y * 8
                            specification = $"RST {y * 8:X2}";
                            break;
                    }

                    break;
            }

            return new Tuple<string, int>(specification, dumpCount);
        }

        private static string RP(int rp)
        {
            switch (rp)
            {
                case 0:
                    return "B";
                case 1:
                    return "D";
                case 2:
                    return "H";
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
                    return "B";
                case 1:
                    return "D";
                case 2:
                    return "H";
                case 3:
                    return "PSW";
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
                    return "M";
                case 7:
                    return "A";
            }

            throw new System.ArgumentOutOfRangeException(nameof(r));
        }

        private string Disassemble(Intel8080 cpu, ushort pc)
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

            var disassembled = Disassemble(x, y, z, p, q);
            var specification = disassembled.Item1;
            var dumpCount = disassembled.Item2;

            var output = $"{opCode:x2}";
            for (var i = 0; i < dumpCount; ++i)
            {
                output += $"{this.Bus.Peek((ushort)(pc + i + 1)):x2}";
            }

            output += '\t';
            output += string.Format(specification, (int)immediate, (int)absolute, relative, (int)displacement, indexedImmediate);

            return output;
        }
    }
}
