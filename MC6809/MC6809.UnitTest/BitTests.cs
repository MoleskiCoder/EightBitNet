// <copyright file="BitTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BitTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public BitTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x85);
            this.board.Poke(1, 0xe0);
        }

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
            this.cpu.A = 0xa6;
            this.cpu.CC = (byte)StatusBits.ZF;
            this.cpu.Step();
            Assert.AreEqual(0xa6, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}