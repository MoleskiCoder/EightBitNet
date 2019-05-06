// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Configuration
    {
        public static readonly ulong CyclesPerSecond = 2 * 1024 * 1024;
        public static readonly ulong FrameCycleInterval = CyclesPerSecond / 60;
        public static readonly ulong TerminationCycles = CyclesPerSecond * 10 * 10;

        public bool DebugMode { get; set;  } = false;

        public bool TerminatesEarly { get; } = false;

        public string RomDirectory { get; } = "roms\\searle";
    }
}