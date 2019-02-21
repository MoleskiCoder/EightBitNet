// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using System.Text;
    using EightBit;

    internal class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram;
        private readonly Symbols symbols;
        private readonly Disassembler disassembler;
        private readonly MemoryMapping mapping;

        private ushort oldPC;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.ram = new Ram(0x10000);
            this.CPU = new M6502(this);
            this.symbols = new Symbols();
            this.disassembler = new Disassembler(this, this.CPU, this.symbols);
            this.mapping = new MemoryMapping(this.ram, 0x0000, (ushort)Mask.Mask16, AccessLevel.ReadWrite);

            this.oldPC = (ushort)Mask.Mask16;
        }

        public M6502 CPU { get; }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
            this.CPU.RaiseSO();
            this.CPU.RaiseRDY();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            var programFilename = this.configuration.Program;
            var programPath = this.configuration.RomDirectory + "/" + this.configuration.Program;
            var loadAddress = this.configuration.LoadAddress;
            this.ram.Load(programPath, loadAddress.Word);

            if (this.configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction;
            }

            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;

            this.Poke(0x00, 0x4c);
            this.CPU.PokeWord(0x01, this.configuration.StartAddress);
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void CPU_ExecutedInstruction(object sender, System.EventArgs e)
        {
            var pc = this.CPU.PC.Word;
            if (this.oldPC != pc)
            {
                this.oldPC = pc;
            }
            else
            {
                this.LowerPOWER();
                var test = this.Peek(0x0200);
                System.Console.Out.WriteLine();
                System.Console.Out.Write("** Test=");
                System.Console.Out.WriteLine(Disassembler.Dump_ByteValue(test));
            }
        }

        private void CPU_ExecutingInstruction(object sender, System.EventArgs e)
        {
            var address = this.CPU.PC.Word;
            var cell = this.Peek(address);

            var output = new StringBuilder();

            output.Append("PC=");
            output.Append(Disassembler.Dump_WordValue(address));
            output.Append(":");

            output.Append("P=");
            output.Append(Disassembler.Dump_Flags(this.CPU.P));
            output.Append(", ");

            output.Append("A=");
            output.Append(Disassembler.Dump_ByteValue(this.CPU.A));
            output.Append(", ");

            output.Append("X=");
            output.Append(Disassembler.Dump_ByteValue(this.CPU.X));
            output.Append(", ");

            output.Append("Y=");
            output.Append(Disassembler.Dump_ByteValue(this.CPU.Y));
            output.Append(", ");

            output.Append("S=");
            output.Append(Disassembler.Dump_ByteValue(this.CPU.S));
            output.Append("\t");

            output.Append(this.disassembler.Disassemble(address));

            System.Console.Out.WriteLine(output.ToString());
        }
    }
}
