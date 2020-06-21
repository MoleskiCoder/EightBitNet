// <copyright file="Profiler.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    using System;

    public sealed class Profiler
    {
        private readonly ulong[] instructionCounts;
        private readonly ulong[] addressProfiles;
        private readonly ulong[] addressCounts;

        private readonly Bus board;
        private readonly MC6809 processor;
        private readonly Disassembler disassembler;

        private ushort address;

        public Profiler(Bus board, MC6809 processor, Disassembler disassembler)
        {
            this.board = board;
            this.processor = processor;
            this.disassembler = disassembler;

            this.instructionCounts = new ulong[0x10000];
            this.addressProfiles = new ulong[0x10000];
            this.addressCounts = new ulong[0x10000];
        }

        public event EventHandler<EventArgs> StartingOutput;

        public event EventHandler<EventArgs> FinishedOutput;

        public event EventHandler<EventArgs> StartingLineOutput;

        public event EventHandler<EventArgs> FinishedLineOutput;

        public event EventHandler<ProfileLineEventArgs> EmitLine;

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

        private void Processor_ExecutingInstruction(object sender, EventArgs e) => this.address = this.processor.PC.Word;

        private void Processor_ExecutedInstruction(object sender, EventArgs e)
        {
            ushort opcode = this.board.Peek(this.address);
            if (opcode == 0x10 || opcode == 0x11)
            {
                opcode *= 0x100;
                opcode += this.board.Peek((ushort)(this.address + 1));
            }

            this.addressCounts[this.address]++;
            this.instructionCounts[opcode]++;

            this.addressProfiles[this.address] += (ulong)this.processor.Cycles;
        }

        private void OnStartingOutput() => this.StartingOutput?.Invoke(this, EventArgs.Empty);

        private void OnFinishedOutput() => this.FinishedOutput?.Invoke(this, EventArgs.Empty);

        private void OnStartingLineOutput() => this.StartingLineOutput?.Invoke(this, EventArgs.Empty);

        private void OnFinishedLineOutput() => this.FinishedLineOutput?.Invoke(this, EventArgs.Empty);

        private void OnEmitLine(string source, ulong cycles) => this.EmitLine?.Invoke(this, new ProfileLineEventArgs(source, cycles));
    }
}