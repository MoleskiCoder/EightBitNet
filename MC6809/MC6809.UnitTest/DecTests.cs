// <copyright file="DecTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DecTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public DecTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestInherentDECA0x32()
        {
            this.board.Poke(0, 0x4a);
            this.cpu.CC = 0;
            this.cpu.A = 0x32;
            this.cpu.Step();
            Assert.AreEqual(0x31, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Test 0x80 - special case
        [TestMethod]
        public void TestInherentDECA0x80()
        {
            this.board.Poke(0, 0x4a);
            this.cpu.CC = 0;
            this.cpu.A = 0x80;
            this.cpu.Step();
            Assert.AreEqual(0x7f, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Test 0x00 - special case
        [TestMethod]
        public void TestInherentDECA0x00()
        {
            this.board.Poke(0, 0x4a);
            this.cpu.CC = 0;
            this.cpu.A = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}
