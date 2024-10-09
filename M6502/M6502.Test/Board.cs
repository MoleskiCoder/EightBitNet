// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using EightBit;
    using M6502;

    internal class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram = new(0x10000);
        private readonly Symbols.Parser symbols = new();
        private readonly Disassembler disassembler;
        private readonly Profiler profiler;
        private readonly MemoryMapping mapping;

        private readonly Register16 oldPC = new((ushort)Mask.Sixteen);
        private int cyclesPolled;

        private char key;
        private bool keyHandled;
        private bool keyAvailable;

        private bool inputting = false;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.CPU = new(this);
            this.disassembler = new(this, this.CPU, this.symbols);
            this.mapping = new(this.ram, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadWrite);
            this.profiler = new(this.CPU, this.disassembler, this.symbols, this.configuration.Profile);
        }

        public MOS6502 CPU { get; }

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
            Task? symbolsParserTask = null;
            if (this.configuration.Profile || this.configuration.DebugMode)
            {
                symbolsParserTask = this.symbols.ParseAsync(string.IsNullOrEmpty(this.configuration.Symbols) ? string.Empty : this.configuration.RomDirectory + "/" + this.configuration.Symbols);
            }

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
                this.ReadingByte += this.Bus_ReadingByte;
                this.ReadByte += this.Bus_ReadByte;
            }

            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;
            this.WrittenByte += this.Bus_WrittenByte;

            if (this.configuration.Profile)
            {
                this.profiler.StartingOutput += this.Profiler_StartingOutput;
                this.profiler.FinishedOutput += this.Profiler_FinishedOutput;
                this.profiler.StartingLineOutput += this.Profiler_StartingLineOutput;
                this.profiler.FinishedLineOutput += this.Profiler_FinishedLineOutput;
                this.profiler.StartingScopeOutput += this.Profiler_StartingScopeOutput;
                this.profiler.FinishedScopeOutput += this.Profiler_FinishedScopeOutput;
                this.profiler.StartingInstructionOutput += this.Profiler_StartingInstructionOutput;
                this.profiler.FinishedInstructionOutput += this.Profiler_FinishedInstructionOutput;
                this.profiler.EmitLine += this.Profiler_EmitLine;
                this.profiler.EmitScope += this.Profiler_EmitScope;
                this.profiler.EmitInstruction += this.Profiler_EmitInstruction;
            }

            this.Poke(0x00, 0x4c);
            this.CPU.PokeWord(0x01, this.configuration.StartAddress);

            symbolsParserTask?.Wait();
        }

        public override MemoryMapping Mapping(ushort absolute) => this.mapping;

        private void Bus_ReadingByte(object? sender, EventArgs e)
        {
            this.inputting = this.Address == this.configuration.InputAddress;
            if (this.inputting && this.keyAvailable && !this.keyHandled)
            {
                if (this.Peek(this.Address) == 0)
                {
                    this.Poke(this.Address, (byte)this.key);
                    this.keyHandled = true;
                }
            }
        }

        private void Bus_ReadByte(object? sender, EventArgs e)
        {
            if (this.inputting)
            {
                if (this.configuration.BreakOnKeyRead)
                {
                    this.LowerPOWER();
                }
                else
                {
                    if (this.keyHandled)
                    {
                        this.Poke(this.Address, 0);
                        this.keyAvailable = false;
                    }
                }
            }
        }

        private void Bus_WrittenByte(object? sender, EventArgs e)
        {
            if (this.Address == this.configuration.OutputAddress)
            {
                var contents = this.Peek(this.Address);
                Console.Out.Write((char)contents);
            }
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e)
        {
            if (this.oldPC != this.CPU.PC)
            {
                this.oldPC.Assign(this.CPU.PC);
            }
            else
            {
                this.LowerPOWER();
                var test = this.Peek(0x0200);
                Console.Out.WriteLine();
                Console.Out.WriteLine($"** Test={Disassembler.DumpByteValue(test)}");
            }
        }

        private void CPU_ExecutedInstruction_Polling(object? sender, EventArgs e)
        {
            var cycles = this.CPU.Cycles;
            this.cyclesPolled += cycles;
            Debug.Assert(cycles > 0, "Invalid pollingcycle count");
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

            output.Append($"PC={Disassembler.DumpWordValue(address)}:");
            output.Append($"P={Disassembler.DumpFlags(this.CPU.P)}, ");
            output.Append($"A={Disassembler.DumpByteValue(this.CPU.A)}, ");
            output.Append($"X={Disassembler.DumpByteValue(this.CPU.X)}, ");
            output.Append($"Y={Disassembler.DumpByteValue(this.CPU.Y)}, ");
            output.Append($"S={Disassembler.DumpByteValue(this.CPU.S)}\t");
            output.Append(this.disassembler.Disassemble(address));

            Console.Out.WriteLine(output.ToString());
        }

        private void Profiler_EmitScope(object? sender, ProfileScopeEventArgs e)
        {
            var proportion = (double)e.Cycles / this.profiler.TotalCycles;
            var scope = this.symbols.LookupScopeByID(e.ID);
            Debug.Assert(scope != null);
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "\t[{0:P2}][{1:d9}][{2:d9}]\t{3}\n", proportion, e.Cycles, e.Count, scope.Name));
        }

        private void Profiler_EmitLine(object? sender, ProfileLineEventArgs e)
        {
            var proportion = (double)e.Cycles / this.profiler.TotalCycles;

            var cycleDistributions = e.CycleDistributions;
            Debug.Assert(cycleDistributions.Count > 0);
            var distributions = "\t#";
            foreach (var (cycles, count) in cycleDistributions)
            {
                distributions += $" {cycles}:{count:N0}";
            }

            var output = $"\t[{proportion:P2}][{e.Cycles:d9}][{e.Count:d9}]\t{Disassembler.DumpWordValue(e.Address)}:{e.Source}{distributions}";
            Console.Out.WriteLine(output);
        }

        private void Profiler_EmitInstruction(object? sender, ProfileInstructionEventArgs e)
        {
            var proportion = (double)e.Cycles / this.profiler.TotalCycles;
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "\t[{0:P2}][{1:d9}][{2:d9}]\t{3:X2}\n", proportion, e.Cycles, e.Count, e.Instruction));
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

        private void Profiler_FinishedInstructionOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Finished instruction output...\n");
        }

        private void Profiler_StartingInstructionOutput(object? sender, EventArgs e)
        {
            Console.Out.Write("Starting instruction output...\n");
        }
    }
}
