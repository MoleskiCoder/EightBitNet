// <copyright file="SchwotzerCompareLoadTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

// Test data from W. Schwotzer's MC6809 CPU Emulation Validation suite,
// verified on an SGS Thomson EF6809P processor.

namespace MC6809.UnitTest
{
    [TestClass]
    public class SchwotzerCompareLoadTests
    {
        private readonly Board board = new();
        private readonly MC6809 cpu;

        public SchwotzerCompareLoadTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise() { this.board.RaisePOWER(); this.cpu.Step(); }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // CMPB immediate ($C1): same data as SUBB but B is not modified; checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x00, 0x00, 0x04)]
        [DataRow(0x00, 0x01, 0x09)]
        [DataRow(0x00, 0x7F, 0x09)]
        [DataRow(0x00, 0x80, 0x0B)]
        [DataRow(0x00, 0xFF, 0x01)]
        [DataRow(0x01, 0x00, 0x00)]
        [DataRow(0x01, 0x7F, 0x09)]
        [DataRow(0x40, 0x41, 0x09)]
        [DataRow(0x40, 0x80, 0x0B)]
        [DataRow(0x40, 0xC0, 0x0B)]
        [DataRow(0x40, 0xC1, 0x01)]
        [DataRow(0x7F, 0x00, 0x00)]
        [DataRow(0x7F, 0x01, 0x00)]
        [DataRow(0x7F, 0x7E, 0x00)]
        [DataRow(0x7F, 0x7F, 0x04)]
        [DataRow(0x7F, 0x80, 0x0B)]
        [DataRow(0x7F, 0xFF, 0x0B)]
        [DataRow(0x80, 0x01, 0x02)]
        [DataRow(0x80, 0x40, 0x02)]
        [DataRow(0x80, 0x7F, 0x02)]
        [DataRow(0x80, 0x80, 0x04)]
        [DataRow(0x80, 0x81, 0x09)]
        [DataRow(0x81, 0x80, 0x00)]
        [DataRow(0xFF, 0xFF, 0x04)]
        [DataRow(0xFF, 0xFE, 0x00)]
        [DataRow(0xFF, 0x00, 0x08)]
        public void TestCMPB(int operand, int data, int flags)
        {
            this.board.Poke(0, 0xC1);
            this.board.Poke(1, (byte)data);
            this.cpu.CC = 0;
            this.cpu.B = (byte)operand;
            this.cpu.Step();
            Assert.AreEqual((byte)operand, this.cpu.B);  // B must not be modified
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // CMPD immediate ($10,$83): same data as SUBD but D is not modified; checks NZVC ($0F)
        [DataTestMethod]
        [DataRow(0x0000, 0x0000, 0x04)]
        [DataRow(0x0000, 0x0001, 0x09)]
        [DataRow(0x0000, 0x7FFF, 0x09)]
        [DataRow(0x0000, 0x8000, 0x0B)]
        [DataRow(0x0000, 0xFFFF, 0x01)]
        [DataRow(0x0001, 0x0000, 0x00)]
        [DataRow(0x0001, 0x7FFF, 0x09)]
        [DataRow(0x4000, 0x4001, 0x09)]
        [DataRow(0x4000, 0x8000, 0x0B)]
        [DataRow(0x4000, 0xC000, 0x0B)]
        [DataRow(0x4000, 0xC001, 0x01)]
        [DataRow(0x7FFF, 0x0000, 0x00)]
        [DataRow(0x7FFF, 0x0001, 0x00)]
        [DataRow(0x7FFF, 0x7FFE, 0x00)]
        [DataRow(0x7FFF, 0x7FFF, 0x04)]
        [DataRow(0x7FFF, 0x8000, 0x0B)]
        [DataRow(0x7FFF, 0xFFFF, 0x0B)]
        [DataRow(0x8000, 0x0001, 0x02)]
        [DataRow(0x8000, 0x4000, 0x02)]
        [DataRow(0x8000, 0x7FFF, 0x02)]
        [DataRow(0x8000, 0x8000, 0x04)]
        [DataRow(0x8000, 0x8001, 0x09)]
        [DataRow(0x8001, 0x8000, 0x00)]
        [DataRow(0xFFFF, 0xFFFF, 0x04)]
        [DataRow(0xFFFF, 0xFFFE, 0x00)]
        [DataRow(0xFFFF, 0x0000, 0x08)]
        public void TestCMPD(int operand, int data, int flags)
        {
            this.board.Poke(0, 0x10);
            this.board.Poke(1, 0x83);
            this.board.Poke(2, (byte)(data >> 8));
            this.board.Poke(3, (byte)(data & 0xFF));
            this.cpu.CC = 0;
            this.cpu.D.Joined = (ushort)operand;
            this.cpu.Step();
            Assert.AreEqual((ushort)operand, this.cpu.D.Joined);  // D must not be modified
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0F));
        }

