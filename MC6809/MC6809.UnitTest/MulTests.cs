// <copyright file="MulTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MulTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public MulTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // Multiply 0x0C with 0x64. Result is 0x04B0.
        // The Zero flag is set if the 16-bit result is zero; cleared otherwise.
        // The Carry flag is set equal to the new value of bit 7 in Accumulator B.
        [TestMethod]
        public void TestMUL_one()
        {
            this.board.Poke(0xb00, 0x3d);
            this.cpu.CC = 0;
            this.cpu.CC |= (byte)StatusBits.ZF;
            this.cpu.A = 0xc;
            this.cpu.B = 0x64;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x4b0, this.cpu.D.Word);
            Assert.AreEqual(0x4, this.cpu.A);
            Assert.AreEqual(0xb0, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Carry);
        }

        // Multiply 0x0C with 0x00. Result is 0x0000.
        // The Zero flag is set if the 16-bit result is zero; cleared otherwise.
        // The Carry flag is set equal to the new value of bit 7 in Accumulator B.
        [TestMethod]
        public void TestMUL_two()
        {
            this.board.Poke(0xb00, 0x3d);
            this.cpu.CC = 0;
            this.cpu.CC &= (byte)~StatusBits.CF;
            this.cpu.CC |= (byte)StatusBits.ZF;
            this.cpu.A = 0xc;
            this.cpu.B = 0x00;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.D.Word);
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Carry);
        }
    }
}


