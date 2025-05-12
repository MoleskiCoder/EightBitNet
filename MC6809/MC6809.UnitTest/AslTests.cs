// <copyright file="AslTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AslTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AslTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x48);
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
            this.cpu.A = 0x7a;
            this.cpu.Step();
            Assert.AreEqual(0xf4, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}
