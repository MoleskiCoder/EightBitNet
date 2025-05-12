// <copyright file="SbcTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SbcTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public SbcTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestImmediateByte()
        {
            this.board.Poke(0, 0x82);
            this.board.Poke(1, 0x34);
            this.cpu.CC = (byte)StatusBits.CF;
            this.cpu.A = 0x14;
            this.cpu.Step();
            Assert.AreEqual(0xdf, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // Test the subtraction with carry instruction.
        // B=0x35 - addr(0x503)=0x3 - C=1 becomes 0x31
        // SBCB dp+03
        [TestMethod]
        public void TestDirectSBCB()
        {
            this.board.Poke(0, 0xd2);
            this.board.Poke(1, 0x03);
            this.board.Poke(0x503, 0x03);
            this.cpu.CC = (byte)StatusBits.CF;
            this.cpu.DP = 5;
            this.cpu.B = 0x35;
            this.cpu.Step();
            Assert.AreEqual(0x31, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // Test the SBCA instruction.
        // A=0xFF - 0xFE - C=1 becomes 0x00
        [TestMethod]
        public void TestImmediateSBCASBCA1()
        {
            this.board.Poke(0, 0x82);
            this.board.Poke(1, 0xfe);
            this.cpu.CC = (byte)(StatusBits.CF | StatusBits.NF);
            this.cpu.A = 0xff;
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // Test the SBCA instruction.
        // A=0x00 - 0xFF - C=0 becomes 0x01
        [TestMethod]
        public void TestImmediateSBCASBCA2()
        {
            this.board.Poke(0, 0x82);
            this.board.Poke(1, 0xff);
            this.cpu.CC = (byte)(StatusBits.NF | StatusBits.VF);
            this.cpu.A = 0;
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // Test the SBCA instruction.
        // A=0x00 - 0x01 - C=0 becomes 0xFF
        [TestMethod]
        public void TestImmediateSBCASBCA3()
        {
            this.board.Poke(0, 0x82);
            this.board.Poke(1, 1);
            this.cpu.CC = (byte)(StatusBits.NF | StatusBits.VF);
            this.cpu.A = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(4, this.cpu.Cycles);
        }
    }
}