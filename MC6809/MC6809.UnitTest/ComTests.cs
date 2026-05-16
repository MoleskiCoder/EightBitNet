// <copyright file="ComTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ComTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public ComTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestCOM_direct()
        {
            this.board.Poke(0xb00, 0x03);
            this.board.Poke(0xb01, 0x00);
            this.board.Poke(0x0200, 0x07);
            this.cpu.CC = 0;
            this.cpu.DP = 2;
            this.cpu.PC.Joined = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xf8, this.board.Peek(0x0200));
            Assert.IsTrue(this.cpu.Negative);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsTrue(this.cpu.Carry);
        }

        [TestMethod]
        public void TestCOM_a()
        {
            this.cpu.CC = 0;
            this.cpu.A = 0x74;
            this.board.Poke(0xb00, 0x43);
            this.cpu.PC.Joined = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x8B, this.cpu.A);
            Assert.AreEqual(0x09, this.cpu.CC);
        }
    }
}
