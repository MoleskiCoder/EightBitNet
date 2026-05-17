// <copyright file="IntTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Z80.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IntTests
    {
        private readonly Board board = new();
        private readonly Z80 cpu;

        public IntTests() => this.cpu = this.board.CPU;

        [TestInitialize]
        public void Initialise()
        {
            this.board.RaisePOWER();
            this.cpu.PC.Joined = 0x0200;
            this.cpu.SP.Joined = 0xFF00;
            this.cpu.IFF1 = true;
        }

        [TestCleanup]
        public void Cleanup() => this.board.LowerPOWER();

        // ── IM 1 ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Int_IM1_Jumps_To_0x0038()
        {
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step();
            Assert.AreEqual(0x0038, this.cpu.PC.Joined, "IM 1 INT must jump to 0x0038");
        }

        [TestMethod]
        public void Int_IM1_Saves_PC_On_Stack()
        {
            this.cpu.PC.Joined = 0x1234;
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step();

            var low = this.board.Peek(this.cpu.SP.Joined);
            var high = this.board.Peek((ushort)(this.cpu.SP.Joined + 1));
            var savedPc = (ushort)((high << 8) | low);
            Assert.AreEqual(0x1234, savedPc, "IM 1 INT must save PC on the stack");
        }

        [TestMethod]
        public void Int_IM1_Clears_IFF1_And_IFF2()
        {
            this.cpu.IFF2 = true;
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step();
            Assert.IsFalse(this.cpu.IFF1, "INT must clear IFF1");
            Assert.IsFalse(this.cpu.IFF2, "INT must clear IFF2");
        }

        [TestMethod]
        public void Int_Blocked_When_IFF1_Disabled()
        {
            this.board.Poke(0x0200, 0x00); // NOP
            this.cpu.IFF1 = false;
            this.cpu.IM = 1;
            this.cpu.LowerINT(); // INT pending, but IFF1 = false
            this.cpu.Step();
            Assert.AreEqual(0x0201, this.cpu.PC.Joined, "INT must not fire when IFF1 is disabled");
        }

        // ── IM 2 ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Int_IM2_Jumps_Via_Vector_Table()
        {
            // Vector table: address = (IV << 8) | bus_data_byte
            // IV = 0x10, bus byte = 0x20  →  table entry at 0x1020
            // Table entry (little-endian) = 0x5678
            this.cpu.IV = 0x10;
            this.board.Poke(0x1020, 0x78);
            this.board.Poke(0x1021, 0x56);

            this.cpu.IM = 2;
            this.board.Data = 0x20; // interrupt device places vector byte on data bus
            this.cpu.LowerINT();
            this.cpu.Step();
            Assert.AreEqual(0x5678, this.cpu.PC.Joined, "IM 2 INT must read jump address from vector table");
        }

        [TestMethod]
        public void Int_IM2_Saves_PC_On_Stack()
        {
            this.cpu.PC.Joined = 0xABCD;
            this.cpu.IV = 0x10;
            this.board.Poke(0x1020, 0x78);
            this.board.Poke(0x1021, 0x56);

            this.cpu.IM = 2;
            this.board.Data = 0x20;
            this.cpu.LowerINT();
            this.cpu.Step();

            var low = this.board.Peek(this.cpu.SP.Joined);
            var high = this.board.Peek((ushort)(this.cpu.SP.Joined + 1));
            var savedPc = (ushort)((high << 8) | low);
            Assert.AreEqual(0xABCD, savedPc, "IM 2 INT must save PC on the stack");
        }

        // ── IM 0 ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Int_IM0_Executes_Instruction_From_Data_Bus()
        {
            // RST 7 (0xFF) placed on data bus by interrupt device → jumps to 0x0038.
            // In practice IM 0 is always used with RST n (single-byte). The Zilog
            // spec technically allows multi-byte instructions inherited from the 8080,
            // but no real Z80 peripheral (PIO, SIO, CTC) ever used that capability.
            this.cpu.PC.Joined = 0x0200;
            this.cpu.IM = 0;
            this.board.Data = 0xFF; // RST 7
            this.cpu.LowerINT();
            this.cpu.Step();
            Assert.AreEqual(0x0038, this.cpu.PC.Joined, "IM 0 INT must execute RST 7 from data bus");
        }

        // ── RETI ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Reti_Restores_PC_From_Stack()
        {
            // RETI (ED 4D) at 0x0038 — return to saved PC
            this.board.Poke(0x0038, 0xED);
            this.board.Poke(0x0039, 0x4D);

            this.cpu.PC.Joined = 0x0200;
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step(); // handles INT, jumps to 0x0038, saves 0x0200

            Assert.AreEqual(0x0038, this.cpu.PC.Joined);

            this.cpu.Step(); // executes RETI
            Assert.AreEqual(0x0200, this.cpu.PC.Joined, "RETI must restore PC from stack");
        }

        [TestMethod]
        public void Reti_Restores_IFF1_From_IFF2()
        {
            // On real Z80 silicon RETI and RETN are electrically identical — both
            // copy IFF2 → IFF1. The Zilog manual omits this for RETI (describing
            // only its peripheral daisy-chain signalling role), but hardware
            // measurement confirms the behaviour. RetI() is implemented as RetN().
            this.board.Poke(0x0038, 0xED);
            this.board.Poke(0x0039, 0x4D); // RETI

            this.cpu.IFF2 = true; // pre-load IFF2 with the state to restore
            this.cpu.IM = 1;
            this.cpu.LowerINT();
            this.cpu.Step(); // INT clears IFF1 and IFF2, jumps to 0x0038

            Assert.IsFalse(this.cpu.IFF1, "INT must clear IFF1 before RETI");
            Assert.IsFalse(this.cpu.IFF2, "INT must clear IFF2 before RETI");

            // Manually set IFF2 to simulate what a real ISR would have saved
            this.cpu.IFF2 = true;

            this.cpu.Step(); // executes RETI — must copy IFF2 → IFF1
            Assert.IsTrue(this.cpu.IFF1, "RETI must copy IFF2 into IFF1");
        }
    }
}
