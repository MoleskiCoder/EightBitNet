namespace EightBit
{
    public sealed class Profiler
    {
        private readonly int[] instructionCounts;
        private readonly int[] addressProfiles;
        private readonly int[] addressCounts;

        private readonly Dictionary<string, int> scopeCycles;

        private readonly M6502 processor;
        private readonly Disassembler disassembler;
        private readonly Files.Symbols.Parser symbols;

        private int priorCycleCount;
        private ushort executingAddress;

        public Profiler(M6502 processor, Disassembler disassembler, Files.Symbols.Parser symbols, bool countInstructions, bool profileAddresses)
        {
            ArgumentNullException.ThrowIfNull(processor);
            ArgumentNullException.ThrowIfNull(disassembler);
            ArgumentNullException.ThrowIfNull(symbols);

            this.processor = processor;
            this.disassembler = disassembler;
            this.symbols = symbols;

            if (profileAddresses || countInstructions)
            {
                this.processor.ExecutingInstruction += this.Processor_ExecutingInstruction_Prequel;
                this.processor.ExecutedInstruction += this.Processor_ExecutedInstruction_Sequal;
            }
            if (profileAddresses)
            {
                this.processor.ExecutingInstruction += this.Processor_ExecutingInstruction_ProfileAddresses;
                this.processor.ExecutedInstruction += this.Processor_ExecutedInstruction_ProfileAddresses;
            }

            if (countInstructions)
            {
                this.processor.ExecutingInstruction += this.Processor_ExecutingInstruction_CountInstructions;
            }

            this.instructionCounts = new int[0x100];
            this.addressProfiles = new int[0x10000];
            this.addressCounts = new int[0x10000];

            this.scopeCycles = [];
        }


        public event EventHandler<EventArgs>? StartingOutput;

        public event EventHandler<EventArgs>? FinishedOutput;

        public event EventHandler<EventArgs>? StartingLineOutput;

        public event EventHandler<EventArgs>? FinishedLineOutput;

        public event EventHandler<ProfileLineEventArgs>? EmitLine;

        public event EventHandler<EventArgs>? StartingScopeOutput;

        public event EventHandler<EventArgs>? FinishedScopeOutput;

        public event EventHandler<ProfileScopeEventArgs>? EmitScope;

        public long TotalCycleCount { get; private set; }

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
            this.OnStartingLineOutput();
            try
            {
                // For each memory address
                for (var i = 0; i < 0x10000; ++i)
                {
                    // If there are any cycles associated
                    var cycles = this.addressProfiles[i];
                    if (cycles > 0)
                    {
                        var address = (ushort)i;

                        // Dump a profile/disassembly line
                        var source = this.disassembler.Disassemble(address);
                        this.OnEmitLine(source, cycles);
                    }
                }
            }
            finally
            {
                this.OnFinishedLineOutput();
            }

            this.OnStartingScopeOutput();
            try
            {
                foreach (var scopeCycle in this.scopeCycles)
                {
                    var name = scopeCycle.Key;
                    var cycles = scopeCycle.Value;
                    var count = this.addressCounts[this.symbols.LookupLabel(name).Value];
                    this.OnEmitScope(name, cycles, count);
                }
            }
            finally
            {
                this.OnFinishedScopeOutput();
            }
        }

        private void Processor_ExecutingInstruction_Prequel(object? sender, EventArgs e)
        {
            this.executingAddress = this.processor.PC.Word;
        }

        private void Processor_ExecutedInstruction_Sequal(object? sender, EventArgs e)
        {
            this.TotalCycleCount += this.processor.Cycles;
        }

        private void Processor_ExecutingInstruction_ProfileAddresses(object? sender, EventArgs e)
        {
            this.priorCycleCount = this.processor.Cycles;
            ++this.addressCounts[this.executingAddress];
        }

        private void Processor_ExecutingInstruction_CountInstructions(object? sender, EventArgs e)
        {
            ++this.instructionCounts[this.processor.Bus.Peek(this.executingAddress)];
        }

        private void Processor_ExecutedInstruction_ProfileAddresses(object? sender, EventArgs e)
        {
            var address = this.executingAddress;
            var cycles = this.processor.Cycles - this.priorCycleCount;

            this.addressProfiles[address] += cycles;

            var addressScope = this.symbols.LookupScope(address);
            if (addressScope != null)
            {
                if (!this.scopeCycles.ContainsKey(addressScope.Name))
                {
                    this.scopeCycles[addressScope.Name] = 0;
                }

                this.scopeCycles[addressScope.Name] += cycles;
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

        private void OnStartingScopeOutput()
        {
            this.StartingScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedScopeOutput()
        {
            this.FinishedScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnEmitLine(string source, int cycles)
        {
            this.EmitLine?.Invoke(this, new ProfileLineEventArgs(source, cycles));
        }

        private void OnEmitScope(string scope, int cycles, int count)
        {
            this.EmitScope?.Invoke(this, new ProfileScopeEventArgs(scope, cycles, count));
        }
    }
}