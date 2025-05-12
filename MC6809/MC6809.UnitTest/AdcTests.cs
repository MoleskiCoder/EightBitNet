// <copyright file="AdcTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AdcTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AdcTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestImmediate()
        {
            this.board.Poke(0, 0x89);
            this.board.Poke(1, 0x7c);
            this.cpu.CC = EightBit.Chip.SetBit(this.cpu.CC, (byte)StatusBits.CF);
            this.cpu.A = 0x3a;
            this.cpu.Step();
            Assert.AreEqual(0xb7, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.HalfCarry);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestImmediateADCANoC1()
        {
            this.cpu.A = 0x5;
            this.cpu.CC = 0;
            this.board.Poke(0, 0x89);
            this.board.Poke(1, 0x02);
            this.cpu.Step();
            Assert.AreEqual(7, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        /* Test half-carry $E + $2 = $10 */
        [TestMethod]
        public void TestImmediateADCANoC2()
        {
            this.cpu.A = 0xe;
            this.cpu.CC = 0;
            this.board.Poke(0, 0x89);
            this.board.Poke(1, 0x02);
            this.cpu.Step();
            Assert.AreEqual(0x10, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        /* Add $22 and carry to register A ($14) */
        [TestMethod]
        public void TestImmediateADCAWiC()
        {
            this.cpu.A = 0x14;
            this.cpu.CC = (byte)(StatusBits.CF | StatusBits.HF);
            this.board.Poke(0, 0x89);
            this.board.Poke(1, 0x22);
            this.cpu.Step();
            Assert.AreEqual(0x37, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        /* Test that half-carry is set when adding with a carry */
        [TestMethod]
        public void TestImmediateADCAWiHC()
        {
            this.cpu.A = 0x14;
            this.cpu.CC = (byte)StatusBits.CF;
            this.board.Poke(0, 0x89);
            this.board.Poke(1, 0x2B);
            this.cpu.Step();
            Assert.AreEqual(0x40, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}