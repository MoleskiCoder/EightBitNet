// <copyright file="NmiTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NmiTests
    {
        private readonly Board board = new();
        private readonly Z80 cpu;

        public NmiTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.PC.Joined = 0x0100;
            this.cpu.SP.Joined = 0xFF00;
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        [TestMethod]
        public void Nmi_Jumps_To_0x0066()
        {
            this.cpu.LowerNMI();
            this.cpu.Step();
            Assert.AreEqual(0x0066, this.cpu.PC.Joined, "NMI must jump to 0x0066");
        }

        [TestMethod]
        public void Nmi_Saves_PC_On_Stack()
        {
            this.cpu.PC.Joined = 0x1234;
            this.cpu.LowerNMI();
            this.cpu.Step();

            // PushShort pushes High first then Low, so:
            //   mem[SP-1] = PC.High, mem[SP-2] = PC.Low
            var low = this.board.Peek(this.cpu.SP.Joined);          // mem[SP]   = PC.Low
            var high = this.board.Peek((ushort)(this.cpu.SP.Joined + 1)); // mem[SP+1] = PC.High
            var savedPc = (ushort)((high << 8) | low);
            Assert.AreEqual(0x1234, savedPc, "NMI must save PC on the stack");
        }

        [TestMethod]
        public void Nmi_Clears_IFF1()
        {
            this.cpu.IFF1 = true;
            this.cpu.LowerNMI();
            this.cpu.Step();
            Assert.IsFalse(this.cpu.IFF1, "NMI must clear IFF1");
        }

        [TestMethod]
        public void Nmi_Preserves_IFF1_In_IFF2()
        {
            this.cpu.IFF1 = true;
            this.cpu.IFF2 = false;
            this.cpu.LowerNMI();
            this.cpu.Step();
            Assert.IsTrue(this.cpu.IFF2, "NMI must copy IFF1 into IFF2 before clearing IFF1");
            Assert.IsFalse(this.cpu.IFF1, "NMI must clear IFF1");
        }

        [TestMethod]
        public void Nmi_Fires_Even_When_IFF1_Disabled()
        {
            this.cpu.IFF1 = false;
            this.cpu.LowerNMI();
            this.cpu.Step();
            Assert.AreEqual(0x0066, this.cpu.PC.Joined, "NMI must fire regardless of IFF1");
        }

        [TestMethod]
        public void Nmi_Exits_Halt_State()
        {
            this.board.Poke(0x0100, 0x76); // HALT
            this.cpu.Step(); // enter HALT
            Assert.IsTrue(this.cpu.HALT.Lowered(), "CPU must be halted before NMI test");

            this.cpu.LowerNMI();
            this.cpu.Step(); // NMI exits HALT
            Assert.IsTrue(this.cpu.HALT.Raised(), "NMI must raise HALT pin");
            Assert.AreEqual(0x0066, this.cpu.PC.Joined, "NMI must jump to 0x0066 from HALT state");
        }

        [TestMethod]
        public void Retn_Restores_IFF1_From_IFF2()
        {
            // RETN (ED 45) at 0x0066
            this.board.Poke(0x0066, 0xED);
            this.board.Poke(0x0067, 0x45);

            this.cpu.IFF1 = true;
            this.cpu.LowerNMI();
            this.cpu.Step(); // handles NMI: IFF2=true, IFF1=false, jumps to 0x0066

            Assert.IsFalse(this.cpu.IFF1, "IFF1 must be false after NMI");
            Assert.IsTrue(this.cpu.IFF2, "IFF2 must be true after NMI");

            this.cpu.Step(); // executes RETN at 0x0066: IFF1 = IFF2 = true
            Assert.IsTrue(this.cpu.IFF1, "RETN must restore IFF1 from IFF2");
        }
    }
}
