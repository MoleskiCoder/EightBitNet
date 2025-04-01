// <copyright file="Disassembler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;
    using System.Globalization;

    public enum IoRegister
    {
        Abbreviated,    // FF00 + dd
        Absolute,       // FFdd
        Register,       // C
        Unused,         // Unused!
    }

    public sealed class Disassembler(Bus bus)
    {
        private bool prefixCB;

        public Bus Bus { get; } = bus;

        public static string AsFlag(byte value, byte flag, string represents) => (value & flag) != 0 ? represents : "-";

        public static string AsFlag(byte value, StatusBits flag, string represents) => AsFlag(value, (byte)flag, represents);

        public static string AsFlag(byte value, Interrupts flag, string represents) => AsFlag(value, (byte)flag, represents);

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

        public static string AsInterrupt(byte value) =>
                      $"{AsFlag(value, Interrupts.KeypadPressed, "K")}"
                    + $"{AsFlag(value, Interrupts.SerialTransfer, "S")}"
                    + $"{AsFlag(value, Interrupts.TimerOverflow, "T")}"
                    + $"{AsFlag(value, Interrupts.DisplayControlStatus, "D")}"
                    + $"{AsFlag(value, Interrupts.VerticalBlank, "V")}";

        public static string State(LR35902 cpu)
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

            var ime = cpu.IME?1:0;
            var ie = cpu.IE;
            var i_f = cpu.IF;
            var masked = cpu.MaskedInterrupts;

            return
                  $"PC={pc.Word:x4} SP={sp.Word:x4} "
                + $"A={a:x2} F={AsFlags(f)} "
                + $"B={b:x2} C={c:x2} "
                + $"D={d:x2} E={e:x2} "
                + $"H={h:x2} L={l:x2} "
                + $"IME={ime} IE={AsInterrupt(ie)} IF={AsInterrupt(i_f)} Masked={AsInterrupt(masked)}";
        }

        public string Disassemble(LR35902 cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            this.prefixCB = false;
            return this.Disassemble(cpu, cpu.PC.Word);
        }

        private static string RP(int rp) => rp switch
        {
            0 => "BC",
            1 => "DE",
            2 => "HL",
            3 => "SP",
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private static string RP2(int rp) => rp switch
        {
            0 => "BC",
            1 => "DE",
            2 => "HL",
            3 => "AF",
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private static string R(int r) => r switch
        {
            0 => "B",
            1 => "C",
            2 => "D",
            3 => "E",
            4 => "H",
            5 => "L",
            6 => "(HL)",
            7 => "A",
            _ => throw new ArgumentOutOfRangeException(nameof(r)),
        };

        private static string IO(byte value) => value switch
        {
            // Port/Mode Registers
            IoRegisters.P1 => "P1",
            IoRegisters.SB => "SB",
            IoRegisters.SC => "SC",
            IoRegisters.DIV => "DIV",
            IoRegisters.TIMA => "TIMA",
            IoRegisters.TMA => "TMA",
            IoRegisters.TAC => "TAC",
            // Interrupt Flags
            IoRegisters.IF => "IF",
            IoRegisters.IE => "IE",
            // LCD Display Registers
            IoRegisters.LCDC => "LCDC",
            IoRegisters.STAT => "STAT",
            IoRegisters.SCY => "SCY",
            IoRegisters.SCX => "SCX",
            IoRegisters.LY => "LY",
            IoRegisters.LYC => "LYC",
            IoRegisters.DMA => "DMA",
            IoRegisters.BGP => "BGP",
            IoRegisters.OBP0 => "OBP0",
            IoRegisters.OBP1 => "OBP1",
            IoRegisters.WY => "WY",
            IoRegisters.WX => "WX",
            // Sound Registers
            IoRegisters.NR10 => "NR10",
            IoRegisters.NR11 => "NR11",
            IoRegisters.NR12 => "NR12",
            IoRegisters.NR13 => "NR13",
            IoRegisters.NR14 => "NR14",
            IoRegisters.NR21 => "NR21",
            IoRegisters.NR22 => "NR22",
            IoRegisters.NR23 => "NR23",
            IoRegisters.NR24 => "NR24",
            IoRegisters.NR30 => "NR30",
            IoRegisters.NR31 => "NR31",
            IoRegisters.NR32 => "NR32",
            IoRegisters.NR33 => "NR33",
            IoRegisters.NR34 => "NR34",
            IoRegisters.NR41 => "NR41",
            IoRegisters.NR42 => "NR42",
            IoRegisters.NR43 => "NR43",
            IoRegisters.NR44 => "NR44",
            IoRegisters.NR50 => "NR50",
            IoRegisters.NR51 => "NR51",
            IoRegisters.NR52 => "NR52",
            IoRegisters.WAVE_PATTERN_RAM_START => "WAVE_PATTERN_RAM_START",
            IoRegisters.WAVE_PATTERN_RAM_END => "WAVE_PATTERN_RAM_END",
            // Boot rom control
            IoRegisters.BOOT_DISABLE => "BOOT_DISABLE",
            _ => $"{value:x2}",
        };

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
            0 => "ADD", // ADD A,n
            1 => "ADC", // ADC
            2 => "SUB", // SUB n
            3 => "SBC", // SBC A,n
            4 => "AND", // AND n
            5 => "XOR", // XOR n
            6 => "OR",  // OR n
            7 => "CP",  // CP n
            _ => throw new ArgumentOutOfRangeException(nameof(which)),
        };

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
                        default:
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
                default:
                    break;
            }

            return output;
        }

        public string Disassemble(LR35902 cpu, ushort pc)
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
            output += string.Format(CultureInfo.InvariantCulture, specification, (int)immediate, (int)absolute, relative, (int)displacement, indexedImmediate);

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
                default:
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
                                        case 2: // GB: LDI (HL),A
                                            specification = "LDI (HL),A";
                                            break;
                                        case 3: // GB: LDD (HL),A
                                            specification = "LDD (HL),A";
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
                                        case 2: // GB: LDI A,(HL)
                                            specification = "LDI A,(HL)";
                                            break;
                                        case 3: // GB: LDD A,(HL)
                                            specification = "LDD A,(HL)";
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
                                    specification = $"INC {RP(p)}";
                                    break;
                                case 1: // DEC rp
                                    specification = $"DEC {RP(p)}";
                                    break;
                                default:
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
                                default:
                                    break;
                            }

                            break;
                        default:
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
                                    specification = "LD (FF00H+{0:X2}H),A";
                                    ioRegister = IoRegister.Abbreviated;
                                    dumpCount++;
                                    break;
                                case 5:
                                    specification = "ADD SP,{4:X4}H";
                                    dumpCount++;
                                    break;
                                case 6:
                                    specification = "LD A,(FF00H+{0:X2}H)";
                                    ioRegister = IoRegister.Abbreviated;
                                    dumpCount++;
                                    break;
                                case 7:
                                    specification = "LD HL,SP+{4}";
                                    dumpCount++;
                                    break;
                                default:
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
                                        default:
                                            break;
                                    }

                                    break;
                                default:
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
                                    specification = "LD ({1:X4}H),A";
                                    dumpCount += 2;
                                    break;
                                case 6:
                                    specification = "LD A,(FF00H+C)";
                                    ioRegister = IoRegister.Register;
                                    break;
                                case 7:
                                    specification = "LD A,({1:X4}H)";
                                    dumpCount += 2;
                                    break;
                                default:
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
                                    specification = $"PUSH {RP2(p)}";
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            specification = "CALL {1:X4}H";
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
    }
}
