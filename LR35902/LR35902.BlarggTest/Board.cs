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
            this.WrittenByte += this.Board_WrittenByte;
            if (this.configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_Debug;
            }

            this.LoadBootRom(this.configuration.RomDirectory + "/DMG_ROM.bin");
        }

        public void Plug(string path) => this.LoadGameRom(this.configuration.RomDirectory + "/" + path);

        private void Board_WrittenByte(object? sender, System.EventArgs e)
        {
            EightBit.Register16 serial = new(IoRegisters.SB, IoRegisters.BasePage);
            if (this.Address == serial)
            {
                System.Console.Out.Write(Convert.ToChar(this.Data));
            }
        }

        private void CPU_ExecutingInstruction_Debug(object? sender, System.EventArgs e)
        {
            if (this.IO.BootRomDisabled)
            {
                System.Console.Error.WriteLine($"{Disassembler.State(this.CPU)} {this.disassembler.Disassemble(this.CPU)}");
            }
        }
    }
}
