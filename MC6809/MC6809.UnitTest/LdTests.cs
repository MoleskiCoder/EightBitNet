// <copyright file="LdTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LdTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public LdTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestIndexedAccumulatorOffset_A()
        {
            this.board.Poke(0, 0xe6);
            this.board.Poke(1, 0x86);
            this.cpu.A = 0x2b;
            this.cpu.B = 0x00;
            this.cpu.X.Word = 0xc300;
            this.board.Poke(0xc32b, 0x4e);
            this.cpu.Step();
            Assert.AreEqual(0x4e, this.cpu.B);
        }

        [TestMethod]
        public void TestIndexedAccumulatorOffset_B()
        {
            this.board.Poke(0, 0xa6);
            this.board.Poke(1, 0x85);
            this.cpu.A = 0x00;
            this.cpu.B = 0x2b;
            this.cpu.X.Word = 0xc300;
            this.board.Poke(0xc32b, 0x4e);
            this.cpu.Step();
            Assert.AreEqual(0x4e, this.cpu.A);
        }

        [TestMethod]
        public void TestIndirectIndexedAccumulatorOffset()
        {
            this.board.Poke(0, 0xa6);
            this.board.Poke(1, 0x95);
            this.cpu.A = 0x00;
            this.cpu.B = 0x2b;
            this.cpu.X.Word = 0xc300;
            this.cpu.PokeWord(0xc32b, 0x1234);
            this.board.Poke(0x1234, 0x56);
            this.cpu.Step();
            Assert.AreEqual(0x56, this.cpu.A);
        }
    }
}
