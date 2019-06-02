// <copyright file="OrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public OrTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestORA_immediate()
        {
            this.cpu.CC = 0x43;
            this.cpu.A = 0xda;
            this.board.Poke(0xb00, 0x8a);
            this.board.Poke(0xb01, 0x0f);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xdf, this.cpu.A);
            Assert.AreEqual(0x49, this.cpu.CC);
        }
    }
}
