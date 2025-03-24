// <copyright file="TestRunner.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.FuseTest
{
    using Fuse;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal enum Register
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

    internal class TestRunner : EightBit.Bus
    {
        private readonly Test<RegisterState> test;
        private readonly Result<RegisterState> result;
        private readonly TestEvents expectedEvents = new();
        private readonly TestEvents actualEvents = new();
        private readonly EightBit.Ram ram = new(0x10000);
        private readonly EightBit.InputOutput ports = new();
        private readonly Z80 cpu;
        private readonly Disassembler disassembler;

        private int totalCycles;

        public TestRunner(Test<RegisterState> test, Result<RegisterState> result)
        {
            this.cpu = new Z80(this, this.ports);
            this.disassembler = new Disassembler(this);
            this.test = test ?? throw new ArgumentNullException(nameof(test));
            this.result = result ?? throw new ArgumentNullException(nameof(result));

            foreach (var e in result.Events.Container)
            {
                // Ignore contention events
                Debug.Assert(e.Specifier is not null);
                if (!e.Specifier.EndsWith('C'))
                {
                    this.expectedEvents.Add(e);
                }
            }
        }

        public bool Failed { get; private set; }

        public bool Unimplemented { get; private set; }

        public override EightBit.MemoryMapping Mapping(ushort address) => new(this.ram, 0, EightBit.Mask.Sixteen, EightBit.AccessLevel.ReadWrite);

        public void Run()
        {
            this.RaisePOWER();
            this.Initialize();
            var allowedCycles = this.test.RegisterState.TStates;
            try
            {
                _ = this.cpu.Run(allowedCycles);
                this.Check();
            }
            catch (InvalidOperationException error)
            {
                this.Unimplemented = true;
                Console.Error.WriteLine($"**** Error: {error.Message}");
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
            this.ReadByte += this.Event_ReadByte;
            this.WrittenByte += this.Event_WrittenByte;
            this.ports.ReadPort += this.Ports_ReadPort;
            this.ports.WrittenPort += this.Ports_WrittenPort;
            this.cpu.ExecutedInstruction += this.Cpu_ExecutedInstruction;
        }

        private void Ports_ReadPort(object? sender, EventArgs e) => this.AddActualEvent("PR");
        private void Ports_WrittenPort(object? sender, EventArgs e) => this.AddActualEvent("PW");
        private void Event_ReadByte(object? sender, EventArgs e) => this.AddActualEvent("MR");
        private void Event_WrittenByte(object? sender, EventArgs e) => this.AddActualEvent("MW");

        private void AddActualEvent(string specifier)
        {
            var address = this.Address.Word;
            var cycles = this.totalCycles + this.cpu.Cycles;
            if (specifier.EndsWith('C'))
            {
                this.actualEvents.Add(new TestEvent(cycles, specifier, address));
            }
            else
            {
                this.actualEvents.Add(new TestEvent(cycles, specifier, address, this.Data));
            }
        }

        private static void DumpDifference(string description, byte expected, byte actual)
        {
            var output = $"**** {description}, Expected: {expected:x2}, Got {actual:x2}";
            Console.Error.WriteLine(output);
        }

        private void Cpu_ExecutedInstruction(object? sender, EventArgs e)
        {
            var output = $"**** Cycle count: {this.cpu.Cycles}";
            Console.Out.WriteLine(output);
            this.totalCycles += this.cpu.Cycles;
        }

        private static void DumpDifference(string highDescription, string lowDescription, EightBit.Register16 expected, EightBit.Register16 actual)
        {
            var expectedHigh = expected.High;
            var expectedLow = expected.Low;

            var actualHigh = actual.High;
            var actualLow = actual.Low;

            if (expectedHigh != actualHigh)
            {
                DumpDifference(highDescription, expectedHigh, actualHigh);
            }

            if (expectedLow != actualLow)
            {
                DumpDifference(lowDescription, expectedLow, actualLow);
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
            this.CheckRegisters();
            this.CheckMemory();
            this.CheckEvents();
        }

        private void CheckRegisters()
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
                Console.Error.WriteLine($"**** Failed test (Register): {this.test.Description}");

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
                        var output = $"**** F, Expected: {Disassembler.AsFlags(expectedF)}, Got: {Disassembler.AsFlags(gotF)}";
                        Console.Error.WriteLine(output);
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
                        var output = $"**** F', Expected: {Disassembler.AsFlags(expectedF)}, Got: {Disassembler.AsFlags(gotF)}";
                        Console.Error.WriteLine(output);
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
                    Console.Error.WriteLine(output);
                }

                if (!refresh)
                {
                    var output = $"**** R, Expected: {expectedState.R:X2}, Got: {this.cpu.REFRESH.ToByte():X2}";
                    Console.Error.WriteLine(output);
                }

                if (!iff1)
                {
                    var output = $"**** IFF1, Expected: {expectedState.IFF1}, Got: {this.cpu.IFF1}";
                    Console.Error.WriteLine(output);
                }

                if (!iff2)
                {
                    var output = $"**** IFF2, Expected: {expectedState.IFF2}, Got: {this.cpu.IFF2}";
                    Console.Error.WriteLine(output);
                }

                if (!im)
                {
                    var output = $"**** IM, Expected: {expectedState.IM}, Got: {this.cpu.IM}";
                    Console.Error.WriteLine(output);
                }

                this.cpu.PC.Word = this.test.RegisterState.Registers[(int)Register.PC].Word;
                var disassembled = this.disassembler.Disassemble(this.cpu);
                Console.Error.WriteLine(disassembled);
            }
        }

        private void CheckEvents()
        {
            var expectations = this.expectedEvents.Container;
            var actuals = this.actualEvents.Container;

            var eventFailure = expectations.Count != actuals.Count;
            for (var i = 0; !eventFailure && i < expectations.Count; ++i)
            {
                var expectation = expectations[i];
                var actual = actuals[i];

                var equalCycles = expectation.Cycles == actual.Cycles;
                var equalSpecifier = expectation.Specifier == actual.Specifier;
                var equalAddress = expectation.Address == actual.Address;
                var equalValue = expectation.Value == actual.Value;

                var equal = equalCycles && equalSpecifier && equalAddress && equalValue;
                eventFailure = !equal;
            }

            if (eventFailure)
            {
                this.DumpExpectedEvents();
                this.DumpActualEvents();
            }

            if (!this.Failed)
            {
                this.Failed = eventFailure;
            }
        }

        private void DumpExpectedEvents()
        {
            Console.Error.WriteLine("++++ Dumping expected events:");
            DumpEvents(this.expectedEvents.Container);
        }

        private void DumpActualEvents()
        {
            Console.Error.WriteLine("++++ Dumping actual events:");
            DumpEvents(this.actualEvents.Container);
        }

        private static void DumpEvents(IEnumerable<TestEvent> events)
        {
            foreach (var e in events)
            {
                DumpEvent(e);
            }
        }

        private static void DumpEvent(TestEvent e)
        {
            var output = $" Event issue {e}";
            Console.Error.WriteLine(output);
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
                            Console.Error.WriteLine($"**** Failed test (Memory): {this.test.Description}");
                        }

                        Console.Error.WriteLine($"**** Difference: Address: {address:x4} Expected: {expected:x2} Actual: {actual:x2}");
                    }

                    ++address;
                }
            }
        }
    }
}
