namespace Test
{
    using EightBit;

    internal class Configuration
    {
        private bool debugMode = false;
        private readonly Register16 loadAddress = new Register16(0x400);
        private readonly Register16 startAddress = new Register16(0x400);
        private readonly string romDirectory = "roms";
        private readonly string program = "6502_functional_test.bin";

        public Configuration() {}

        public bool DebugMode {
            get { return debugMode; }
            set { debugMode = value; }
        }

        public Register16 LoadAddress { get { return loadAddress; } }
        public Register16 StartAddress { get { return startAddress; } }

        public string RomDirectory { get { return romDirectory; } }
        public string Program { get { return program; } }
    }
}