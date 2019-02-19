namespace Z80
{
    using EightBit;
    using System.Text;

    public class Disassembly
    {
        private bool prefixCB = false;
        private bool prefixDD = false;
        private bool prefixED = false;
        private bool prefixFD = false;
        private readonly Bus bus;

        public Disassembly(Bus bus)
        {
            this.bus = bus;
        }

        public Bus Bus => this.bus;

        public static string State(Z80 cpu)
        {
            var pc = cpu.PC();
            var sp = cpu.SP();

            var a = cpu.A();
            var f = cpu.F();

            var b = cpu.B();
            var c = cpu.C();

            var d = cpu.D();
            var e = cpu.E();

            var h = cpu.H();
            var l = cpu.L();

            var i = cpu.IV;
            var r = cpu.REFRESH();

            var im = cpu.IM;

            return
                  $"PC={pc} SP={sp} "
                + $"A={AsHex(a)} F={AsFlags(f)} "
                + $"B={AsHex(b)} C={AsHex(c)} "
                + $"D={AsHex(d)} E={AsHex(e)} "
                + $"H={AsHex(h)} L={AsHex(l)} "
                + $"I={AsHex(i)} R={AsHex(r)} "
                + $"IM={im}";
        }

        public string Disassemble(Z80 cpu)
        {
            this.prefixCB = this.prefixDD = this.prefixED = this.prefixFD = false;
            return Disassemble(cpu, cpu.PC().Word);
        }

        public static string flag(byte value, int flag, string represents)
        {
            return "";
        }

        public static string AsFlags(byte value)
        {
            return "";
        }

        public static string AsHex(byte value)
        {
            return "";
        }

        public static string AsHex(ushort value)
        {
            return "";
        }

        public static string AsBinary(byte value)
        {
            return "";
        }

        public static string AsDecimal(byte value)
        {
            return "";
        }

        public static string AsInvalid(byte value)
        {
            return "";
        }

		private string Disassemble(Z80 cpu, ushort pc)
        {
            var opCode = Bus.Peek(pc);

            var decoded = cpu.GetDecodedOpCode(opCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            var immediate = Bus.Peek((ushort)(pc + 1));
            var absolute = cpu.PeekWord((ushort)(pc + 1)).Word;
            var displacement = (sbyte)immediate;
            var relative = pc + displacement + 2;
            var indexedImmediate = Bus.Peek((ushort)(pc + 1));

            var dumpCount = 0;

            var output = $"{AsHex(opCode)}";

            var specification = "";

            if (this.prefixCB)
                output += this.DisassembleCB(
                    cpu, pc,
                    specification, ref dumpCount,
                    x, y, z, p, q);
            else if (this.prefixED)
                output += this.DisassembleED(
                    cpu, pc,
                    specification, ref dumpCount,
                    x, y, z, p, q);
            else
                output += this.DisassembleOther(
                    cpu, pc,
                    specification, ref dumpCount,
                    x, y, z, p, q);

            for (int i = 0; i < dumpCount; ++i)
                output += $"{AsHex(this.Bus.Peek((ushort)(pc + i + 1)))}";

            var outputFormatSpecification = !this.prefixDD;
            if (this.prefixDD)
            {
                if (opCode != 0xdd)
                {
                    outputFormatSpecification = true;
                }
            }

            if (outputFormatSpecification)
            {
                output += '\t';
                //m_formatter.parse(specification);
                //output << m_formatter % (int)immediate % (int)absolute % relative % (int)displacement % indexedImmediate;
            }

            return output;
        }

        private string DisassembleCB(
            Z80 cpu,
            ushort pc,
            string specification,
            ref int dumpCount,
            int x, int y, int z,
            int p, int q)
        {
            return "";
        }

        private string DisassembleED(
            Z80 cpu,
            ushort pc,
            string specification,
            ref int dumpCount,
            int x, int y, int z,
            int p, int q)
        {
            return "";
        }

        private string DisassembleOther(
            Z80 cpu,
            ushort pc,
            string specification,
            ref int dumpCount,
            int x, int y, int z,
            int p, int q)
        {
            return "";
        }

        private string RP(int rp)
        {
            return "";
        }

        private string RP2(int rp)
        {
            return "";
        }

        private string R(int r)
        {
            return "";
        }

        private static string CC(int flag)
        {
            return "";
        }

        private static string ALU(int which)
        {
            return "";
        }
    }
}
