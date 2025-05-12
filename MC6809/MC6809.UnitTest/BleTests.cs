// <copyright file="BleTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BleTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public BleTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x2f);   // BLE
            this.board.Poke(1, 0x03);
            this.board.Poke(2, 0x86);   // LDA #1
            this.board.Poke(3, 0x01);
            this.board.Poke(4, 0x12);   // NOP
            this.board.Poke(5, 0x86);   // LDA #2
            this.board.Poke(6, 0x02);
            this.board.Poke(7, 0x12);   // NOP
        }

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step();
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void TestBLE1()
        {
            Assert.AreEqual(0, this.cpu.PC.Word);
            this.cpu.CC = (byte)StatusBits.ZF;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
        }

        [TestMethod]
        public void TestBLE2()
        {
            this.cpu.A = 0;
            this.cpu.CC = 0;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }

        [TestMethod]
        public void TestBLE3()
        {
            this.cpu.CC = (byte)StatusBits.NF;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
        }

        [TestMethod]
        public void TestBLE4()
        {
            this.cpu.A = 0;
            this.cpu.CC = (byte)(StatusBits.NF | StatusBits.VF);
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }

        [TestMethod]
        public void TestBGT5()
        {
            this.cpu.CC = (byte)(StatusBits.ZF | StatusBits.NF);
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
        }
    }
}