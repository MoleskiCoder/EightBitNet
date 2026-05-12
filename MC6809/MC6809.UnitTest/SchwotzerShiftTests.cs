// <copyright file="SchwotzerShiftTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

// Test data from W. Schwotzer's MC6809 CPU Emulation Validation suite,
// verified on an SGS Thomson EF6809P processor.
//
// Note: the Schwotzer LSL flag table was demonstrably incorrect on several
// entries (the original LBNE OUTERR for flags was commented out). Result
// values are hardware-verified; expected flags are from the MC6809 spec:
//   C = old bit 7, N = result bit 7, Z = result==0, V = old_bit7 XOR old_bit6.

namespace MC6809.UnitTest
{
    [TestClass]
    public class SchwotzerShiftTests
    {
        private readonly Board board = new();
        private readonly MC6809 cpu;

        public SchwotzerShiftTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise() { this.board.RaisePOWER(); this.cpu.Step(); }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // LSL: result from hardware; flags from MC6809 spec; checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]  // C=0 N=0 Z=1 V=0
        [DataRow(0xFF, 0xFE, 0x09)]  // C=1 N=1 Z=0 V=0 (bit7^bit6 = 1^1 = 0)
        [DataRow(0x7F, 0xFE, 0x0A)]  // C=0 N=1 Z=0 V=1 (0^1=1)
        [DataRow(0x3F, 0x7E, 0x00)]  // C=0 N=0 Z=0 V=0 (0^0=0)
        [DataRow(0x1F, 0x3E, 0x00)]
        [DataRow(0x0F, 0x1E, 0x00)]
        [DataRow(0x07, 0x0E, 0x00)]
        [DataRow(0x03, 0x06, 0x00)]
        [DataRow(0x01, 0x02, 0x00)]
        [DataRow(0x55, 0xAA, 0x0A)]  // C=0 N=1 Z=0 V=1 (0^1=1)
        [DataRow(0xAA, 0x54, 0x03)]  // C=1 N=0 Z=0 V=1 (1^0=1)
        [DataRow(0x80, 0x00, 0x07)]  // C=1 N=0 Z=1 V=1 (1^0=1)
        [DataRow(0x10, 0x20, 0x00)]  // C=0 N=0 Z=0 V=0 (0^0=0)
        [DataRow(0xC0, 0x80, 0x09)]  // C=1 N=1 Z=0 V=0 (1^1=0)
        [DataRow(0xE0, 0xC0, 0x09)]
        [DataRow(0xF0, 0xE0, 0x09)]
        [DataRow(0xF8, 0xF0, 0x09)]
        [DataRow(0xFC, 0xF8, 0x09)]
        [DataRow(0xFE, 0xFC, 0x09)]
        public void TestLSLA(int input, int expected, int flags)
        {
            this.board.Poke(0, 0x48);
            this.cpu.CC = 0;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // ROR: carry-in from carryIn parameter; checks NZC ($0D)
        [DataTestMethod]
        [DataRow(0x00, 0, 0x00, 0x04)]
        [DataRow(0x01, 0, 0x00, 0x05)]
        [DataRow(0x0F, 0, 0x07, 0x01)]
        [DataRow(0x10, 0, 0x08, 0x00)]
        [DataRow(0x7F, 0, 0x3F, 0x01)]
        [DataRow(0x80, 0, 0x40, 0x00)]
        [DataRow(0xFE, 0, 0x7F, 0x00)]
        [DataRow(0xFF, 0, 0x7F, 0x01)]
        [DataRow(0x00, 1, 0x80, 0x08)]
        [DataRow(0x01, 1, 0x80, 0x09)]
        [DataRow(0x0F, 1, 0x87, 0x09)]
        [DataRow(0x10, 1, 0x88, 0x08)]
        [DataRow(0x7F, 1, 0xBF, 0x09)]
        [DataRow(0x80, 1, 0xC0, 0x08)]
        [DataRow(0xFE, 1, 0xFF, 0x08)]
        [DataRow(0xFF, 1, 0xFF, 0x09)]
        public void TestRORA(int input, int carryIn, int expected, int flags)
        {
            this.board.Poke(0, 0x46);
            this.cpu.CC = (byte)carryIn;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0D));
        }

        // ROL: carry-in from carryIn parameter; checks NZC ($0D)
        [DataTestMethod]
        [DataRow(0x00, 0, 0x00, 0x04)]
        [DataRow(0x01, 0, 0x02, 0x00)]
        [DataRow(0x0F, 0, 0x1E, 0x00)]
        [DataRow(0x08, 0, 0x10, 0x00)]
        [DataRow(0x7F, 0, 0xFE, 0x08)]
        [DataRow(0x80, 0, 0x00, 0x05)]
        [DataRow(0xFE, 0, 0xFC, 0x09)]
        [DataRow(0xFF, 0, 0xFE, 0x09)]
        [DataRow(0x00, 1, 0x01, 0x00)]
        [DataRow(0x01, 1, 0x03, 0x00)]
        [DataRow(0x0F, 1, 0x1F, 0x00)]
        [DataRow(0x08, 1, 0x11, 0x00)]
        [DataRow(0x7F, 1, 0xFF, 0x08)]
        [DataRow(0x80, 1, 0x01, 0x01)]
        [DataRow(0xFE, 1, 0xFD, 0x09)]
        [DataRow(0xFF, 1, 0xFF, 0x09)]
        public void TestROLA(int input, int carryIn, int expected, int flags)
        {
            this.board.Poke(0, 0x49);
            this.cpu.CC = (byte)carryIn;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0D));
        }

        // BIT: AND without storing result; checks NZV ($0E)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]
        [DataRow(0xAA, 0x55, 0x04)]
        [DataRow(0xAA, 0xAA, 0x08)]
        [DataRow(0x55, 0x55, 0x00)]
        [DataRow(0xFF, 0xFF, 0x08)]
        [DataRow(0xFF, 0x80, 0x08)]
        [DataRow(0x81, 0x80, 0x08)]
        [DataRow(0xFF, 0x7F, 0x00)]
        [DataRow(0xFF, 0x01, 0x00)]
        [DataRow(0xF0, 0x0F, 0x04)]
        public void TestBITA(int operand, int data, int flags)
        {
            this.board.Poke(0, 0x85);
            this.board.Poke(1, (byte)data);
            this.cpu.CC = 0;
            this.cpu.A = (byte)operand;
            this.cpu.Step();
            Assert.AreEqual((byte)operand, this.cpu.A);  // A must not be modified
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }
    }
}
