// <copyright file="TestRunner.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Fuse
{
    public enum Register
    {
        AF,
        BC,
        DE,
        HL,
        AF_,
        BC_,
        DE_,
        HL_,
        IX,
        IY,
        SP,
        PC,
        MEMPTR,
    }

    public class TestRunner : EightBit.Bus
    {
        private readonly Test<RegisterState> test;
        private readonly Result<RegisterState> result;
        private readonly EightBit.Ram ram = new EightBit.Ram(0x10000);
        private readonly EightBit.InputOutput ports = new EightBit.InputOutput();
        private readonly EightBit.Z80 cpu;

        public TestRunner(Test<RegisterState> test, Result<RegisterState> result)
        {
            this.cpu = new EightBit.Z80(this, this.ports);
            this.test = test;
            this.result = result;
        }

        public bool Failed { get; private set; } = false;

        public bool Unimplemented { get; private set; } = false;

        public override EightBit.MemoryMapping Mapping(ushort address) => new EightBit.MemoryMapping(this.ram, 0, EightBit.Mask.Mask16, EightBit.AccessLevel.ReadWrite);

        public void Run()
        {
            this.RaisePOWER();
            this.Initialize();
            var allowedCycles = this.test.RegisterState.TStates;
            try
            {
                this.cpu.Run(allowedCycles);
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
            this.cpu.RaisePOWER();
            this.cpu.RaiseRESET();
            this.cpu.RaiseINT();
            this.cpu.RaiseNMI();
            this.InitialiseRegisters();
            this.InitialiseMemory();
        }

        public override void LowerPOWER()
        {
            this.cpu.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
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

        private void InitialiseRegisters()
        {
            var testState = this.test.RegisterState;
            var inputRegisters = testState.Registers;

            this.cpu.AF.Word = inputRegisters[(int)Register.AF_].Word;
            this.cpu.BC.Word = inputRegisters[(int)Register.BC_].Word;
            this.cpu.DE.Word = inputRegisters[(int)Register.DE_].Word;
            this.cpu.HL.Word = inputRegisters[(int)Register.HL_].Word;
            this.cpu.Exx();
            this.cpu.ExxAF();
            this.cpu.AF.Word = inputRegisters[(int)Register.AF].Word;
            this.cpu.BC.Word = inputRegisters[(int)Register.BC].Word;
            this.cpu.DE.Word = inputRegisters[(int)Register.DE].Word;
            this.cpu.HL.Word = inputRegisters[(int)Register.HL].Word;

            this.cpu.IX.Word = inputRegisters[(int)Register.IX].Word;
            this.cpu.IY.Word = inputRegisters[(int)Register.IY].Word;

            this.cpu.SP.Word = inputRegisters[(int)Register.SP].Word;
            this.cpu.PC.Word = inputRegisters[(int)Register.PC].Word;

            this.cpu.MEMPTR.Word = inputRegisters[(int)Register.MEMPTR].Word;

            this.cpu.IV = (byte)testState.I;
            this.cpu.REFRESH = (byte)testState.R;
            this.cpu.IFF1 = testState.IFF1;
            this.cpu.IFF2 = testState.IFF2;
            this.cpu.IM = testState.IM;
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

            var af = this.cpu.AF.Word == expectedRegisters[(int)Register.AF].Word;
            var bc = this.cpu.BC.Word == expectedRegisters[(int)Register.BC].Word;
            var de = this.cpu.DE.Word == expectedRegisters[(int)Register.DE].Word;
            var hl = this.cpu.HL.Word == expectedRegisters[(int)Register.HL].Word;

            this.cpu.Exx();
            this.cpu.ExxAF();

            var af_ = this.cpu.AF.Word == expectedRegisters[(int)Register.AF_].Word;
            var bc_ = this.cpu.BC.Word == expectedRegisters[(int)Register.BC_].Word;
            var de_ = this.cpu.DE.Word == expectedRegisters[(int)Register.DE_].Word;
            var hl_ = this.cpu.HL.Word == expectedRegisters[(int)Register.HL_].Word;

            var ix = this.cpu.IX.Word == expectedRegisters[(int)Register.IX].Word;
            var iy = this.cpu.IY.Word == expectedRegisters[(int)Register.IY].Word;

            var sp = this.cpu.SP.Word == expectedRegisters[(int)Register.SP].Word;
            var pc = this.cpu.PC.Word == expectedRegisters[(int)Register.PC].Word;

            var memptr = this.cpu.MEMPTR.Word == expectedRegisters[(int)Register.MEMPTR].Word;

            var iv = this.cpu.IV == expectedState.I;
            var refresh = this.cpu.REFRESH == expectedState.R;
            var iff1 = this.cpu.IFF1 == expectedState.IFF1;
            var iff2 = this.cpu.IFF2 == expectedState.IFF2;
            var im = this.cpu.IM == expectedState.IM;

            // And back again, so the following works as expected...
            this.cpu.Exx();
            this.cpu.ExxAF();

            var success =
                af && bc && de && hl
                && af_ && bc_ && de_ && hl_
                && ix && iy
                && sp && pc
                && iv && refresh
                && iff1 && iff2
                && im
                && memptr;

            if (!success)
            {
                this.Failed = true;
                System.Console.Error.WriteLine($"**** Failed test (Register): {this.test.Description}");

                if (!af)
                {
                    var expectedA = expectedRegisters[(int)Register.AF].High;
                    var gotA = this.cpu.A;
                    if (expectedA != gotA)
                    {
                        DumpDifference("A", expectedA, gotA);
                    }

                    var expectedF = expectedRegisters[(int)Register.AF].Low;
                    var gotF = this.cpu.F;
                    if (expectedF != gotF)
                    {
                        var output = $"**** F, Expected: {EightBit.Disassembler.AsFlags(expectedF)}, Got: {EightBit.Disassembler.AsFlags(gotF)}";
                        System.Console.Error.WriteLine(output);
                    }
                }

                if (!bc)
                {
                    var expectedWord = expectedRegisters[(int)Register.BC];
                    var actualWord = this.cpu.BC;
                    DumpDifference("B", "C", expectedWord, actualWord);
                }

                if (!de)
                {
                    var expectedWord = expectedRegisters[(int)Register.DE];
                    var actualWord = this.cpu.DE;
                    DumpDifference("D", "E", expectedWord, actualWord);
                }

                if (!hl)
                {
                    var expectedWord = expectedRegisters[(int)Register.HL];
                    var actualWord = this.cpu.HL;
                    DumpDifference("H", "L", expectedWord, actualWord);
                }

                if (!ix)
                {
                    var expectedWord = expectedRegisters[(int)Register.IX];
                    var actualWord = this.cpu.IX;
                    DumpDifference("IXH", "IXL", expectedWord, actualWord);
                }

                if (!iy)
                {
                    var expectedWord = expectedRegisters[(int)Register.IY];
                    var actualWord = this.cpu.IY;
                    DumpDifference("IYH", "IYL", expectedWord, actualWord);
                }

                if (!sp)
                {
                    var expectedWord = expectedRegisters[(int)Register.SP];
                    var actualWord = this.cpu.SP;
                    DumpDifference("SPH", "SPL", expectedWord, actualWord);
                }

                if (!pc)
                {
                    var expectedWord = expectedRegisters[(int)Register.PC];
                    var actualWord = this.cpu.PC;
                    DumpDifference("PCH", "PCL", expectedWord, actualWord);
                }

                if (!memptr)
                {
                    var expectedWord = expectedRegisters[(int)Register.MEMPTR];
                    var actualWord = this.cpu.MEMPTR;
                    DumpDifference("MEMPTRH", "MEMPTRL", expectedWord, actualWord);
                }

                this.cpu.ExxAF();
                this.cpu.Exx();

                if (!af_)
                {
                    var expectedA = expectedRegisters[(int)Register.AF_].High;
                    var gotA = this.cpu.A;
                    if (expectedA != gotA)
                    {
                        DumpDifference("A'", expectedA, gotA);
                    }

                    var expectedF = expectedRegisters[(int)Register.AF_].Low;
                    var gotF = this.cpu.F;
                    if (expectedF != gotF)
                    {
                        var output = $"**** F', Expected: {EightBit.Disassembler.AsFlags(expectedF)}, Got: {EightBit.Disassembler.AsFlags(gotF)}";
                        System.Console.Error.WriteLine(output);
                    }
                }

                if (!bc_)
                {
                    var expectedWord = expectedRegisters[(int)Register.BC_];
                    var actualWord = this.cpu.BC;
                    DumpDifference("B'", "C'", expectedWord, actualWord);
                }

                if (!de_)
                {
                    var expectedWord = expectedRegisters[(int)Register.DE_];
                    var actualWord = this.cpu.DE;
                    DumpDifference("D'", "E'", expectedWord, actualWord);
                }

                if (!hl_)
                {
                    var expectedWord = expectedRegisters[(int)Register.HL_];
                    var actualWord = this.cpu.HL;
                    DumpDifference("H'", "L'", expectedWord, actualWord);
                }

                if (!iv)
                {
                    var output = $"**** IV, Expected: {expectedState.I:X2}, Got: {this.cpu.IV:X2}";
                    System.Console.Error.WriteLine(output);
                }

                if (!refresh)
                {
                    var output = $"**** R, Expected: {expectedState.R:X2}, Got: {this.cpu.REFRESH.ToByte():X2}";
                    System.Console.Error.WriteLine(output);
                }

                if (!iff1)
                {
                    var output = $"**** IFF1, Expected: {expectedState.IFF1}, Got: {this.cpu.IFF1}";
                    System.Console.Error.WriteLine(output);
                }

                if (!iff2)
                {
                    var output = $"**** IFF2, Expected: {expectedState.IFF2}, Got: {this.cpu.IFF2}";
                    System.Console.Error.WriteLine(output);
                }

                if (!im)
                {
                    var output = $"**** IM, Expected: {expectedState.IM}, Got: {this.cpu.IM}";
                    System.Console.Error.WriteLine(output);
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
    }
}
