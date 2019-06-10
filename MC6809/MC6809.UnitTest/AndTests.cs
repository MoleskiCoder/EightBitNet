// <copyright file="AndTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AndTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AndTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestImmediate()
        {
            this.board.Poke(0, 0x84);
            this.board.Poke(1, 0x13);
            this.cpu.A = 0xfc;

            this.cpu.Step();

            Assert.AreEqual(0x10, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestANDCC()
        {
            this.board.Poke(0xb00, 0x1c);
            this.board.Poke(0xb01, 0xaf);
            this.cpu.CC = 0x79;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x29, this.cpu.CC);
        }
    }
}