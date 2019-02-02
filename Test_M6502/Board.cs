namespace Test
{
    using EightBit;
    using System.Text;

    internal class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram;
        private readonly M6502 cpu;
        private readonly Symbols symbols;
        private readonly Disassembly disassembler;

        private Register16 oldPC;
        private bool stopped;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.ram = new Ram(0x10000);
            this.cpu = new M6502(this);
            this.symbols = new Symbols();
            this.disassembler = new Disassembly(this, this.cpu, this.symbols);

            this.oldPC = new Register16((ushort)Mask.Mask16);
            this.stopped = false;
        }

        public M6502 CPU { get { return this.cpu; } }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            CPU.RaisePOWER();
            CPU.RaiseRESET();
            CPU.RaiseINT();
            CPU.RaiseNMI();
            CPU.RaiseSO();
            CPU.RaiseRDY();
        }

        public override void LowerPOWER()
        {
            CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            var programFilename = configuration.Program;
            var programPath = configuration.RomDirectory + "/" + configuration.Program;
            var loadAddress = configuration.LoadAddress;
            ram.Load(programPath, loadAddress.Word);

            if (configuration.DebugMode)
                CPU.ExecutingInstruction += CPU_ExecutingInstruction;

            CPU.ExecutedInstruction += CPU_ExecutedInstruction;

            Poke(0x00, 0x4c);
            Poke(0x01, configuration.StartAddress.Low);
            Poke(0x02, configuration.StartAddress.High);
        }

        private void CPU_ExecutedInstruction(object sender, System.EventArgs e)
        {
            var pc = CPU.PC;
            if (oldPC != pc)
            {
                oldPC = pc;
            }
            else
            {
                LowerPOWER();
                var test = Peek(0x0200);
                System.Console.Out.WriteLine();
                System.Console.Out.Write("** Test=");
                System.Console.Out.WriteLine(Disassembly.Dump_ByteValue(test));
            }
        }

        private void CPU_ExecutingInstruction(object sender, System.EventArgs e)
        {
            var address = CPU.PC;
            var cell = Peek(address);

            var output = new StringBuilder();

            output.Append("PC=");
            output.Append(Disassembly.Dump_WordValue(address));
            output.Append(":");

            output.Append("P=");
            output.Append(Disassembly.Dump_Flags(CPU.P));
            output.Append(", ");

            output.Append("A=");
            output.Append(Disassembly.Dump_ByteValue(CPU.A));
            output.Append(", ");

            output.Append("X=");
            output.Append(Disassembly.Dump_ByteValue(CPU.X));
            output.Append(", ");

            output.Append("Y=");
            output.Append(Disassembly.Dump_ByteValue(CPU.Y));
            output.Append(", ");

            output.Append("S=");
            output.Append(Disassembly.Dump_ByteValue(CPU.S));
            output.Append("\t");

            output.Append(disassembler.Disassemble(address.Word));

            System.Console.Out.WriteLine(output.ToString());
        }

        public override MemoryMapping Mapping(Register16 absolute)
        {
            return new MemoryMapping(ram, 0x0000, (ushort)Mask.Mask16, AccessLevel.ReadWrite);
        }
    }
}
