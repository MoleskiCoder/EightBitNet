// <copyright file="TstTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TstTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public TstTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestTSTA_inherent_one()
        {
            this.board.Poke(0xb00, 0x4D);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
        }

        [TestMethod]
        public void TestTSTA_inherent_two()
        {
            this.board.Poke(0xb00, 0x4D);
            this.cpu.CC = 0;
            this.cpu.CC |= (byte)StatusBits.VF;
            this.cpu.A = 0x01;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x01, this.cpu.A);
            Assert.AreEqual(0, this.cpu.CC);
        }

        [TestMethod]
        public void TestTSTA_inherent_three()
        {
            this.board.Poke(0xb00, 0x4D);
            this.cpu.CC = 0;
            this.cpu.A = 0;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
        }

        [TestMethod]
        public void TestTSTA_indirect()
        {
            this.board.Poke(0x205, 0xff);
            this.cpu.Y.Word = 0x205;
            this.board.Poke(0xb00, 0x6d);
            this.board.Poke(0xb01, 0xa4);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xff, this.board.Peek(0x205));
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
        }
    }
}
