// <copyright file="JsrTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    // https://github.com/sorenroug/osnine-java/blob/master/core/src/test/java/org/roug/osnine/BranchAndJumpTest.java
    [TestClass]
    public class JsrTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public JsrTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x3a);
        }

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // Test the JSR - Jump to Subroutine - instruction.
        // INDEXED mode:   JSR   D,Y
        [TestMethod]
        public void TestJsr()
        {
            // Set up a word to test at address 0x205
            this.cpu.PokeWord(0x205, 0x03ff);

            // Set register D
            this.cpu.D.Word = 0x105;

            // Set register Y to point to that location minus 5
            this.cpu.Y.Word = 0x200;

            // Set register S to point to 0x915
            this.cpu.S.Word = 0x915;

            // Two bytes of instruction
            this.board.Poke(0xB00, 0xAD);
            this.board.Poke(0xB01, 0xAB);
            this.board.Poke(0xB02, 0x11); // Junk
            this.board.Poke(0xB03, 0x22); // Junk

            this.cpu.PC.Word = 0xB00;
            this.cpu.CC = 0;

            this.cpu.Step();

            Assert.AreEqual(0, this.cpu.CC);
            Assert.AreEqual(0x01, this.cpu.A);
            Assert.AreEqual(0x05, this.cpu.B);
            Assert.AreEqual(0, this.cpu.DP);
            Assert.AreEqual(0, this.cpu.X.Word);
            Assert.AreEqual(0x200, this.cpu.Y.Word);
            Assert.AreEqual(0x105, this.cpu.D.Word);
            Assert.AreEqual(0x913, this.cpu.S.Word);
            Assert.AreEqual(0, this.cpu.U.Word);
            Assert.AreEqual(0x305, this.cpu.PC.Word);

            Assert.AreEqual(2, this.board.Peek(0x914));
            Assert.AreEqual(0xb, this.board.Peek(0x913));

            Assert.AreEqual(10, this.cpu.Cycles);
        }
    }
}