namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SubTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public SubTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.Step(); // Step over the reset
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // Test the SUBA instruction.
        // The overflow (V) bit indicates signed two’s complement overflow, which
        // occurs when the sign bit differs from the carry bit after an arithmetic
        // operation.
        // A=0x00 - 0xFF becomes 0x01
        // positive - negative = positive
        [TestMethod]
        public void TestImmediateSUBASUBA1()
        {
            this.board.Poke(0, 0x80);
            this.board.Poke(1, 0xff);
            this.cpu.CC = (byte)(StatusBits.CF | StatusBits.NF);
            this.cpu.A = 0;
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Test the SUBA instruction.
        // The overflow (V) bit indicates signed two’s complement overflow, which
        // occurs when the sign bit differs from the carry bit after an arithmetic
        // operation.
        // A=0x00 - 0xFF becomes 0x01
        // positive - negative = positive
        [TestMethod]
        public void TestImmediateSUBASUBA2()
        {
            this.board.Poke(0, 0x80);
            this.board.Poke(1, 1);
            this.cpu.CC = (byte)(StatusBits.CF | StatusBits.NF);
            this.cpu.A = 0;
            this.cpu.Step();
            Assert.AreEqual(0xff, this.cpu.A);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Test the subtraction instruction.
        // IMMEDIATE mode:   B=0x02 - 0xB3  becomes 0x4F
        // positive - negative = positive
        [TestMethod]
        public void TestImmediateSUBBSUBB1()
        {
            this.board.Poke(0, 0xc0);
            this.board.Poke(1, 0xb3);
            this.cpu.CC = 0;
            this.cpu.B = 2;
            this.cpu.Step();
            Assert.AreEqual(0x4f, this.cpu.B);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Test the subtraction instruction.
        // IMMEDIATE mode:   B=0x02 - 0x81  becomes 0x81
        // positive - negative = negative + overflow
        [TestMethod]
        public void TestImmediateSUBBSUBB2()
        {
            this.board.Poke(0, 0xc0);
            this.board.Poke(1, 0x81);
            this.cpu.CC = 0;
            this.cpu.B = 2;
            this.cpu.Step();
            Assert.AreEqual(0x81, this.cpu.B);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }

        // Example from Programming the 6809.
        // 0x03 - 0x21 = 0xE2
        // positive - positive = negative
        [TestMethod]
        public void TestImmediateSUBBSUBBY()
        {
            this.board.Poke(0, 0xe0);
            this.board.Poke(1, 0xa4);
            this.board.Poke(0x21, 0x21);
            this.cpu.CC = (byte)StatusBits.ZF;
            this.cpu.B = 3;
            this.cpu.Y.Word = 0x21;
            this.cpu.Step();
            Assert.AreEqual(0xe2, this.cpu.B);
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreEqual(4, this.cpu.Cycles);
        }
    }
}