// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using System.Globalization;
    using System.Text;
    using EightBit;

    internal class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram;
        private readonly EightBit.Files.Symbols.Parser symbols;
        private readonly Disassembler disassembler;
        private readonly Profiler profiler;
        private readonly MemoryMapping mapping;

        private ushort oldPC;
        private int cyclesPolled;

        private char key;
        private bool keyHandled;
        private bool keyAvailable;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.ram = new Ram(0x10000);
            this.CPU = new M6502(this);
            this.symbols = new EightBit.Files.Symbols.Parser();
            this.disassembler = new Disassembler(this, this.CPU, this.symbols);
            this.mapping = new MemoryMapping(this.ram, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);

            this.oldPC = (ushort)Mask.Sixteen;

            this.symbols.Parse(string.IsNullOrEmpty(this.configuration.Symbols) ? string.Empty : this.configuration.RomDirectory + "/"  + this.configuration.Symbols);

            this.profiler = new Profiler(this.CPU, this.disassembler, this.symbols, this.configuration.Profile, this.configuration.Profile);
            if (this.configuration.Profile)
            {
                this.profiler.StartingOutput += this.Profiler_StartingOutput;
                this.profiler.FinishedOutput += this.Profiler_FinishedOutput;
                this.profiler.StartingLineOutput += this.Profiler_StartingLineOutput;
                this.profiler.FinishedLineOutput += this.Profiler_FinishedLineOutput;
                this.profiler.StartingScopeOutput += this.Profiler_StartingScopeOutput;
                this.profiler.FinishedScopeOutput += this.Profiler_FinishedScopeOutput;
                this.profiler.EmitLine += this.Profiler_EmitLine;
                this.profiler.EmitScope += this.Profiler_EmitScope;
            }
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
            if (this.configuration.Profile)
            {
                this.profiler.Generate();
            }
        }

        public override void Initialize()
        {
            var programPath = this.configuration.RomDirectory + "/" + this.configuration.Program;
            var loadAddress = this.configuration.LoadAddress;
            this.ram.Load(programPath, loadAddress.Word);

            if (this.configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction_Debugging;
            }

            if (this.configuration.AllowKeyRead)
            {
                this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction_Polling;
            }

            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;

            this.CPU.Bus.WrittenByte += this.Bus_WrittenByte;

            if (this.configuration.AllowKeyRead)
            {
                this.CPU.Bus.ReadingByte += this.Bus_ReadingByte;
                this.CPU.Bus.ReadByte += this.Bus_ReadByte;
            }

            this.Poke(0x00, 0x4c);
            this.CPU.PokeWord(0x01, this.configuration.StartAddress);
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void Bus_ReadingByte(object? sender, EventArgs e)
        {
            var address = this.CPU.Bus.Address;
            if (address == this.configuration.InputAddress)
            {
                var ready = this.keyAvailable && !this.keyHandled;
                if (ready && (this.CPU.Bus.Peek(address) == 0))
                {
                    this.CPU.Bus.Poke(address, (byte)this.key);
                    this.keyHandled = true;
                }
            }
        }

        private void Bus_ReadByte(object? sender, EventArgs e)
        {
            var address = this.CPU.Bus.Address;
            if (address == this.configuration.InputAddress)
            {
                if (this.configuration.BreakOnKeyRead)
                {
                    this.LowerPOWER();
                }
                else
                {
                    if (this.keyHandled)
                    {
                        this.CPU.Bus.Poke(address, 0);
                        this.keyAvailable = false;
                    }
                }
            }
        }

        private void Bus_WrittenByte(object? sender, EventArgs e)
        {
            if (this.CPU.Bus.Address == this.configuration.OutputAddress)
            {
                var contents = this.CPU.Bus.Peek(this.CPU.Bus.Address);
                Console.Out.Write((char)contents);
            }
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e)
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
                System.Console.Out.WriteLine(Disassembler.DumpByteValue(test));
            }
        }

        private void CPU_ExecutedInstruction_Polling(object? sender, EventArgs e)
        {
            var cycles = this.CPU.Cycles;
            this.cyclesPolled += cycles;
            System.Diagnostics.Debug.Assert(cycles > 0, "Invalid pollingcycle count");
            if (this.cyclesPolled > this.configuration.PollingTickInterval)
            {
                this.cyclesPolled = 0;
                this.PollHostKeyboard();
            }
        }

        private void PollHostKeyboard()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                this.key = key.KeyChar;
                this.keyAvailable = true;
                this.keyHandled = false;
            }
        }

        private void CPU_ExecutingInstruction_Debugging(object? sender, EventArgs e)
        {
            var address = this.CPU.PC.Word;

            var output = new StringBuilder();

            output.Append("PC=");
            output.Append(Disassembler.DumpWordValue(address));
            output.Append(":");

            output.Append("P=");
            output.Append(Disassembler.DumpFlags(this.CPU.P));
            output.Append(", ");

            output.Append("A=");
            output.Append(Disassembler.DumpByteValue(this.CPU.A));
            output.Append(", ");

            output.Append("X=");
            output.Append(Disassembler.DumpByteValue(this.CPU.X));
            output.Append(", ");

            output.Append("Y=");
            output.Append(Disassembler.DumpByteValue(this.CPU.Y));
            output.Append(", ");

            output.Append("S=");
            output.Append(Disassembler.DumpByteValue(this.CPU.S));
            output.Append("\t");

            output.Append(this.disassembler.Disassemble(address));

            Console.Out.WriteLine(output.ToString());
        }

        private void Profiler_EmitScope(object? sender, ProfileScopeEventArgs e)
        {
            var proportion = (double)e.Cycles / this.CPU.Cycles;
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "\t[{0:P2}][{1:d9}][{2:d9}]\t{3}\n", proportion, e.Cycles, e.Count, e.Scope));
        }

        private void Profiler_EmitLine(object? sender, ProfileLineEventArgs e)
        {
            var proportion = (double)e.Cycles / this.profiler.TotalCycleCount;
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "\t[{0:P2}][{1:d9}]\t{2}\n", proportion, e.Cycles, e.Source));
        }

        private void Profiler_FinishedScopeOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Finished profiler scope output...\n");
        }

        private void Profiler_StartingScopeOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Starting profiler scope output...\n");
        }

        private void Profiler_FinishedLineOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Finished profiler line output...\n");
        }

        private void Profiler_StartingLineOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Starting profiler line output...\n");
        }

        private void Profiler_FinishedOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Finished profiler output...\n");
        }

        private void Profiler_StartingOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Starting profiler output...\n");
        }
    }
}
