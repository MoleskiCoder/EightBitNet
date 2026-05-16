// <copyright file="CmpTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CmpTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public CmpTests() => this.cpu = this.board.CPU;

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
            this.board.Poke(0, 0x81);
            this.board.Poke(1, 0x18);
            this.cpu.A = 0xf6;
            this.cpu.Step();
            Assert.AreEqual(0xf6, this.cpu.A);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsTrue(this.cpu.Negative);
            Assert.IsFalse(this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Indirect mode: CMPA ,Y+ (CMP1)
        [TestMethod]
        public void TestCMP1_indexed_y_plus()
        {
            this.board.Poke(0, 0xa1);
            this.board.Poke(1, 0xa0);
            this.board.Poke(0x205, 0xff);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.B = 0;
            this.cpu.X.Joined = 0;
            this.cpu.Y.Joined = 0x205;
            this.cpu.U.Joined = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreEqual(0, this.cpu.X.Joined);
            Assert.AreEqual(0x206, this.cpu.Y.Joined);
            Assert.AreEqual(0, this.cpu.U.Joined);
            Assert.IsTrue(this.cpu.Zero);
            Assert.AreEqual(6, this.cpu.Cycles);
        }

        // Indirect mode: CMPA ,Y++ (CMP1)
        [TestMethod]
        public void TestCMP1_indexed_y_plusplus()
        {
            this.board.Poke(0, 0xa1);
            this.board.Poke(1, 0xa1);
            this.board.Poke(0x205, 0xff);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.B = 0;
            this.cpu.X.Joined = 0;
            this.cpu.Y.Joined = 0x205;
            this.cpu.U.Joined = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreEqual(0, this.cpu.X.Joined);
            Assert.AreEqual(0x207, this.cpu.Y.Joined);
            Assert.AreEqual(0, this.cpu.U.Joined);
            Assert.IsTrue(this.cpu.Zero);
            Assert.AreEqual(7, this.cpu.Cycles);
        }

        // Indirect mode: CMPA ,-Y (CMP1)
        [TestMethod]
        public void TestCMP1_indexed_minus_y()
        {
            this.board.Poke(0, 0xa1);
            this.board.Poke(1, 0xa2);
            this.board.Poke(0x204, 0xff);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.B = 0;
            this.cpu.X.Joined = 0;
            this.cpu.Y.Joined = 0x205;
            this.cpu.U.Joined = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreEqual(0, this.cpu.X.Joined);
            Assert.AreEqual(0x204, this.cpu.Y.Joined);
            Assert.AreEqual(0, this.cpu.U.Joined);
            Assert.IsTrue(this.cpu.Zero);
            Assert.AreEqual(6, this.cpu.Cycles);
        }

        // Indirect mode: CMPA ,Y++ (CMP1)
        [TestMethod]
        public void TestCMP1_indexed_minusminus_y()
        {
            this.board.Poke(0, 0xa1);
            this.board.Poke(1, 0xa3);
            this.board.Poke(0x203, 0xff);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.B = 0;
            this.cpu.X.Joined = 0;
            this.cpu.Y.Joined = 0x205;
            this.cpu.U.Joined = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreEqual(0, this.cpu.B);
            Assert.AreEqual(0, this.cpu.X.Joined);
            Assert.AreEqual(0x203, this.cpu.Y.Joined);
            Assert.AreEqual(0, this.cpu.U.Joined);
            Assert.IsTrue(this.cpu.Zero);
            Assert.AreEqual(7, this.cpu.Cycles);
        }

        // B = 0xA0, CMPB with 0xA0
        [TestMethod]
        public void TestCMP2()
        {
            this.board.Poke(0, 0xc1);
            this.board.Poke(1, 0xa0);
            this.cpu.CC = (byte)StatusBits.NF;
            this.cpu.B = 0xa0;
            this.cpu.Step();
            Assert.IsFalse(this.cpu.Negative);
            Assert.IsTrue(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsFalse(this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // B = 0x70, CMPB with 0xA0
        [TestMethod]
        public void TestCMP3()
        {
            this.board.Poke(0, 0xc1);
            this.board.Poke(1, 0xa0);
            this.cpu.CC = (byte)StatusBits.NF;
            this.cpu.B = 0x70;
            this.cpu.Step();
            Assert.IsTrue(this.cpu.Carry);
            Assert.IsTrue(this.cpu.Overflow);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsTrue(this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Compare 0x5410 with 0x5410
        [TestMethod]
        public void TestCMP16()
        {
            this.cpu.CC = (byte)(StatusBits.HF | StatusBits.VF | StatusBits.CF);
            this.cpu.X.Joined = 0x5410;
            this.cpu.PokeShort(0x33, 0x5410);
            this.board.Poke(0, 0xbc);
            this.cpu.PokeShort(1, 0x33);
            this.cpu.Step();
            Assert.AreEqual(0x5410, this.cpu.X.Joined);
            Assert.IsFalse(this.cpu.Carry);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsTrue(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Negative);
            Assert.AreEqual(7, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestImmediateWord()
        {
            this.board.Poke(0, 0x8c);
            this.cpu.PokeShort(1, 0x1bb0);
            this.cpu.X.Joined = 0x1ab0;
            this.cpu.Step();
            Assert.AreEqual(0x1ab0, this.cpu.X.Joined);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsTrue(this.cpu.Negative);
            Assert.IsTrue(this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }
    }
}
