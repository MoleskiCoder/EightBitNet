// <copyright file="SexTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SexTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public SexTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestSEX_Inherent_one()
        {
            this.board.Poke(0, 0x1d);
            this.cpu.A = 0x02;
            this.cpu.B = 0xe6;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0xe6, this.cpu.B);
        }

        [TestMethod]
        public void TestSEX_Inherent_two()
        {
            this.board.Poke(0, 0x1d);
            this.cpu.A = 0x02;
            this.cpu.B = 0x76;
            this.cpu.Step();
            Assert.AreEqual(0x00, this.cpu.A);
            Assert.AreEqual(0x76, this.cpu.B);
        }
    }
}