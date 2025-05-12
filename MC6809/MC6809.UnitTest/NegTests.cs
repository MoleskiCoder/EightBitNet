// <copyright file="NegTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NegTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public NegTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestNEG_zero()
        {
            this.board.Poke(0xb00, 0x40);
            this.cpu.A = 0;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Carry);
        }

        [TestMethod]
        public void TestNEG_one()
        {
            this.board.Poke(0xb00, 0x40);
            this.cpu.A = 1;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Carry);
        }

        [TestMethod]
        public void TestNEG_two()
        {
            this.board.Poke(0xb00, 0x40);
            this.cpu.A = 2;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xfe, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
        }

        [TestMethod]
        public void TestNEG_eighty()
        {
            this.board.Poke(0xb00, 0x40);
            this.cpu.A = 0x80;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x80, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Negative);
        }
    }
}