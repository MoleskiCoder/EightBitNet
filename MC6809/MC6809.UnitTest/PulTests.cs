// <copyright file="PulTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PulTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public PulTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestPULS_all()
        {
            this.board.Poke(512 - 12, 0x00);
            this.board.Poke(512 - 11, 0x11);
            this.board.Poke(512 - 10, 0x12);
            this.board.Poke(512 - 9, 0x13);
            this.cpu.PokeWord(512 - 8, 0x9141);
            this.cpu.PokeWord(512 - 6, 0xa142);
            this.cpu.PokeWord(512 - 4, 0xb140);
            this.cpu.PokeWord(512 - 2, 0x04ff);
            this.cpu.Y.Word = 0x1115;
            this.cpu.S.Word = 500;
            this.cpu.CC = 0xf;
            this.board.Poke(0xb00, 0x35);
            this.board.Poke(0xb01, 0xff);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.CC);
            Assert.AreEqual(0x11, this.cpu.A);
            Assert.AreEqual(0x12, this.cpu.B);
            Assert.AreEqual(0x13, this.cpu.DP);
            Assert.AreEqual(0x9141, this.cpu.X.Word);
            Assert.AreEqual(0xa142, this.cpu.Y.Word);
            Assert.AreEqual(0xb140, this.cpu.U.Word);
            Assert.AreEqual(0x4ff, this.cpu.PC.Word);
        }

        [TestMethod]
        public void TestPULU_all()
        {
            this.board.Poke(512 - 12, 0x00);
            this.board.Poke(512 - 11, 0x11);
            this.board.Poke(512 - 10, 0x12);
            this.board.Poke(512 - 9, 0x13);
            this.cpu.PokeWord(512 - 8, 0x9141);
            this.cpu.PokeWord(512 - 6, 0xa142);
            this.cpu.PokeWord(512 - 4, 0xb140);
            this.cpu.PokeWord(512 - 2, 0x04ff);
            this.cpu.Y.Word = 0x1115;
            this.cpu.U.Word = 500;
            this.cpu.CC = 0xf;
            this.board.Poke(0xb00, 0x37);
            this.board.Poke(0xb01, 0xff);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.CC);
            Assert.AreEqual(0x11, this.cpu.A);
            Assert.AreEqual(0x12, this.cpu.B);
            Assert.AreEqual(0x13, this.cpu.DP);
            Assert.AreEqual(0x9141, this.cpu.X.Word);
            Assert.AreEqual(0xa142, this.cpu.Y.Word);
            Assert.AreEqual(0xb140, this.cpu.S.Word);
            Assert.AreEqual(0x4ff, this.cpu.PC.Word);
        }

        [TestMethod]
        public void TestPULS_y()
        {
            this.cpu.PokeWord(0x205, 0xb140);
            this.cpu.PokeWord(0x207, 0x04ff);
            this.cpu.Y.Word = 0x1115;
            this.cpu.S.Word = 0x205;
            this.cpu.CC = 0xf;
            this.board.Poke(0xb00, 0x35);
            this.board.Poke(0xb01, 0xa0);
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xb140, this.cpu.Y.Word);
            Assert.AreEqual(0x4ff, this.cpu.PC.Word);
            Assert.AreEqual(0xf, this.cpu.CC);
        }
    }
}
