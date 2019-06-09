// <copyright file="LeaTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LeaTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public LeaTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestLEA_one()
        {
            ushort location = 0x1e20;

            //// LEAX        n,PCR (16-bit)
            this.board.Poke(location, 0x30);
            this.board.Poke((ushort)(location + 1), 0x8d);
            this.cpu.PokeWord((ushort)(location + 2), 0xfe49);
            this.cpu.PC.Word = location;

            this.cpu.Step();

            var offset = 0x10000 - 0xfe49;
            Assert.AreEqual(location + 4 - offset, this.cpu.X.Word);
            Assert.AreEqual(location + 4, this.cpu.PC.Word);
        }

        [TestMethod]
        public void TestLEA_two()
        {
            ushort location = 0x846;

            //// LEAX        n,PCR (8-bit)
            this.board.Poke(location, 0x30);
            this.board.Poke((ushort)(location + 1), 0x8c);
            this.board.Poke((ushort)(location + 2), 0xf1);
            this.cpu.PC.Word = location;

            this.cpu.Step();

            var offset = 0x100 - 0xf1;
            Assert.AreEqual(location + 3 - offset, this.cpu.X.Word);
            Assert.AreEqual(location + 3, this.cpu.PC.Word);
        }

        [TestMethod]
        public void TestLEA_three()
        {
            this.board.Poke(0, 0x30);
            this.board.Poke(1, 0xab);
            this.cpu.CC = 0;
            this.cpu.X.Word = 0xabcd;
            this.cpu.Y.Word = 0x804f;
            this.cpu.A = 0x80;
            this.cpu.B = 0x01;

            this.cpu.Step();

            Assert.AreEqual(0x50, this.cpu.X.Word);
            Assert.AreEqual(0, this.cpu.Zero);
        }

        [TestMethod]
        public void TestLEA_four()
        {
            this.board.Poke(0, 0x30);
            this.board.Poke(1, 0xab);
            this.cpu.CC = 0x28;
            this.cpu.X.Word = 0xefa;
            this.cpu.Y.Word = 0xef8;
            this.cpu.A = 0xff;
            this.cpu.B = 0x82;

            this.cpu.Step();

            Assert.AreEqual(0xe7a, this.cpu.X.Word);
            Assert.AreEqual(0, this.cpu.Zero);
        }
    }
}