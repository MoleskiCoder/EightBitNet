namespace EightBit
{
    using System.Diagnostics;

    public sealed class Profiler
    {
        private readonly long[] instructionCounts = new long[0x100];
        private readonly long[] instructionCycles = new long[0x100];
        private readonly long[] addressCycles = new long[0x10000];
        private readonly long[] addressCounts = new long[0x10000];

        private readonly Dictionary<int, long> scopeCycles = []; // ID -> Cycles

        private readonly M6502 processor;
        private readonly Disassembler disassembler;
        private readonly Files.Symbols.Parser symbols;

        private ushort executingAddress;
        private byte executingInstruction;

        private long totalCycles = -1;
        private bool totalCyclesValid;

        public Profiler(M6502 processor, Disassembler disassembler, Files.Symbols.Parser symbols, bool activate)
        {
            ArgumentNullException.ThrowIfNull(processor);
            ArgumentNullException.ThrowIfNull(disassembler);
            ArgumentNullException.ThrowIfNull(symbols);

            this.processor = processor;
            this.disassembler = disassembler;
            this.symbols = symbols;

            if (activate)
            {
                this.processor.RaisingSYNC += this.Processor_RaisingSYNC;
                this.processor.ExecutedInstruction += this.Processor_ExecutedInstruction;
            }
        }

        public event EventHandler<EventArgs>? StartingOutput;

        public event EventHandler<EventArgs>? FinishedOutput;

        public event EventHandler<EventArgs>? StartingLineOutput;

        public event EventHandler<EventArgs>? FinishedLineOutput;

        public event EventHandler<ProfileLineEventArgs>? EmitLine;

        public event EventHandler<EventArgs>? StartingInstructionOutput;

        public event EventHandler<EventArgs>? FinishedInstructionOutput;

        public event EventHandler<ProfileInstructionEventArgs>? EmitInstruction;

        public event EventHandler<EventArgs>? StartingScopeOutput;

        public event EventHandler<EventArgs>? FinishedScopeOutput;

        public event EventHandler<ProfileScopeEventArgs>? EmitScope;

        public long TotalCycles
        {
            get
            {
                return this.totalCycles;
            }
            private set
            {
                Debug.Assert(!this.totalCyclesValid);
                this.totalCycles = value;
            }
        }
        public void Generate()
        {
            this.OnStartingOutput();
            try
            {
                this.EmitProfileInformation();
            }
            finally
            {
                this.OnFinishedOutput();
            }
        }

        private void EmitProfileInformation()
        {
            this.TotalCycles = this.instructionCycles.Sum();
            this.totalCyclesValid = true;

            this.EmitProfileLineInformation();
            this.EmitProfileScopeInformation();
            this.EmitProfileInstructionInformation();
        }

        private void EmitProfileScopeInformation()
        {
            this.OnStartingScopeOutput();
            try
            {
                foreach (var (id, cycles) in this.scopeCycles)
                {
                    var symbol = this.symbols.LookupLabelByID(id);
                    Debug.Assert(symbol != null);
                    var count = this.addressCounts[symbol.Value];
                    this.OnEmitScope(id, cycles, count);
                }
            }
            finally
            {
                this.OnFinishedScopeOutput();
            }
        }

        private void EmitProfileLineInformation()
        {
            this.OnStartingLineOutput();
            try
            {
                // For each memory address
                for (var i = 0; i < 0x10000; ++i)
                {
                    // If there are any cycles associated
                    var cycles = this.addressCycles[i];
                    if (cycles > 0)
                    {
                        var count = this.addressCounts[i];
                        Debug.Assert(count > 0);
                        var address = (ushort)i;

                        // Dump a profile/disassembly line
                        var source = this.disassembler.Disassemble(address);
                        this.OnEmitLine(source, cycles, count);
                    }
                }
            }
            finally
            {
                this.OnFinishedLineOutput();
            }
        }

        private void EmitProfileInstructionInformation()
        {
            this.OnStartingInstructionOutput();
            try
            {
                // For each instruction
                for (var i = 0; i < 0x100; ++i)
                {
                    // If there are any cycles associated
                    var cycles = this.instructionCycles[i];
                    if (cycles > 0)
                    {
                        var count = this.instructionCounts[i];
                        Debug.Assert(count > 0);
                        var instruction = (byte)i;

                        // Emit an instruction event
                        this.OnEmitInstruction(instruction, cycles, count);
                    }
                }
            }
            finally
            {
                this.OnFinishedInstructionOutput();
            }
        }

        private void Processor_RaisingSYNC(object? sender, EventArgs e)
        {
            // Everything needs this
            this.executingAddress = this.processor.Bus.Address.Word;
            this.executingInstruction = this.processor.Bus.Peek(this.executingAddress);

            ++this.addressCounts[this.executingAddress];
            ++this.instructionCounts[this.processor.Bus.Data];
        }

        private void Processor_ExecutedInstruction(object? sender, EventArgs e)
        {
            var cycles = this.processor.Cycles;

            this.addressCycles[this.executingAddress] += cycles;
            this.instructionCycles[this.executingInstruction] += cycles;

            var scope = this.symbols.LookupScopeByAddress(this.executingAddress);
            if (scope != null)
            {
                var id = scope.ID;
                // Current will be initialised to zero, if absent
                _ = this.scopeCycles.TryGetValue(id, out var current);
                this.scopeCycles[id] = current + cycles;
            }
        }

        private void OnStartingOutput()
        {
            this.StartingOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedOutput()
        {
            this.FinishedOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingLineOutput()
        {
            this.StartingLineOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedLineOutput()
        {
            this.FinishedLineOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingInstructionOutput()
        {
            this.StartingInstructionOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedInstructionOutput()
        {
            this.FinishedInstructionOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingScopeOutput()
        {
            this.StartingScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedScopeOutput()
        {
            this.FinishedScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnEmitLine(string source, long cycles, long count)
        {
            this.EmitLine?.Invoke(this, new ProfileLineEventArgs(source, cycles, count));
        }

        private void OnEmitScope(int id, long cycles, long count)
        {
            this.EmitScope?.Invoke(this, new ProfileScopeEventArgs(id, cycles, count));
        }

        private void OnEmitInstruction(byte instruction, long cycles, long count)
        {
            this.EmitInstruction?.Invoke(this, new ProfileInstructionEventArgs(instruction, cycles, count));
        }
    }
}