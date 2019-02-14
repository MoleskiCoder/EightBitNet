// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using EightBit;

    internal class Configuration
    {
        private readonly Register16 loadAddress = new Register16(0x400);
        private readonly Register16 startAddress = new Register16(0x400);
        private readonly string romDirectory = "roms";
        private readonly string program = "6502_functional_test.bin";
        private bool debugMode = false;

        public Configuration()
        {
        }

        public bool DebugMode
        {
            get => this.debugMode;
            set => this.debugMode = value;
        }

        public Register16 LoadAddress { get => this.loadAddress; }

        public Register16 StartAddress { get => this.startAddress; }

        public string RomDirectory { get => this.romDirectory; }

        public string Program { get => this.program; }
    }
}