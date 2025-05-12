// <copyright file="RolTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RolTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public RolTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestROLB_inherent_one()
        {
            this.board.Poke(0xB00, 0x59);
            this.cpu.B = 0x89;
            this.cpu.CC = 0;
            this.cpu.CC |= (byte)StatusBits.NF;
            this.cpu.CC |= (byte)StatusBits.CF;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xb01, this.cpu.PC.Word);
            Assert.AreEqual(0x13, this.cpu.B);
            Assert.AreEqual(0x3, this.cpu.CC);
            Assert.AreNotEqual(0, this.cpu.Carry);
        }

        [TestMethod]
        public void TestROLB_inherent_two()
        {
            this.board.Poke(0xB00, 0x59);
            this.cpu.CC = 0;
            this.cpu.CC |= (byte)StatusBits.CF;
            this.cpu.CC |= (byte)StatusBits.VF;
            this.cpu.B = 1;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x3, this.cpu.B);
            Assert.AreEqual(0, this.cpu.CC);
        }

        [TestMethod]
        public void TestROLB_inherent_three()
        {
            this.board.Poke(0xB00, 0x59);
            this.cpu.CC = 0;
            this.cpu.B = 0xd8;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xb0, this.cpu.B);
            Assert.AreEqual(9, this.cpu.CC);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Negative);
        }
    }
}
