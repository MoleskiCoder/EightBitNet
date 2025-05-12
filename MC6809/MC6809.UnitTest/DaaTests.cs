// <copyright file="DaaTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DaaTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public DaaTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // Decimal Addition Adjust.
        // The Half-Carry flag is not affected by this instruction.
        // The Negative flag is set equal to the new value of bit 7 in Accumulator A.
        // The Zero flag is set if the new value of Accumulator A is zero; cleared otherwise.
        // The affect this instruction has on the Overflow flag is undefined.
        // The Carry flag is set if the BCD addition produced a carry; cleared otherwise.
        [TestMethod]
        public void TestDAA()
        {
            this.board.Poke(0xb00, 0x19);
            this.cpu.CC = 0;
            this.cpu.A = 0x7f;
            this.cpu.PC.Word = 0xb00;

            this.cpu.Step();

            Assert.AreEqual(0x85, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
        }
    }
}
