﻿namespace M6502
{
    public sealed class ProfileInstructionEventArgs(byte instruction, long cycles, long count) : EventArgs
    {
        public byte Instruction { get; } = instruction;

        public long Cycles { get; } = cycles;

        public long Count { get; } = count;
    }
}