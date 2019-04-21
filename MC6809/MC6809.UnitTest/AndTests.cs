namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AndTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public AndTests()
        {
            this.cpu = this.board.CPU;

            this.board.Poke(0, 0x84);
            this.board.Poke(1, 0x13);
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
        public void TestImmediate()
        {
            this.cpu.A = 0xfc;
            this.cpu.Step();
            Assert.AreEqual(0x10, this.cpu.A);
            Assert.AreEqual(0, this.cpu.Zero);
            Assert.AreEqual(0, this.cpu.Overflow);
            Assert.AreEqual(0, this.cpu.Negative);
            Assert.AreEqual(2, this.cpu.Cycles);
        }
    }
}