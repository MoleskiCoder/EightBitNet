﻿// <copyright file="TestRunner.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.FuseTest
{
    using Fuse;

    public enum Register
    {
        AF,
        BC,
        DE,
        HL,
        SP,
        PC,
    }

    public class TestRunner<T>(Test<T> test, Result<T> result) : Bus
        where T : IRegisterState, new()
    {
        private readonly Test<T> test = test;
        private readonly Result<T> result = result;
        private readonly EightBit.Ram ram = new(0x10000);

        public bool Failed { get; private set; } = false;

        public bool Unimplemented { get; private set; } = false;

        public override EightBit.MemoryMapping Mapping(ushort address) => new(this.ram, 0, EightBit.Mask.Sixteen, EightBit.AccessLevel.ReadWrite);

        public void Run()
        {
            this.RaisePOWER();
            this.Initialize();
            var allowedCycles = this.test.RegisterState.TStates;
            try
            {
                _ = this.CPU.Run(allowedCycles);
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

        private static void DumpDifference(string description, byte expected, byte actual)
        {
            var output = $"**** {description}, Expected: {expected:x2}, Got {actual:x2}";
            Console.Error.WriteLine(output);
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

            this.CPU.AF.Word = inputRegisters[(int)Register.AF].Word;
            this.CPU.BC.Word = inputRegisters[(int)Register.BC].Word;
            this.CPU.DE.Word = inputRegisters[(int)Register.DE].Word;
            this.CPU.HL.Word = inputRegisters[(int)Register.HL].Word;

            this.CPU.SP.Word = inputRegisters[(int)Register.SP].Word;
            this.CPU.PC.Word = inputRegisters[(int)Register.PC].Word;
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
        }

        private void CheckRegisters()
        {
            var expectedState = this.result.RegisterState;
            var expectedRegisters = expectedState.Registers;

            var af = this.CPU.AF.Word == expectedRegisters[(int)Register.AF].Word;
            var bc = this.CPU.BC.Word == expectedRegisters[(int)Register.BC].Word;
            var de = this.CPU.DE.Word == expectedRegisters[(int)Register.DE].Word;
            var hl = this.CPU.HL.Word == expectedRegisters[(int)Register.HL].Word;

            var sp = this.CPU.SP.Word == expectedRegisters[(int)Register.SP].Word;
            var pc = this.CPU.PC.Word == expectedRegisters[(int)Register.PC].Word;

            var success = af && bc && de && hl && sp && pc;
            if (!success)
            {
                this.Failed = true;
                Console.Error.WriteLine($"**** Failed test (Register): {this.test.Description}");

                if (!af)
                {
                    var expectedWord = expectedRegisters[(int)Register.AF];
                    var actualWord = this.CPU.AF;
                    DumpDifference("A", "F", expectedWord, actualWord);
                }

                if (!bc)
                {
                    var expectedWord = expectedRegisters[(int)Register.BC];
                    var actualWord = this.CPU.BC;
                    DumpDifference("B", "C", expectedWord, actualWord);
                }

                if (!de)
                {
                    var expectedWord = expectedRegisters[(int)Register.DE];
                    var actualWord = this.CPU.DE;
                    DumpDifference("D", "E", expectedWord, actualWord);
                }

                if (!hl)
                {
                    var expectedWord = expectedRegisters[(int)Register.HL];
                    var actualWord = this.CPU.HL;
                    DumpDifference("H", "L", expectedWord, actualWord);
                }

                if (!sp)
                {
                    var expectedWord = expectedRegisters[(int)Register.SP];
                    var actualWord = this.CPU.SP;
                    DumpDifference("SPH", "SPL", expectedWord, actualWord);
                }

                if (!pc)
                {
                    var expectedWord = expectedRegisters[(int)Register.PC];
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
