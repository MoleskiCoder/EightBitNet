// <copyright file="AddTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AddTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AddTests() => this.cpu = this.board.CPU;

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
            this.board.Poke(0, 0x8b);
            this.board.Poke(1, 0x8b);
            this.cpu.A = 0x24;
            this.cpu.Step();
            Assert.AreEqual(0xaf, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Add 0x02 to A=0x04.
        [TestMethod]
        public void TestImmediateADDANoC()
        {
            this.board.Poke(0, 0x8b);
            this.board.Poke(1, 0x02);
            this.cpu.CC = 0;
            this.cpu.A = 4;
            this.cpu.B = 5;
            this.cpu.Step();
            Assert.AreEqual(6, this.cpu.A);
            Assert.AreEqual(5, this.cpu.B);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // The overflow (V) bit indicates signed two’s complement overflow, which occurs when the
        // sign bit differs from the carry bit after an arithmetic operation.
        // A=0x03 + 0xFF becomes 0x02
        [TestMethod]
        public void TestImmediateADDAWiC()
        {
            this.board.Poke(0, 0x8b);
            this.board.Poke(1, 0xff);
            this.cpu.CC = 0;
            this.cpu.A = 3;
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // positive + positive with overflow.
        // B=0x40 + 0x41 becomes 0x81 or -127
        [TestMethod]
        public void TestImmediateADDB1()
        {
            this.board.Poke(0, 0xcb);
            this.board.Poke(1, 0x41);
            this.cpu.B = 0x40;
            this.cpu.CC = 0;
            this.cpu.Step();
            Assert.AreEqual(0x81, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // negative + negative.
        // B=0xFF + 0xFF becomes 0xFE or -2
        [TestMethod]
        public void TestImmediateADDB2()
        {
            this.board.Poke(0, 0xcb);
            this.board.Poke(1, 0xff);
            this.cpu.B = 0xff;
            this.cpu.CC = 0;
            this.cpu.Step();
            Assert.AreEqual(0xfe, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // negative + negative with overflow.
        // B=0xC0 + 0xBF becomes 0x7F or 127
        [TestMethod]
        public void TestImmediateADDB3()
        {
            this.board.Poke(0, 0xcb);
            this.board.Poke(1, 0xbf);
            this.cpu.B = 0xc0;
            this.cpu.CC = 0;
            this.cpu.Step();
            Assert.AreEqual(0x7f, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // positive + negative with negative result.
        // B=0x02 + 0xFC becomes 0xFE or -2
        [TestMethod]
        public void TestImmediateADDB4()
        {
            this.board.Poke(0, 0xcb);
            this.board.Poke(1, 0xfc);
            this.cpu.B = 0x02;
            this.cpu.CC = 0;
            this.cpu.Step();
            Assert.AreEqual(0xfe, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Add 0x02B0 to D=0x0405 becomes 0x6B5.
        // positive + positive = positive
        [TestMethod]
        public void TestImmediateADDDNoC()
        {
            this.board.Poke(0, 0xc3);
            this.board.Poke(1, 0x02);
            this.board.Poke(2, 0xb0);
            this.cpu.CC = 0;
            this.cpu.A = 4;
            this.cpu.B = 5;
            this.cpu.Step();
            Assert.AreEqual(0x06, this.cpu.A);
            Assert.AreEqual(0xb5, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // Add 0xE2B0 to D=0x8405 becomes 0x66B5.
        // negative + negative = positive + overflow
        [TestMethod]
        public void TestImmediateADDD1()
        {
            this.board.Poke(0, 0xc3);
            this.board.Poke(1, 0xe2);
            this.board.Poke(2, 0xb0);
            this.cpu.CC = 0;
            this.cpu.A = 0x84;
            this.cpu.B = 5;
            this.cpu.Step();
            Assert.AreEqual(0x66, this.cpu.A);
            Assert.AreEqual(0xb5, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // negative + negative = negative.
        // Add 0xE000 to D=0xD000 becomes 0xB000
        [TestMethod]
        public void TestImmediateADDD2()
        {
            this.board.Poke(0, 0xc3);
            this.board.Poke(1, 0xe0);
            this.board.Poke(2, 0);
            this.cpu.CC = 0;
            this.cpu.A = 0xd0;
            this.cpu.B = 0;
            this.cpu.Step();
            Assert.AreEqual(0xb0, this.cpu.A);
            Assert.AreEqual(0x00, this.cpu.B);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }

        // positive + positive = negative + overflow.
        // Add 0x7000 to D=0x7000 becomes 0xE000
        [TestMethod]
        public void TestImmediateADDD3()
        {
            this.board.Poke(0, 0xc3);
            this.board.Poke(1, 0x70);
            this.board.Poke(2, 0);
            this.cpu.CC = 0;
            this.cpu.A = 0x70;
            this.cpu.B = 0;
            this.cpu.Step();
            Assert.AreEqual(0xe0, this.cpu.A);
            Assert.AreEqual(0x00, this.cpu.B);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }
    }
}