        // LDA immediate ($86): checks NZV ($0E) — V always cleared
        [DataTestMethod]
        [DataRow(0x00, 0x04)]
        [DataRow(0x01, 0x00)]
        [DataRow(0x7F, 0x00)]
        [DataRow(0x80, 0x08)]
        [DataRow(0xFF, 0x08)]
        [DataRow(0xAA, 0x08)]
        [DataRow(0x55, 0x00)]
        [DataRow(0x10, 0x00)]
        public void TestLDA(int data, int flags)
        {
            this.board.Poke(0, 0x86);
            this.board.Poke(1, (byte)data);
            this.cpu.CC = 0;
            this.cpu.A = 0xFF;  // pre-load a different value
            this.cpu.Step();
            Assert.AreEqual((byte)data, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // LDD immediate ($CC): checks NZV ($0E) — V always cleared
        [DataTestMethod]
        [DataRow(0x0000, 0x04)]
        [DataRow(0x0001, 0x00)]
        [DataRow(0x7FFF, 0x00)]
        [DataRow(0x8000, 0x08)]
        [DataRow(0xFFFF, 0x08)]
        [DataRow(0xAAAA, 0x08)]
        [DataRow(0x5555, 0x00)]
        [DataRow(0x1000, 0x00)]
        public void TestLDD(int data, int flags)
        {
            this.board.Poke(0, 0xCC);
            this.board.Poke(1, (byte)(data >> 8));
            this.board.Poke(2, (byte)(data & 0xFF));
            this.cpu.CC = 0;
            this.cpu.D.Joined = 0x1234;  // pre-load a different value
            this.cpu.Step();
            Assert.AreEqual((ushort)data, this.cpu.D.Joined);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // STA extended ($B7 addr): store A to memory, checks NZV ($0E) — V always cleared
        // Stores to $0100 so it doesn't overwrite the instruction at $0000
        [DataTestMethod]
        [DataRow(0x00, 0x04)]
        [DataRow(0x01, 0x00)]
        [DataRow(0x7F, 0x00)]
        [DataRow(0x80, 0x08)]
        [DataRow(0xFF, 0x08)]
        [DataRow(0xAA, 0x08)]
        [DataRow(0x55, 0x00)]
        [DataRow(0x10, 0x00)]
        public void TestSTA(int data, int flags)
        {
            this.board.Poke(0, 0xB7);
            this.board.Poke(1, 0x01);
            this.board.Poke(2, 0x00);
            this.cpu.CC = 0;
            this.cpu.A = (byte)data;
            this.cpu.Step();
            Assert.AreEqual((byte)data, this.board.Peek(0x0100));
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // STD extended ($FD addr): store D to memory, checks NZV ($0E) — V always cleared
        // Stores to $0100 so it doesn't overwrite the instruction
        [DataTestMethod]
        [DataRow(0x0000, 0x04)]
        [DataRow(0x0001, 0x00)]
        [DataRow(0x7FFF, 0x00)]
        [DataRow(0x8000, 0x08)]
        [DataRow(0xFFFF, 0x08)]
        [DataRow(0xAAAA, 0x08)]
        [DataRow(0x5555, 0x00)]
        [DataRow(0x1000, 0x00)]
        public void TestSTD(int data, int flags)
        {
            this.board.Poke(0, 0xFD);
            this.board.Poke(1, 0x01);
            this.board.Poke(2, 0x00);
            this.cpu.CC = 0;
            this.cpu.D.Joined = (ushort)data;
            this.cpu.Step();
            var stored = (ushort)((this.board.Peek(0x0100) << 8) | this.board.Peek(0x0101));
            Assert.AreEqual((ushort)data, stored);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0E));
        }

        // LEAY ,Y ($31,$A4): load Y with effective address of ,Y (i.e. Y itself); checks Z ($04)
        // Z is set if result is zero, cleared otherwise. No other CC flags affected.
        [DataTestMethod]
        [DataRow(0x0000, 0x04)]
        [DataRow(0x0001, 0x00)]
        [DataRow(0xFFFF, 0x00)]
        [DataRow(0x7FFF, 0x00)]
        [DataRow(0x8000, 0x00)]
        [DataRow(0x8001, 0x00)]
        public void TestLEAY(int yValue, int flags)
        {
            this.board.Poke(0, 0x31);
            this.board.Poke(1, 0xA4);  // ,Y (no offset, indirect=0)
            this.cpu.CC = 0;
            this.cpu.Y.Joined = (ushort)yValue;
            this.cpu.Step();
            Assert.AreEqual((ushort)yValue, this.cpu.Y.Joined);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x04));
        }

