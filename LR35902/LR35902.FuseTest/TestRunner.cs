namespace Fuse
{
    public class TestRunner : EightBit.GameBoy.Bus
    {
        private readonly Test test;
        private readonly Result result;
        private readonly EightBit.Ram ram = new EightBit.Ram(0x10000);

        public TestRunner(Test test, Result result)
        {
            this.test = test;
            this.result = result;
        }

        public bool Failed { get; private set; } = false;

        public bool Unimplemented { get; private set; } = false;

        public override EightBit.MemoryMapping Mapping(ushort address) => new EightBit.MemoryMapping(this.ram, 0, EightBit.Mask.Mask8, EightBit.AccessLevel.ReadWrite);

        public void Run()
        {
            this.RaisePOWER();
            this.Initialize();
            var allowedCycles = this.test.RegisterState.TStates;
            try
            {
                this.CPU.Run(allowedCycles);
                this.Check();
            }
            catch (System.InvalidOperationException error)
            {
                this.Unimplemented = true;
                System.Console.Error.WriteLine($"**** Error: {error.Message}");
            }
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseRESET();
            this.CPU.RaiseINT();
            this.InitialiseRegisters();
            this.InitialiseMemory();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize() => this.DisableGameRom();

        private void InitialiseRegisters()
        {
            var testState = this.test.RegisterState;
            var inputRegisters = testState.Registers;

            this.CPU.AF.Word = inputRegisters[(int)RegisterState.Register.AF].Word;
            this.CPU.BC.Word = inputRegisters[(int)RegisterState.Register.BC].Word;
            this.CPU.DE.Word = inputRegisters[(int)RegisterState.Register.DE].Word;
            this.CPU.HL.Word = inputRegisters[(int)RegisterState.Register.HL].Word;

            this.CPU.SP.Word = inputRegisters[(int)RegisterState.Register.SP].Word;
            this.CPU.PC.Word = inputRegisters[(int)RegisterState.Register.PC].Word;
        }

        private void InitialiseMemory()
        {
            foreach (var memoryDatum in this.test.MemoryData)
            {
                var address = memoryDatum.Address;
                foreach (var seed in memoryDatum.Bytes)
                {
                    this.Poke(address++, seed);
                }
            }
        }

        private void Check()
        {
            this.Checkregisters();
            this.CheckMemory();
        }

        private void Checkregisters()
        {
            var expectedState = this.result.RegisterState;
            var expectedRegisters = expectedState.Registers;

            var af = this.CPU.AF.Word == expectedRegisters[(int)RegisterState.Register.AF].Word;
            var bc = this.CPU.BC.Word == expectedRegisters[(int)RegisterState.Register.BC].Word;
            var de = this.CPU.DE.Word == expectedRegisters[(int)RegisterState.Register.DE].Word;
            var hl = this.CPU.HL.Word == expectedRegisters[(int)RegisterState.Register.HL].Word;

            var sp = this.CPU.SP.Word == expectedRegisters[(int)RegisterState.Register.SP].Word;
            var pc = this.CPU.PC.Word == expectedRegisters[(int)RegisterState.Register.PC].Word;

            var success = af && bc && de && hl && sp && pc;
            if (!success)
            {
                this.Failed = true;
                System.Console.Error.WriteLine($"**** Failed test (Register): {this.test.Description}");

                if (!af)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.AF];
                    var actualWord = this.CPU.AF;
                    DumpDifference("A", "F", expectedWord, actualWord);
                }

                if (!bc)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.BC];
                    var actualWord = this.CPU.BC;
                    DumpDifference("B", "C", expectedWord, actualWord);
                }

                if (!de)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.DE];
                    var actualWord = this.CPU.DE;
                    DumpDifference("D", "E", expectedWord, actualWord);
                }

                if (!hl)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.HL];
                    var actualWord = this.CPU.HL;
                    DumpDifference("H", "L", expectedWord, actualWord);
                }

                if (!sp)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.SP];
                    var actualWord = this.CPU.SP;
                    DumpDifference("SPH", "SPL", expectedWord, actualWord);
                }

                if (!pc)
                {
                    var expectedWord = expectedRegisters[(int)RegisterState.Register.PC];
                    var actualWord = this.CPU.PC;
                    DumpDifference("PCH", "PCL", expectedWord, actualWord);
                }
            }
        }

        private void CheckMemory()
        {
            var first = true;

            foreach (var memoryDatum in this.result.MemoryData)
            {
                var address = memoryDatum.Address;
                foreach (var expected in memoryDatum.Bytes)
                {
                    var actual = this.Peek(address);
                    if (expected != actual)
                    {
                        this.Failed = true;
                        if (first)
                        {
                            first = false;
                            System.Console.Error.WriteLine($"**** Failed test (Memory): {this.test.Description}");
                        }

                        System.Console.Error.WriteLine($"**** Difference: Address: {address:x4} Expected: {expected:x2} Actual: {actual:x2}");
                    }
                    ++address;
                }
            }
        }

        private static void DumpDifference(string description, byte expected, byte actual)
        {
            var output = $"**** {description}, Expected: {expected:x2}, Got {actual:x2}";
            System.Console.Error.WriteLine(output);
        }

        private static void DumpDifference(string highDescription, string lowDescription, EightBit.Register16 expected, EightBit.Register16 actual)
        {
            var expectedHigh = expected.High;
            var expectedLow = expected.Low;

            var actualHigh = actual.High;
            var actualLow = actual.Low;

            if (expectedHigh != actualHigh)
            {
                DumpDifference(highDescription, actualHigh, expectedHigh);
            }

            if (expectedLow != actualLow)
            {
                DumpDifference(lowDescription, actualLow, expectedLow);
            }
        }
    }
}
