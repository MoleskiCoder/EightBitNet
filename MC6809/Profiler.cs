// <copyright file="Profiler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    using System.Diagnostics;

    using EightBit;

    public sealed class Profiler(Bus board, MC6809 processor, Disassembler disassembler)
    {
        private readonly long[] instructionCounts = new long[0x10000];
        private readonly long[] addressProfiles = new long[0x10000];
        private readonly long[] addressCounts = new long[0x10000];

        private readonly Bus board = board;
        private readonly MC6809 processor = processor;
        private readonly Disassembler disassembler = disassembler;

        private ushort _executingAddress;

        private long _totalCycles = -1;
        private bool _totalCyclesValid;


        public event EventHandler<EventArgs>? StartingOutput;

        public event EventHandler<EventArgs>? FinishedOutput;

        public event EventHandler<EventArgs>? StartingLineOutput;

        public event EventHandler<EventArgs>? FinishedLineOutput;

        public event EventHandler<ProfileLineEventArgs>? EmitLine;

        public long TotalCycles
        {
            get
            {
                Debug.Assert(this._totalCyclesValid);
                return this._totalCycles;
            }
            private set
            {
                Debug.Assert(!this._totalCyclesValid);
                this._totalCycles = value;
                this._totalCyclesValid = true;
            }
        }

        public void Enable()
        {
            this.processor.ExecutingInstruction += this.Processor_ExecutingInstruction;
            this.processor.ExecutedInstruction += this.Processor_ExecutedInstruction;
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
            this.TotalCycles = addressProfiles.Sum();

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
                        var disassembled = this.disassembler.Disassemble(address);
                        var output = $"{address:x4}|{disassembled}";
                        this.OnEmitLine(output, cycles);
                    }
                }
            }
            finally
            {
                this.OnFinishedLineOutput();
            }
        }

        private void Processor_ExecutingInstruction(object? sender, EventArgs e) => this._executingAddress = this.processor.PC.Joined;

        private void Processor_ExecutedInstruction(object? sender, EventArgs e)
        {
            ushort opcode = this.board.Peek(this._executingAddress);
            if (opcode == 0x10 || opcode == 0x11)
            {
                opcode *= 0x100;
                opcode += this.board.Peek((ushort)(this._executingAddress + 1));
            }

            this.addressCounts[this._executingAddress]++;
            this.instructionCounts[opcode]++;

            this.addressProfiles[this._executingAddress] += this.processor.Cycles;
        }

        private void OnStartingOutput() => this.StartingOutput?.Invoke(this, EventArgs.Empty);

        private void OnFinishedOutput() => this.FinishedOutput?.Invoke(this, EventArgs.Empty);

        private void OnStartingLineOutput() => this.StartingLineOutput?.Invoke(this, EventArgs.Empty);

        private void OnFinishedLineOutput() => this.FinishedLineOutput?.Invoke(this, EventArgs.Empty);

        private void OnEmitLine(string source, long cycles) => this.EmitLine?.Invoke(this, new ProfileLineEventArgs(source, cycles));
    }
}