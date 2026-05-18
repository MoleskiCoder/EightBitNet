// <copyright file="HaltTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HaltTests
    {
        private readonly Board board = new();
        private readonly Z80 cpu;

        public HaltTests() => this.cpu = this.board.CPU;

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
        public void Halt_Instruction_Lowers_Halt_Pin()
        {
            this.board.Poke(0x0000, 0x76); // HALT
            Assert.IsTrue(this.cpu.HALT.Raised(), "HALT pin should be inactive before HALT instruction");
            this.cpu.Step();
            Assert.IsTrue(this.cpu.HALT.Lowered(), "HALT pin should be active after HALT instruction");
        }

        [TestMethod]
        public void Halt_State_Does_Not_Advance_PC()
        {
            this.board.Poke(0x0000, 0x76); // HALT
            this.board.Poke(0x0001, 0x00); // NOP (should not execute while halted)
            this.cpu.Step(); // execute HALT — PC advances to 0x0001
            var pcAfterHalt = this.cpu.PC.Joined;
            this.cpu.Step(); // CPU is halted — PC must not advance
            Assert.AreEqual(pcAfterHalt, this.cpu.PC.Joined, "PC must not advance while CPU is halted");
        }

        [TestMethod]
        public void Halt_State_Forces_Nop_Regardless_Of_Memory_Contents()
        {
            // The Z80 manual states: "the data received from the memory is ignored
            // and an NOP instruction is forced internally to the CPU."
            // INC A (0x3C) at the looping address must not execute — A must be unchanged.
            this.board.Poke(0x0000, 0x76); // HALT
            this.board.Poke(0x0001, 0x3C); // INC A — must be suppressed while halted
            this.cpu.A = 0x42;
            this.cpu.Step(); // execute HALT — enters halt state, PC moves to 0x0001
            this.cpu.Step(); // looping — memory byte (INC A) must be ignored, NOP forced
            Assert.AreEqual(0x42, this.cpu.A, "Memory contents must be ignored during HALT; NOP must be forced");
        }

        [TestMethod]
        public void Halt_Nmi_Raises_Halt_Pin()
        {
            this.board.Poke(0x0000, 0x76); // HALT
            this.cpu.Step(); // enter HALT
            Assert.IsTrue(this.cpu.HALT.Lowered(), "CPU must be halted before NMI test");

            this.cpu.LowerNMI();
            this.cpu.Step(); // NMI exits HALT and jumps to 0x0066
            Assert.IsTrue(this.cpu.HALT.Raised(), "HALT pin must be raised after NMI");
        }

        [TestMethod]
        public void Halt_Int_Raises_Halt_Pin_When_IFF1_Enabled()
        {
            this.board.Poke(0x0000, 0x76); // HALT
            this.cpu.IFF1 = true;
            this.cpu.IM = 1;
            this.cpu.Step(); // enter HALT
            Assert.IsTrue(this.cpu.HALT.Lowered(), "CPU must be halted before INT test");

            this.cpu.LowerINT();
            this.cpu.Step(); // INT exits HALT (IM1 → jumps to 0x0038)
            Assert.IsTrue(this.cpu.HALT.Raised(), "HALT pin must be raised after INT with IFF1 enabled");
        }

        [TestMethod]
        public void Halt_Int_Does_Not_Exit_When_IFF1_Disabled()
        {
            this.board.Poke(0x0000, 0x76); // HALT
            this.cpu.IFF1 = false;
            this.cpu.Step(); // enter HALT
            var pcAfterHalt = this.cpu.PC.Joined;
            Assert.IsTrue(this.cpu.HALT.Lowered(), "CPU must be halted before INT test");

            this.cpu.LowerINT(); // INT pending, but IFF1=false so it will be dismissed
            this.cpu.Step();
            Assert.IsTrue(this.cpu.HALT.Lowered(), "CPU must remain halted when IFF1 is disabled");
            Assert.AreEqual(pcAfterHalt, this.cpu.PC.Joined, "PC must not change when INT is masked");
        }
    }
}
