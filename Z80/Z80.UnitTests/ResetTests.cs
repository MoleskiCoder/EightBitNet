// <copyright file="ResetTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ResetTests
    {
        private readonly Board board = new();
        private readonly Z80 cpu;

        public ResetTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.PC.Joined = 0x1234;
            this.cpu.SP.Joined = 0x8000;
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void Reset_Sets_PC_To_Zero()
        {
            this.cpu.LowerRESET();
            this.cpu.Step();
            Assert.AreEqual(0x0000, this.cpu.PC.Joined, "RESET must set PC to 0x0000");
        }

        [TestMethod]
        public void Reset_Clears_IFF1_And_IFF2()
        {
            this.cpu.IFF1 = true;
            this.cpu.IFF2 = true;
            this.cpu.LowerRESET();
            this.cpu.Step();
            Assert.IsFalse(this.cpu.IFF1, "RESET must clear IFF1");
            Assert.IsFalse(this.cpu.IFF2, "RESET must clear IFF2");
        }

        [TestMethod]
        public void Reset_Sets_IM_To_Zero()
        {
            this.cpu.IM = 2;
            this.cpu.LowerRESET();
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.IM, "RESET must set interrupt mode to 0");
        }

        [TestMethod]
        public void Reset_Clears_I_And_R_Registers()
        {
            this.cpu.IV = 0xAB;
            this.cpu.REFRESH = 0xCD;
            this.cpu.LowerRESET();
            this.cpu.Step();
            Assert.AreEqual(0x00, this.cpu.IV, "RESET must clear I register");
            Assert.AreEqual(0x00, (byte)this.cpu.REFRESH, "RESET must clear R register");
        }

        [TestMethod]
        public void Reset_Deasserts_Reset_Pin()
        {
            this.cpu.LowerRESET(); // asserts RESET — fires LoweredRESET event
            Assert.IsTrue(this.cpu.RESET.Lowered(), "RESET pin must be asserted before step");
            this.cpu.Step();
            Assert.IsTrue(this.cpu.RESET.Raised(), "HandleRESET must deassert the RESET pin");
        }

        [TestMethod]
        public void Reset_Has_Priority_Over_NMI()
        {
            // Assert both RESET and NMI simultaneously; RESET is checked first in PoweredStep
            this.cpu.LowerRESET();
            this.cpu.LowerNMI();
            this.cpu.Step();
            // RESET handler runs, sets PC=0 — NMI (0x0066) must not win
            Assert.AreEqual(0x0000, this.cpu.PC.Joined, "RESET must take priority over pending NMI");
        }
    }
}
