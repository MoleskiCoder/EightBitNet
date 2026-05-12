// <copyright file="SchwotzerUnaryTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

// Test data from W. Schwotzer's MC6809 CPU Emulation Validation suite,
// verified on an SGS Thomson EF6809P processor.

namespace MC6809.UnitTest
{
    [TestClass]
    public class SchwotzerUnaryTests
    {
        private readonly Board board = new();
        private readonly MC6809 cpu;

        public SchwotzerUnaryTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise() { this.board.RaisePOWER(); this.cpu.Step(); }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // NEG: checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]
        [DataRow(0x80, 0x80, 0x0B)]
        [DataRow(0x01, 0xFF, 0x09)]
        [DataRow(0xFF, 0x01, 0x01)]
        [DataRow(0x7F, 0x81, 0x09)]
        [DataRow(0x81, 0x7F, 0x01)]
        [DataRow(0xC0, 0x40, 0x01)]
        [DataRow(0x40, 0xC0, 0x09)]
        public void TestNEGA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x40);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // COM: checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x00, 0xFF, 0x09)]
        [DataRow(0xFF, 0x00, 0x05)]
        [DataRow(0xF0, 0x0F, 0x01)]
        [DataRow(0x0F, 0xF0, 0x09)]
        [DataRow(0x55, 0xAA, 0x09)]
        [DataRow(0xAA, 0x55, 0x01)]
        [DataRow(0x01, 0xFE, 0x09)]
        [DataRow(0xFE, 0x01, 0x01)]
        [DataRow(0x80, 0x7F, 0x01)]
        [DataRow(0x7F, 0x80, 0x09)]
        public void TestCOMA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x43);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // CLR: checks NZVC ($0F) — always clears to zero, Z=1
        [DataTestMethod]
        [DataRow(0x01, 0x04)]
        [DataRow(0x00, 0x04)]
        [DataRow(0xFF, 0x04)]
        [DataRow(0x80, 0x04)]
        [DataRow(0x7F, 0x04)]
        [DataRow(0x10, 0x04)]
        [DataRow(0x0F, 0x04)]
        public void TestCLRA(int input, int flags)
        {
            this.board.Poke(0, 0x4F);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // DEC: checks NZV ($0E)
        [DataTestMethod]
        [DataRow(0x01, 0x00, 0x04)]
        [DataRow(0x00, 0xFF, 0x08)]
        [DataRow(0xFF, 0xFE, 0x08)]
        [DataRow(0x80, 0x7F, 0x02)]
        [DataRow(0x7F, 0x7E, 0x00)]
        [DataRow(0x10, 0x0F, 0x00)]
        [DataRow(0x0F, 0x0E, 0x00)]
        public void TestDECA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x4A);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // INC: checks NZV ($0E)
        [DataTestMethod]
        [DataRow(0x00, 0x01, 0x00)]
        [DataRow(0xFE, 0xFF, 0x08)]
        [DataRow(0xFF, 0x00, 0x04)]
        [DataRow(0x7F, 0x80, 0x0A)]
        [DataRow(0x80, 0x81, 0x08)]
        [DataRow(0x0F, 0x10, 0x00)]
        [DataRow(0x10, 0x11, 0x00)]
        public void TestINCA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x4C);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // TST: checks NZV ($0E) — does not modify A
        [DataTestMethod]
        [DataRow(0x00, 0x04)]
        [DataRow(0x80, 0x08)]
        [DataRow(0x01, 0x00)]
        [DataRow(0xFF, 0x08)]
        [DataRow(0x7F, 0x00)]
        [DataRow(0x81, 0x08)]
        [DataRow(0xC0, 0x08)]
        [DataRow(0x40, 0x00)]
        public void TestTSTA(int input, int flags)
        {
            this.board.Poke(0, 0x4D);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)input, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // LSR: checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]
        [DataRow(0xFF, 0x7F, 0x01)]
        [DataRow(0x7F, 0x3F, 0x01)]
        [DataRow(0x3F, 0x1F, 0x01)]
        [DataRow(0x1F, 0x0F, 0x01)]
        [DataRow(0x0F, 0x07, 0x01)]
        [DataRow(0x07, 0x03, 0x01)]
        [DataRow(0x03, 0x01, 0x01)]
        [DataRow(0x01, 0x00, 0x05)]
        [DataRow(0x55, 0x2A, 0x01)]
        [DataRow(0xAA, 0x55, 0x00)]
        [DataRow(0x80, 0x40, 0x00)]
        [DataRow(0x10, 0x08, 0x00)]
        [DataRow(0xC0, 0x60, 0x00)]
        [DataRow(0xE0, 0x70, 0x00)]
        [DataRow(0xF0, 0x78, 0x00)]
        [DataRow(0xF8, 0x7C, 0x00)]
        [DataRow(0xFC, 0x7E, 0x00)]
        [DataRow(0xFE, 0x7F, 0x00)]
        public void TestLSRA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x44);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // ASR: checks NZC ($0D)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]
        [DataRow(0xFF, 0xFF, 0x09)]
        [DataRow(0x7F, 0x3F, 0x01)]
        [DataRow(0x3F, 0x1F, 0x01)]
        [DataRow(0x1F, 0x0F, 0x01)]
        [DataRow(0x0F, 0x07, 0x01)]
        [DataRow(0x07, 0x03, 0x01)]
        [DataRow(0x03, 0x01, 0x01)]
        [DataRow(0x01, 0x00, 0x05)]
        [DataRow(0x55, 0x2A, 0x01)]
        [DataRow(0xAA, 0xD5, 0x08)]
        [DataRow(0x80, 0xC0, 0x08)]
        [DataRow(0x10, 0x08, 0x00)]
        [DataRow(0xC0, 0xE0, 0x08)]
        [DataRow(0xE0, 0xF0, 0x08)]
        [DataRow(0xF0, 0xF8, 0x08)]
        [DataRow(0xF8, 0xFC, 0x08)]
        [DataRow(0xFC, 0xFE, 0x08)]
        [DataRow(0xFE, 0xFF, 0x08)]
        public void TestASRA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x47);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0D));
        }

        // SEX: input in B, sign-extended into D; checks NZ ($0C)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x00, 0x04)]
        [DataRow(0x01, 0x00, 0x01, 0x00)]
        [DataRow(0x7F, 0x00, 0x7F, 0x00)]
        [DataRow(0x80, 0xFF, 0x80, 0x08)]
        [DataRow(0xFF, 0xFF, 0xFF, 0x08)]
        public void TestSEX(int inputB, int expectedA, int expectedB, int flags)
        {
            this.board.Poke(0, 0x1D);
            this.cpu.CC = 0;
            this.cpu.B = (byte)inputB;
            this.cpu.Step();
            Assert.AreEqual((byte)expectedA, this.cpu.A);
            Assert.AreEqual((byte)expectedB, this.cpu.B);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0C));
        }

        // MUL: A × B → D (unsigned); checks ZC ($05)
        // C is set if bit 7 of the result's low byte (B) is set
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x00, 0x00, 0x04)]
        [DataRow(0x80, 0x80, 0x40, 0x00, 0x00)]
        [DataRow(0x01, 0xFF, 0x00, 0xFF, 0x01)]
        [DataRow(0xFF, 0x01, 0x00, 0xFF, 0x01)]
        [DataRow(0x7F, 0x81, 0x3F, 0xFF, 0x01)]
        [DataRow(0x81, 0x7F, 0x3F, 0xFF, 0x01)]
        [DataRow(0xC0, 0x40, 0x30, 0x00, 0x00)]
        [DataRow(0x40, 0xC0, 0x30, 0x00, 0x00)]
        [DataRow(0xFF, 0xFF, 0xFE, 0x01, 0x00)]
        [DataRow(0x7F, 0x7F, 0x3F, 0x01, 0x00)]
        [DataRow(0x01, 0x01, 0x00, 0x01, 0x00)]
        public void TestMUL(int inputA, int inputB, int expectedA, int expectedB, int flags)
        {
            this.board.Poke(0, 0x3D);
            this.cpu.CC = 0;
            this.cpu.A = (byte)inputA;
            this.cpu.B = (byte)inputB;
            this.cpu.Step();
            Assert.AreEqual((byte)expectedA, this.cpu.A);
            Assert.AreEqual((byte)expectedB, this.cpu.B);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x05));
        }
    }
}
