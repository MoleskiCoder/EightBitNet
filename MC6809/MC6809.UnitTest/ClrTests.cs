// <copyright file="ClrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public ClrTests()
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
        public void TestImplied()
        {
            this.cpu.A = 0x43;
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}