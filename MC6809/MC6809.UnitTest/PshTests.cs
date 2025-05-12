// <copyright file="PshTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PshTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public PshTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestPSHS_all()
        {
            this.cpu.S.Word = 517;
            this.cpu.CC = 0xf;
            this.cpu.A = 1;
            this.cpu.B = 2;
            this.cpu.DP = 3;
            this.cpu.X.Word = 0x405;
            this.cpu.Y.Word = 0x607;
            this.cpu.U.Word = 0x809;
            this.cpu.PC.Word = 0xb00;
            this.board.Poke(0xb00, 0x34);
            this.board.Poke(0xb01, 0xff);

            this.cpu.Step();

            Assert.AreEqual(0xb02, this.cpu.PC.Word);
            Assert.AreEqual(0xf, this.cpu.CC);
            Assert.AreEqual(1, this.cpu.A);
            Assert.AreEqual(2, this.cpu.B);
            Assert.AreEqual(517 - 12, this.cpu.S.Word);
            Assert.AreEqual(0x02, this.board.Peek(517 - 1));
            Assert.AreEqual(0x0b, this.board.Peek(517 - 2));
            Assert.AreEqual(0x09, this.board.Peek(517 - 3));
            Assert.AreEqual(0x08, this.board.Peek(517 - 4));
            Assert.AreEqual(0x07, this.board.Peek(517 - 5));
            Assert.AreEqual(0x06, this.board.Peek(517 - 6));
            Assert.AreEqual(0x05, this.board.Peek(517 - 7));
            Assert.AreEqual(0x04, this.board.Peek(517 - 8));
            Assert.AreEqual(0x03, this.board.Peek(517 - 9));
            Assert.AreEqual(0x02, this.board.Peek(517 - 10));
            Assert.AreEqual(0x01, this.board.Peek(517 - 11));
            Assert.AreEqual(0x0f, this.board.Peek(517 - 12));
        }

        [TestMethod]
        public void TestPSHU_all()
        {
            this.cpu.U.Word = 517;
            this.cpu.CC = 0xf;
            this.cpu.A = 1;
            this.cpu.B = 2;
            this.cpu.DP = 3;
            this.cpu.X.Word = 0x405;
            this.cpu.Y.Word = 0x607;
            this.cpu.S.Word = 0x809;
            this.cpu.PC.Word = 0xb00;
            this.board.Poke(0xb00, 0x36);
            this.board.Poke(0xb01, 0xff);

            this.cpu.Step();

            Assert.AreEqual(0xb02, this.cpu.PC.Word);
            Assert.AreEqual(0xf, this.cpu.CC);
            Assert.AreEqual(1, this.cpu.A);
            Assert.AreEqual(2, this.cpu.B);
            Assert.AreEqual(517 - 12, this.cpu.U.Word);
            Assert.AreEqual(0x02, this.board.Peek(517 - 1));
            Assert.AreEqual(0x0b, this.board.Peek(517 - 2));
            Assert.AreEqual(0x09, this.board.Peek(517 - 3));
            Assert.AreEqual(0x08, this.board.Peek(517 - 4));
            Assert.AreEqual(0x07, this.board.Peek(517 - 5));
            Assert.AreEqual(0x06, this.board.Peek(517 - 6));
            Assert.AreEqual(0x05, this.board.Peek(517 - 7));
            Assert.AreEqual(0x04, this.board.Peek(517 - 8));
            Assert.AreEqual(0x03, this.board.Peek(517 - 9));
            Assert.AreEqual(0x02, this.board.Peek(517 - 10));
            Assert.AreEqual(0x01, this.board.Peek(517 - 11));
            Assert.AreEqual(0x0f, this.board.Peek(517 - 12));
        }
    }
}
