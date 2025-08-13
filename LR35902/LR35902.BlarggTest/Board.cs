// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.BlarggTest
{
    using System;

    internal class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Disassembler disassembler;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.disassembler = new Disassembler(this);
        }

        public override void Initialize()
        {
#if GAMEBOY_DOCTOR
            this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_Debug;
#else
            this.WrittenByte += this.Board_WrittenByte;
            if (this.configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_Debug;
            }

            this.LoadBootRom(this.configuration.RomDirectory + "/DMG_ROM.bin");
#endif
        }

#if GAMEBOY_DOCTOR
        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.A = 0x01;
            this.CPU.F = 0xB0;
            this.CPU.B = 0x00;
            this.CPU.C = 0x13;
            this.CPU.D = 0x00;
            this.CPU.E = 0xD8;
            this.CPU.H = 0x01;
            this.CPU.L = 0x4D;
            this.CPU.SP.Assign(0xfe, 0xff);
            this.CPU.PC.Assign(0x00, 0x01);
            this.CPU.RaiseRESET();
        }
#endif

        public void Plug(string path) => this.LoadGameRom(this.configuration.RomDirectory + "/" + path);

        private void Board_WrittenByte(object? sender, System.EventArgs e)
        {
            EightBit.Register16 serial = new(IoRegisters.SB, IoRegisters.BasePage);
            if (this.Address == serial)
            {
                System.Console.Out.Write(Convert.ToChar(this.Data));
            }
        }

        private void CPU_ExecutingInstruction_Debug(object? sender, System.EventArgs _)
        {
#if GAMEBOY_DOCTOR
            var a = this.CPU.A;
            var f = this.CPU.F;
            var b = this.CPU.B;
            var c = this.CPU.C;
            var d = this.CPU.D;
            var e = this.CPU.E;
            var h = this.CPU.H;
            var l = this.CPU.L;
            var sp = this.CPU.SP.Word;
            var pc = this.CPU.PC.Word;
            var aa = this.Peek(pc);
            var bb = this.Peek((ushort)(pc + 1));
            var cc = this.Peek((ushort)(pc + 2));
            var dd = this.Peek((ushort)(pc + 3));
            System.Console.WriteLine($"A:{a:X2} F:{f:X2} B:{b:X2} C:{c:X2} D:{d:X2} E:{e:X2} H:{h:X2} L:{l:X2} SP:{sp:X4} PC:{pc:X4} PCMEM:{aa:X2},{bb:X2},{cc:X2},{dd:X2}");
#else
            if (this.IO.BootRomDisabled)
            {
                System.Console.Error.WriteLine($"{Disassembler.State(this.CPU)} {this.disassembler.Disassemble(this.CPU)}");
            }
#endif
        }
    }
}
