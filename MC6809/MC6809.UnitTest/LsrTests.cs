// <copyright file="LsrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LsrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public LsrTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // Logical Shift Right of 0x3E to 0x1F
        [TestMethod]
        public void TestLSR_a_one()
        {
            this.board.Poke(0xb00, 0x44);
            this.cpu.CC = 0xf;
            this.cpu.A = 0x3e;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x1F, this.cpu.A);
            Assert.AreEqual(2, this.cpu.CC);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Negative);
        }

        // Logical Shift Right of 1
        [TestMethod]
        public void TestLSR_a_two()
        {
            this.board.Poke(0xb00, 0x44);
            this.cpu.CC &= (byte)~StatusBits.CF;
            this.cpu.CC |= (byte)StatusBits.VF;
            this.cpu.CC |= (byte)StatusBits.NF;
            this.cpu.A = 1;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Negative);
        }

        // Logical Shift Right of 0xB8
        [TestMethod]
        public void TestLSR_a_three()
        {
            this.board.Poke(0xb00, 0x44);
            this.cpu.CC &= (byte)~StatusBits.CF;
            this.cpu.CC &= (byte)~StatusBits.VF;
            this.cpu.A = 0xb8;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x5c, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Carry);
        }

        // Shift a byte at 0x0402, because DP = 0x04.
        [TestMethod]
        public void TestLSR_direct()
        {
            this.board.Poke(0xb00, 0x4);
            this.board.Poke(0xb01, 0x2);
            this.cpu.PC.Word = 0xb00;
            this.cpu.CC = 0;
            this.cpu.A = 0;
            this.cpu.B = 0;
            this.cpu.DP = 4;
            this.cpu.X.Word = 0;
            this.cpu.Y.Word = 0;
            this.cpu.S.Word = 0;
            this.cpu.U.Word = 0;
            this.board.Poke(0x0402, 0xf1);

            this.cpu.Step();

            Assert.AreEqual(0xb02, this.cpu.PC.Word);
            Assert.AreEqual(1, this.cpu.CC);
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreEqual(4, this.cpu.DP);
            Assert.AreEqual(0, this.cpu.X.Word);
            Assert.AreEqual(0, this.cpu.Y.Word);
            Assert.AreEqual(0, this.cpu.S.Word);
            Assert.AreEqual(0, this.cpu.U.Word);
            Assert.AreEqual(0x78, this.board.Peek(0x402));
        }
    }
}
