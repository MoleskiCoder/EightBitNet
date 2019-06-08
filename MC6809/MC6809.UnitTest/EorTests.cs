// <copyright file="EorTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EorTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public EorTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x4f);
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
        public void TestEOR_implied()
        {
            this.cpu.Y.Word = 0x12f0;
            this.cpu.A = 0xf2;
            this.cpu.CC = 0x03;
            this.board.Poke(0x12f8, 0x98);
            this.board.Poke(0xb00, 0xa8);
            this.board.Poke(0xb01, 0x28);
            this.cpu.PC.Word = 0xb00;
            this.cpu.Step();
            Assert.AreEqual(0x98, this.board.Peek(0x12f8));
            Assert.AreEqual(0x6a, this.cpu.A);
            Assert.AreEqual(0x01, this.cpu.CC);
            Assert.AreEqual(0x12f0, this.cpu.Y.Word);
        }
    }
}