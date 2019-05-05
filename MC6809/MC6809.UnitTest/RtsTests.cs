namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    // https://github.com/sorenroug/osnine-java/blob/master/core/src/test/java/org/roug/osnine/BranchAndJumpTest.java

    [TestClass]
    public class RtsTests
    {
        private readonly Board board = new Board();
        private readonly MC6809 cpu;

        public RtsTests()
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
        public void TestRts()
        {
            this.cpu.S.Word = 0x300;

            this.cpu.PokeWord(0x300, 0x102C); // Write return address
            this.board.Poke(0xB00, 0x39); // RTS

            this.cpu.PC.Word = 0xB00;

            this.cpu.Step();

            Assert.AreEqual(0x102C, this.cpu.PC.Word);
            Assert.AreEqual(0x302, this.cpu.S.Word);
            Assert.AreEqual(5, this.cpu.Cycles);
        }
    }
}