        // DAA ($19): decimal adjust A after BCD addition; checks NZC ($0D)
        // initialCC encodes H (bit5) and C (bit0) before the instruction
        // 64 rows: exact Schwotzer TBDAA table, hardware-verified on EF6809P
        [DataTestMethod]
        // H=0, C=0 (initialCC=0x00)
        [DataRow(0x00, 0x00, 0x00, 0x04)]
        [DataRow(0x00, 0x01, 0x01, 0x00)]
        [DataRow(0x00, 0x09, 0x09, 0x00)]
        [DataRow(0x00, 0x0A, 0x10, 0x00)]
        [DataRow(0x00, 0x0F, 0x15, 0x00)]
        [DataRow(0x00, 0x10, 0x10, 0x00)]
        [DataRow(0x00, 0x4A, 0x50, 0x00)]
        [DataRow(0x00, 0x79, 0x79, 0x00)]
        [DataRow(0x00, 0x7A, 0x80, 0x08)]
        [DataRow(0x00, 0x7F, 0x85, 0x08)]
        [DataRow(0x00, 0x81, 0x81, 0x08)]
        [DataRow(0x00, 0x99, 0x99, 0x08)]
        [DataRow(0x00, 0xA0, 0x00, 0x05)]
        [DataRow(0x00, 0xBF, 0x25, 0x01)]
        [DataRow(0x00, 0xF0, 0x50, 0x01)]
        [DataRow(0x00, 0xFF, 0x65, 0x01)]
        // H=0, C=1 (initialCC=0x01)
        [DataRow(0x01, 0x00, 0x60, 0x01)]
        [DataRow(0x01, 0x01, 0x61, 0x01)]
        [DataRow(0x01, 0x09, 0x69, 0x01)]
        [DataRow(0x01, 0x0A, 0x70, 0x01)]
        [DataRow(0x01, 0x0F, 0x75, 0x01)]
        [DataRow(0x01, 0x10, 0x70, 0x01)]
        [DataRow(0x01, 0x4A, 0xB0, 0x09)]
        [DataRow(0x01, 0x79, 0xD9, 0x09)]
        [DataRow(0x01, 0x7A, 0xE0, 0x09)]
        [DataRow(0x01, 0x7F, 0xE5, 0x09)]
        [DataRow(0x01, 0x81, 0xE1, 0x09)]
        [DataRow(0x01, 0x99, 0xF9, 0x09)]
        [DataRow(0x01, 0xA0, 0x00, 0x05)]
        [DataRow(0x01, 0xBF, 0x25, 0x01)]
        [DataRow(0x01, 0xF0, 0x50, 0x01)]
        [DataRow(0x01, 0xFF, 0x65, 0x01)]
        // H=1, C=0 (initialCC=0x20)
        [DataRow(0x20, 0x00, 0x06, 0x00)]
        [DataRow(0x20, 0x01, 0x07, 0x00)]
        [DataRow(0x20, 0x09, 0x0F, 0x00)]
        [DataRow(0x20, 0x0A, 0x10, 0x00)]
        [DataRow(0x20, 0x0F, 0x15, 0x00)]
        [DataRow(0x20, 0x10, 0x16, 0x00)]
        [DataRow(0x20, 0x4A, 0x50, 0x00)]
        [DataRow(0x20, 0x79, 0x7F, 0x00)]
        [DataRow(0x20, 0x7A, 0x80, 0x08)]
        [DataRow(0x20, 0x7F, 0x85, 0x08)]
        [DataRow(0x20, 0x81, 0x87, 0x08)]
        [DataRow(0x20, 0x99, 0x9F, 0x08)]
        [DataRow(0x20, 0xA0, 0x06, 0x01)]
        [DataRow(0x20, 0xBF, 0x25, 0x01)]
        [DataRow(0x20, 0xF0, 0x56, 0x01)]
        [DataRow(0x20, 0xFF, 0x65, 0x01)]
        // H=1, C=1 (initialCC=0x21)
        [DataRow(0x21, 0x00, 0x66, 0x01)]
        [DataRow(0x21, 0x01, 0x67, 0x01)]
        [DataRow(0x21, 0x09, 0x6F, 0x01)]
        [DataRow(0x21, 0x0A, 0x70, 0x01)]
        [DataRow(0x21, 0x0F, 0x75, 0x01)]
        [DataRow(0x21, 0x10, 0x76, 0x01)]
        [DataRow(0x21, 0x4A, 0xB0, 0x09)]
        [DataRow(0x21, 0x79, 0xDF, 0x09)]
        [DataRow(0x21, 0x7A, 0xE0, 0x09)]
        [DataRow(0x21, 0x7F, 0xE5, 0x09)]
        [DataRow(0x21, 0x81, 0xE7, 0x09)]
        [DataRow(0x21, 0x99, 0xFF, 0x09)]
        [DataRow(0x21, 0xA0, 0x06, 0x01)]
        [DataRow(0x21, 0xBF, 0x25, 0x01)]
        [DataRow(0x21, 0xF0, 0x56, 0x01)]
        [DataRow(0x21, 0xFF, 0x65, 0x01)]
        public void TestDAA(int initialCC, int input, int expected, int flags)
        {
            this.board.Poke(0, 0x19);
            this.cpu.CC = (byte)initialCC;
            this.cpu.A = (byte)input;
            this.cpu.Step();
            Assert.AreEqual((byte)expected, this.cpu.A);
            Assert.AreEqual((byte)flags, (byte)(this.cpu.CC & 0x0D));
        }
    }
}
