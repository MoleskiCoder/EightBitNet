namespace Test
{
    using EightBit;

    internal class Configuration
    {
        private bool debugMode = false;
        private readonly ushort loadAddress = 0x400;
        private readonly ushort startAddress = 0x400;
        private readonly string romDirectory = "roms";
        private readonly string program = "6502_functional_test.bin";

        public Configuration() {}

        public bool DebugMode {
            get { return debugMode; }
            set { debugMode = value; }
        }

        public ushort LoadAddress { get { return loadAddress; } }
        public ushort StartAddress { get { return startAddress; } }

        public string RomDirectory { get { return romDirectory; } }
        public string Program { get { return program; } }
    }
}