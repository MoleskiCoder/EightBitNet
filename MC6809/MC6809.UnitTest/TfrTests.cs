// <copyright file="TfrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TfrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public TfrTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestTFR_d_y()
        {
            this.board.Poke(0xb00, 0x1f);
            this.board.Poke(0xb01, 0x02);
            this.cpu.CC = 0;
            this.cpu.D.Word = 0xabba;
            this.cpu.Y.Word = 0x101;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xb02, this.cpu.PC.Word);
            Assert.AreEqual(0xabba, this.cpu.D.Word);
            Assert.AreEqual(0xabba, this.cpu.Y.Word);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
        }

        [TestMethod]
        public void TestTFR_s_pc()
        {
            this.board.Poke(0xb00, 0x1f);
            this.board.Poke(0xb01, 0x45);
            this.cpu.CC = 0;
            this.cpu.S.Word = 0x1bb1;
            this.cpu.Y.Word = 0x101;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x1bb1, this.cpu.PC.Word);
            Assert.AreEqual(0x1bb1, this.cpu.S.Word);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
        }

        [TestMethod]
        public void TestTFR_dp_cc()
        {
            this.board.Poke(0xb00, 0x1f);
            this.board.Poke(0xb01, 0xba);
            this.cpu.CC = 0;
            this.cpu.DP = 0x1b;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x1b, this.cpu.DP);
            Assert.AreEqual(0x1b, this.cpu.CC);

            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreNotEqual(0, this.cpu.InterruptMasked);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
        }

        [TestMethod]
        public void TestTFR_a_x()
        {
            this.board.Poke(0xb00, 0x1f);
            this.board.Poke(0xb01, 0x81);
            this.cpu.A = 0x56;
            this.cpu.B = 0x78;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x56, this.cpu.A);
            Assert.AreEqual(0xff56, this.cpu.X.Word);
        }

        [TestMethod]
        public void TestTFR_x_b()
        {
            this.board.Poke(0xb00, 0x1f);
            this.board.Poke(0xb01, 0x19);
            this.cpu.CC = 0;
            this.cpu.X.Word = 0x6541;
            this.cpu.B = 0x78;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x41, this.cpu.B);
            Assert.AreEqual(0x6541, this.cpu.X.Word);
        }
    }
}