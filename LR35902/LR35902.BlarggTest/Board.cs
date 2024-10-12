// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.BlarggTest
{
    using System;

    internal class Board : EightBit.GameBoy.Bus
    {
        private readonly Configuration configuration;
        private readonly EightBit.GameBoy.Disassembler disassembler;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.disassembler = new EightBit.GameBoy.Disassembler(this);
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
            switch (this.Address.Word)
            {
                case EightBit.GameBoy.IoRegisters.BASE + EightBit.GameBoy.IoRegisters.SB:
                    System.Console.Out.Write(Convert.ToChar(this.Data));
                    break;
                default:
                    break;
            }
        }

        private void CPU_ExecutingInstruction_Debug(object? sender, System.EventArgs e)
        {
            if (this.IO.BootRomDisabled)
            {
                System.Console.Error.WriteLine($"{EightBit.GameBoy.Disassembler.State(this.CPU)} {this.disassembler.Disassemble(this.CPU)}");
            }
        }
    }
}
