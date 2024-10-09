// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.Test
{
    using EightBit;

    internal sealed class Configuration
    {
        public Configuration()
        {
        }

        public bool DebugMode { get; set; }

        public Register16 LoadAddress { get; } = new(0x100);

        public Register16 StartAddress { get; } = new(0x100);

        public string RomDirectory { get; } = "roms";

        public string Program { get; } = "zexall.com";
    }
}