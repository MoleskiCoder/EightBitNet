namespace M6502
{
    using System.Diagnostics;

    public sealed class Profiler
    {
        private readonly long[] instructionCounts = new long[0x100];
        private readonly long[] instructionCycles = new long[0x100];
        private readonly Dictionary<int, long>[] addressCycleDistributions = new Dictionary<int, long>[0x10000];  // Addresses -> cycles -> counts
        private readonly Dictionary<int, long> scopeCycles = []; // ID -> Cycles

        private readonly MOS6502 processor;
        private readonly Disassembler disassembler;
        private readonly Symbols.Parser symbols;

        private ushort executingAddress;
        private byte executingInstruction;

        private long totalCycles = -1;
        private bool totalCyclesValid;

        public Profiler(MOS6502 processor, Disassembler disassembler, Symbols.Parser symbols, bool activate)
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
                Debug.Assert(this.totalCyclesValid);
                return this.totalCycles;
            }
            private set
            {
                Debug.Assert(!this.totalCyclesValid);
                this.totalCycles = value;
                this.totalCyclesValid = true;
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
                    Debug.Assert(symbol is not null);
                    var available = this.ExtractCycleDistribution((ushort)symbol.Value, out var _, out var _, out var count);
                    Debug.Assert(available);
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
                    var address = (ushort)i;
                    var available = this.ExtractCycleDistribution(address, out var cycleDistributions, out var cycles, out var count);
                    if (available)
                    {
                        // Dump a profile/disassembly line
                        var source = this.disassembler.Disassemble(address);
                        Debug.Assert(cycleDistributions is not null);
                        this.OnEmitLine(address, source, cycles, count, cycleDistributions);
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
            this.executingAddress = this.processor.Bus.Address.Word;
            ++this.instructionCounts[this.executingInstruction = this.processor.Bus.Data];
        }

        private void Processor_ExecutedInstruction(object? sender, EventArgs e)
        {
            var cycles = this.processor.Cycles;

            {
                var addressDistribution = this.addressCycleDistributions[this.executingAddress];
                if (addressDistribution is null)
                {
                    this.addressCycleDistributions[this.executingAddress] = addressDistribution = [];
                }
                _ = addressDistribution.TryGetValue(cycles, out var current);
                addressDistribution[cycles] = ++current;
            }

            this.instructionCycles[this.executingInstruction] += cycles;

            {
                var scope = this.symbols.LookupScopeByAddress(this.executingAddress);
                if (scope is not null)
                {
                    var id = scope.ID;
                    // Current will be initialised to zero, if absent
                    _ = this.scopeCycles.TryGetValue(id, out var current);
                    this.scopeCycles[id] = current + cycles;
                }
            }
        }

        private bool ExtractCycleDistribution(ushort address, out Dictionary<int, long>? cycleDistribution, out long cycleCount, out long hitCount)
        {
            cycleDistribution = this.addressCycleDistributions[address];
            if (cycleDistribution is null)
            {
                cycleCount = -1;
                hitCount = -1;
                return false;
            }

            Debug.Assert(cycleDistribution.Count > 0);

            cycleCount = hitCount = 0L;
            foreach (var (cycle, count) in cycleDistribution)
            {
                hitCount += count;
                cycleCount += cycle * count;
            }

            Debug.Assert(hitCount > 0);
            Debug.Assert(cycleCount > 0);

            return true;
        }

        private void OnStartingOutput()
        {
            StartingOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedOutput()
        {
            FinishedOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingLineOutput()
        {
            StartingLineOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedLineOutput()
        {
            FinishedLineOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingInstructionOutput()
        {
            StartingInstructionOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedInstructionOutput()
        {
            FinishedInstructionOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartingScopeOutput()
        {
            StartingScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinishedScopeOutput()
        {
            FinishedScopeOutput?.Invoke(this, EventArgs.Empty);
        }

        private void OnEmitLine(ushort address, string source, long cycles, long count, Dictionary<int, long> cycleDistributions)
        {
            EmitLine?.Invoke(this, new ProfileLineEventArgs(address, source, cycles, count, cycleDistributions));
        }

        private void OnEmitScope(int id, long cycles, long count)
        {
            EmitScope?.Invoke(this, new ProfileScopeEventArgs(id, cycles, count));
        }

        private void OnEmitInstruction(byte instruction, long cycles, long count)
        {
            EmitInstruction?.Invoke(this, new ProfileInstructionEventArgs(instruction, cycles, count));
        }
    }
}