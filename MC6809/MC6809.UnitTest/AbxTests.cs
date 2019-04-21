namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AbxTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AbxTests()
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

        [TestMethod]
        public void TestInherent()
        {
            this.cpu.B = 0x84;
            this.cpu.X.Word = 0x1097;
            this.cpu.Step();
            Assert.AreEqual(0x111b, this.cpu.X.Word);
            Assert.AreEqual(3, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestInherentABX1()
        {
            this.cpu.A = 0;
            this.cpu.B = 0xce;
            this.cpu.X.Word = 0x8006;
            this.cpu.Y.Word = 0;
            this.cpu.U.Word = 0;
            this.cpu.CC = 0;
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0xce, this.cpu.B);
            Assert.AreEqual(0x80d4, this.cpu.X.Word);
            Assert.AreEqual(0, this.cpu.Y.Word);
            Assert.AreEqual(0, this.cpu.U.Word);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(3, this.cpu.Cycles);
        }

        [TestMethod]
        public void TestInherentABX2()
        {
            this.cpu.A = 0;
            this.cpu.B = 0xd6;
            this.cpu.X.Word = 0x7ffe;
            this.cpu.Y.Word = 0;
            this.cpu.U.Word = 0;
            this.cpu.CC = (byte)(StatusBits.CF | StatusBits.VF | StatusBits.ZF);
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.A);
            Assert.AreEqual(0xd6, this.cpu.B);
            Assert.AreEqual(0x80d4, this.cpu.X.Word);
            Assert.AreEqual(0, this.cpu.Y.Word);
            Assert.AreEqual(0, this.cpu.U.Word);
            Assert.AreNotEqual(0, this.cpu.Carry);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.HalfCarry);
            Assert.AreEqual(3, this.cpu.Cycles);
        }
    }
}
