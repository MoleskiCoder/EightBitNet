// <copyright file="ExgTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExgTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public ExgTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestEXG_a_dp()
        {
            this.board.Poke(0xb00, 0x1e);
            this.board.Poke(0xb01, 0x8B);
            this.cpu.CC = 0;
            this.cpu.A = 0x7f;
            this.cpu.DP = 0xf6;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x7f, this.cpu.DP);
            Assert.AreEqual(0xf6, this.cpu.A);
        }

        [TestMethod]
        public void TestEXG_d_x()
        {
            this.board.Poke(0xb00, 0x1e);
            this.board.Poke(0xb01, 0x01);
            this.cpu.CC = 0;
            this.cpu.D.Word = 0x117f;
            this.cpu.X.Word = 0xff16;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0xff16, this.cpu.D.Word);
            Assert.AreEqual(0x117f, this.cpu.X.Word);
        }

        [TestMethod]
        public void TestEXG_a_x()
        {
            this.board.Poke(0xb00, 0x1e);
            this.board.Poke(0xb01, 0x81);
            this.cpu.CC = 0;
            this.cpu.A = 0x56;
            this.cpu.X.Word = 0x1234;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x34, this.cpu.A);
            Assert.AreEqual(0xff56, this.cpu.X.Word);
        }
    }
}
