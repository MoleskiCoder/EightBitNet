// <copyright file="IffTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IffTests
    {
        private readonly Board board = new();
        private readonly Z80 cpu;

        public IffTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.PC.Joined = 0x0000;
            this.cpu.SP.Joined = 0xFF00;
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void DI_Clears_IFF1_And_IFF2()
        {
            this.cpu.IFF1 = true;
            this.cpu.IFF2 = true;
            this.board.Poke(0x0000, 0xF3); // DI
            this.cpu.Step();
            Assert.IsFalse(this.cpu.IFF1, "DI must clear IFF1");
            Assert.IsFalse(this.cpu.IFF2, "DI must clear IFF2");
        }

        [TestMethod]
        public void EI_Sets_IFF1_And_IFF2()
        {
            this.cpu.IFF1 = false;
            this.cpu.IFF2 = false;
            this.board.Poke(0x0000, 0xFB); // EI
            this.cpu.Step();
            Assert.IsTrue(this.cpu.IFF1, "EI must set IFF1");
            Assert.IsTrue(this.cpu.IFF2, "EI must set IFF2");
        }

        [TestMethod]
        public void DI_Prevents_INT()
        {
            this.board.Poke(0x0000, 0x00); // NOP
            this.cpu.IFF1 = false;
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step(); // pending cleared (IFF1=false), then NOP executes
            Assert.AreEqual(0x0001, this.cpu.PC.Joined, "INT must be blocked when IFF1 is clear");
        }

        [TestMethod]
        public void EI_Followed_By_INT_Fires_On_Next_LowerINT()
        {
            // After EI, the previous INT pending was already consumed without firing
            // (because IFF1 was false when it was checked). INT must produce a new
            // falling edge to retrigger. The INT pin is still low, so raise it first.
            this.board.Poke(0x0000, 0xFB); // EI
            this.board.Poke(0x0001, 0x00); // NOP
            this.cpu.IFF1 = false;
            this.cpu.IM = 1;

            this.cpu.LowerINT(); // pending=true, but IFF1=false
            this.cpu.Step();     // pending cleared (IFF1=false), EI runs → IFF1=true
            Assert.IsTrue(this.cpu.IFF1, "IFF1 must be true after EI");
            Assert.AreEqual(0x0001, this.cpu.PC.Joined, "No interrupt must fire during EI step");

            this.cpu.Step();     // NOP at 0x0001 — still no interrupt (pending was cleared)
            Assert.AreEqual(0x0002, this.cpu.PC.Joined, "No interrupt must fire on instruction after EI");

            // Produce a new falling edge on INT (raise then lower) to retrigger
            this.cpu.RaiseINT();
            this.cpu.LowerINT();
            this.cpu.Step();
            Assert.AreEqual(0x0038, this.cpu.PC.Joined, "INT must fire after new falling edge post-EI");
        }

        [TestMethod]
        public void Im_Set_Mode_0()
        {
            this.cpu.IM = 2;
            this.board.Poke(0x0000, 0xED); // IM 0 prefix
            this.board.Poke(0x0001, 0x46); // IM 0 (ED 46)
            this.cpu.Step();
            Assert.AreEqual(0, this.cpu.IM, "IM 0 instruction must set interrupt mode to 0");
        }

        [TestMethod]
        public void Im_Set_Mode_1()
        {
            this.cpu.IM = 0;
            this.board.Poke(0x0000, 0xED); // IM 1 prefix
            this.board.Poke(0x0001, 0x56); // IM 1 (ED 56)
            this.cpu.Step();
            Assert.AreEqual(1, this.cpu.IM, "IM 1 instruction must set interrupt mode to 1");
        }

        [TestMethod]
        public void Im_Set_Mode_2()
        {
            this.cpu.IM = 0;
            this.board.Poke(0x0000, 0xED); // IM 2 prefix
            this.board.Poke(0x0001, 0x5E); // IM 2 (ED 5E)
            this.cpu.Step();
            Assert.AreEqual(2, this.cpu.IM, "IM 2 instruction must set interrupt mode to 2");
        }
    }
}
