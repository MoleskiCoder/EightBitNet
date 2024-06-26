﻿namespace EightBit
{
    using System.Diagnostics;

    public sealed class Profiler
    {
        private readonly int[] instructionCounts = new int[0x100];
        private readonly int[] addressProfiles = new int[0x10000];
        private readonly int[] addressCounts = new int[0x10000];

        private readonly Dictionary<int, int> scopeCycles = []; // ID -> Cycles

        private readonly M6502 processor;
        private readonly Disassembler disassembler;
        private readonly Files.Symbols.Parser symbols;

        private ushort executingAddress;

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

        private void Processor_RaisingSYNC(object? sender, EventArgs e)
        {
            // Everything needs this
            this.executingAddress = this.processor.Bus.Address.Word;

            ++this.addressCounts[this.executingAddress];
            ++this.instructionCounts[this.processor.Bus.Data];
        }

        private void Processor_ExecutedInstruction(object? sender, EventArgs e)
        {
            this.TotalCycleCount += this.processor.Cycles;

            this.addressProfiles[this.executingAddress] += this.processor.Cycles;

            var scope = this.symbols.LookupScopeByAddress(this.executingAddress);
            if (scope != null)
            {
                var id = scope.ID;
                if (!this.scopeCycles.TryAdd(id, 0))
                {
                    this.scopeCycles[id] += this.processor.Cycles;
                }
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

        private void OnEmitLine(string source, int cycles, int count)
        {
            this.EmitLine?.Invoke(this, new ProfileLineEventArgs(source, cycles, count));
        }

        private void OnEmitScope(int id, int cycles, int count)
        {
            this.EmitScope?.Invoke(this, new ProfileScopeEventArgs(id, cycles, count));
        }
    }
}