// <copyright file="AsrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AsrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AsrTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x47);
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
        public void TestInherent()
        {
            this.cpu.A = 0xcb;
            this.cpu.Step();
            Assert.AreEqual(0xe5, this.cpu.A);
            Assert.IsTrue(this.cpu.Carry);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsTrue(this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // ASR of positive value 0x40: sign bit (0) must be preserved, not forced to 1
        [TestMethod]
        public void TestInherentPositive()
        {
            this.cpu.A = 0x40;
            this.cpu.Step();
            Assert.AreEqual(0x20, this.cpu.A);
            Assert.IsFalse(this.cpu.Carry);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // ASR of 0x01: carry set, result zero
        [TestMethod]
        public void TestInherentOne()
        {
            this.cpu.A = 0x01;
            this.cpu.Step();
            Assert.AreEqual(0x00, this.cpu.A);
            Assert.IsTrue(this.cpu.Carry);
            Assert.IsTrue(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}
