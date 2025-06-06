﻿// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.Test
{
    using EightBit;
    using System.Globalization;

    internal sealed class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram;
        private readonly InputOutput ports;
        private readonly Disassembler disassembler;
        private readonly MemoryMapping mapping;

        private int warmstartCount;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.ram = new(0x10000);
            this.ports = new();
            this.CPU = new(this, this.ports);
            this.disassembler = new(this);
            this.mapping = new(this.ram, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
        }

        public Z80 CPU { get; }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            var programPath = this.configuration.RomDirectory + "/" + this.configuration.Program;
            var loadAddress = this.configuration.LoadAddress;
            _ = this.ram.Load(programPath, loadAddress.Word);

            this.CPU.LoweredHALT += this.CPU_LoweredHALT;
            this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_CPM;

            if (this.configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_Debug;
            }

            this.Poke(0, 0xc3);  // JMP
            this.CPU.PokeWord(1, this.configuration.StartAddress);
            this.Poke(5, 0xc9); // ret
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void BDOS()
        {
            switch (this.CPU.C)
            {
                case 0x2:
                    System.Console.Out.Write(this.CPU.E.ToString(CultureInfo.InvariantCulture));
                    break;
                case 0x9:
                    for (var i = this.CPU.DE.Word; this.Peek(i) != '$'; ++i)
                    {
                        System.Console.Out.Write((char)this.Peek(i));
                    }

                    break;
                default:
                    break;
            }
        }

        private void CPU_ExecutingInstruction_CPM(object? sender, System.EventArgs e)
        {
            if (this.CPU.PC.High != 0)
            {
                // We're only interested in zero page
                return;
            }
            switch (this.CPU.PC.Low)
            {
                case 0x0: // CP/M warm start
                    if (++this.warmstartCount == 2)
                    {
                        this.LowerPOWER();
                    }

                    break;
                case 0x5: // BDOS
                    this.BDOS();
                    break;
                default:
                    break;
            }
        }

        private void CPU_LoweredHALT(object? sender, System.EventArgs e) => this.LowerPOWER();

        private void CPU_ExecutingInstruction_Debug(object? sender, System.EventArgs e) => System.Console.Error.WriteLine($"{Disassembler.State(this.CPU)}\t{this.disassembler.Disassemble(this.CPU)}");
    }
}
