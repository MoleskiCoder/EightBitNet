// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using EightBit;

    internal class Configuration
    {
        public Configuration()
        {
        }

        public bool DebugMode { get; set; } = false;

        public Register16 LoadAddress { get; } = new Register16(0x400);

        public Register16 StartAddress { get; } = new Register16(0x400);

        public string RomDirectory { get; } = "roms";

        public string Program { get; } = "6502_functional_test.bin";
    }
}