// <copyright file="IncTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IncTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public IncTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestInherentINCA1()
        {
            this.board.Poke(0, 0x4c);
            this.cpu.CC = 0;
            this.cpu.A = 0x32;
            this.cpu.Step();
            Assert.AreEqual(0x33, this.cpu.A);
            Assert.IsFalse(this.cpu.Carry);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestInherentINCA2()
        {
            this.board.Poke(0, 0x4c);
            this.cpu.CC = 0;
            this.cpu.A = 0x7f;
            this.cpu.Step();
            Assert.AreEqual(0x80, this.cpu.A);
            Assert.IsTrue(this.cpu.Negative);
            Assert.IsFalse(this.cpu.Zero);
            Assert.IsTrue(this.cpu.Overflow);
            Assert.IsFalse(this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestInherentINCA3()
        {
            this.board.Poke(0, 0x4c);
            this.cpu.CC = 0;
            this.cpu.A = 0xff;
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.IsFalse(this.cpu.Negative);
            Assert.IsTrue(this.cpu.Zero);
            Assert.IsFalse(this.cpu.Overflow);
            Assert.IsFalse(this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // INC does not affect H: H=1 before must be H=1 after
        [TestMethod]
        public void TestInherentINCA_preserves_half_carry_set()
        {
            this.board.Poke(0, 0x4c);
            this.cpu.CC = (byte)StatusBits.HF;
            this.cpu.A = 0x01;
            this.cpu.Step();
            Assert.AreEqual(0x02, this.cpu.A);
            Assert.IsTrue(this.cpu.HalfCarry);
        }

        // INC does not affect H: a nibble-crossing increment must not set H=1
        [TestMethod]
        public void TestInherentINCA_preserves_half_carry_clear()
        {
            this.board.Poke(0, 0x4c);
            this.cpu.CC = 0;
            this.cpu.A = 0x0f;
            this.cpu.Step();
            Assert.AreEqual(0x10, this.cpu.A);
            Assert.IsFalse(this.cpu.HalfCarry);
        }
    }
}
