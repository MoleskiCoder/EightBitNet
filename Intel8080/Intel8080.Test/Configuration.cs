// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Intel8080.Test
{
    using EightBit;

    internal class Configuration
    {
        public Configuration()
        {
        }

        public bool DebugMode { get; set; } = false;

        public Register16 LoadAddress { get; } = new Register16(0x100);

        public Register16 StartAddress { get; } = new Register16(0x100);

        public string RomDirectory { get; } = "roms";

        public string Program { get; } = "8080EX1.COM";
    }
}