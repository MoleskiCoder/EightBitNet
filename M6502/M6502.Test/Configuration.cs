// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using EightBit;

    internal class Configuration
    {
        public bool DebugMode { get; set; }

        public bool Profile { get; set; }

        // Sudoku
        public string Program { get; } = "sudoku.65b";
        public string Symbols { get; } = "sudoku.dbg";
        public Register16 InputAddress { get; } = new Register16(0xe000);
        public Register16 OutputAddress { get; } = new Register16(0xe001);
        public Register16 LoadAddress { get; } = new Register16(0xf000);
        public Register16 StartAddress { get; } = new Register16(0xf000);
        public bool AllowKeyRead { get; }
        public bool BreakOnKeyRead { get; }

        // Klaus
        //public string Program { get; } = "6502_functional_test.bin";
        //public string Symbols { get; } = "6502_functional_test.dbg";
        //public Register16 InputAddress { get; } = new Register16(0xf004);
        //public Register16 OutputAddress { get; } = new Register16(0xf001);
        //public Register16 LoadAddress { get; } = new Register16(0x0);
        //public Register16 StartAddress { get; } = new Register16(0x400);
        //public bool AllowKeyRead { get; } = true;
        //public bool BreakOnKeyRead { get; } = true;

        public int PollingTickInterval { get; } = 10000000;

        public string RomDirectory { get; } = "roms";
    }
}