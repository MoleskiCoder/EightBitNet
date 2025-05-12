// <copyright file="BhiTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BhiTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public BhiTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x22);    // BHI
            this.board.Poke(1, 0x03);
            this.board.Poke(2, 0x86);    // LDA #1
            this.board.Poke(3, 0x01);
            this.board.Poke(4, 0x12);    // NOP
            this.board.Poke(5, 0x86);    // LDA #2
            this.board.Poke(6, 0x02);
            this.board.Poke(7, 0x12);    // NOP
        }

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step();
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestBHI1()
        {
            this.cpu.A = 0;
            this.cpu.CC = (byte)StatusBits.ZF;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }
    }
}
