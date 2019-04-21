namespace EightBit
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
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Negative);
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
            Assert.AreNotEqual(0, this.cpu.Negative);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreNotEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
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
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreNotEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Carry);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}
