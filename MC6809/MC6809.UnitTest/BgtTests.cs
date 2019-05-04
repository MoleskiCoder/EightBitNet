namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BgtTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public BgtTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x2e);    // BGT
            this.board.Poke(1, 0x03);
            this.board.Poke(2, 0x86);    // LDA	#1
            this.board.Poke(3, 0x01);
            this.board.Poke(4, 0x12);    // NOP
            this.board.Poke(5, 0x86);    // LDA	#2
            this.board.Poke(6, 0x02);
            this.board.Poke(7, 0x12);    // NOP
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
        public void TestBGT1()
        {
            this.cpu.A = 0;
            this.cpu.CC = (byte)StatusBits.ZF;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }

        [TestMethod]
        public void TestBGT2()
        {
            Assert.AreEqual(0, this.cpu.PC.Word);
            this.cpu.CC = 0;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
        }

        [TestMethod]
        public void TestBGT3()
        {
            Assert.AreEqual(0, this.cpu.PC.Word);
            this.cpu.CC = (byte)StatusBits.NF;
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }

        [TestMethod]
        public void TestBGT4()
        {
            Assert.AreEqual(0, this.cpu.PC.Word);
            this.cpu.CC = (byte)(StatusBits.NF | StatusBits.VF);
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.A);
        }

        [TestMethod]
        public void TestBGT5()
        {
            Assert.AreEqual(0, this.cpu.PC.Word);
            this.cpu.CC = (byte)(StatusBits.ZF | StatusBits.NF);
            this.cpu.Step();
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.A);
        }
    }
}