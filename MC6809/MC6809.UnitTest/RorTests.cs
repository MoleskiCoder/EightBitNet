// <copyright file="RorTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using EightBit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RorTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public RorTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestRORB_inherent_one()
        {
            this.board.Poke(0xb00, 0x56);
            this.cpu.B = 0x89;
            this.cpu.CC = 0;
            this.cpu.CC |= (byte)StatusBits.CF;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xc4, this.cpu.B);
            Assert.AreEqual(9, this.cpu.CC);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.Negative);
        }

        [TestMethod]
        public void TestRORB_inherent_two()
        {
            this.board.Poke(0xb00, 0x56);
            this.cpu.B = 0x89;
            this.cpu.CC = 0;
            this.cpu.CC = Chip.ClearBit(this.cpu.CC, (byte)StatusBits.CF);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x44, this.cpu.B);
            Assert.AreEqual(1, this.cpu.CC);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Negative);
        }
    }
}
