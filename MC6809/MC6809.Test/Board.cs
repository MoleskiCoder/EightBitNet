namespace EightBit
{
    using System;

    public sealed class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram = new Ram(0x8000);                                 // 0000 - 7FFF, 32K RAM
        private readonly UnusedMemory unused2000 = new UnusedMemory(0x2000, 0xff);  // 8000 - 9FFF, 8K unused
        private readonly Ram io = new Ram(0x2000);                                  // A000 - BFFF, 8K serial interface, minimally decoded
        private readonly Rom rom = new Rom(0x4000);                                 // C000 - FFFF, 16K ROM

        private readonly Disassembler disassembler;

        private ulong totalCycleCount = 0UL;
        private long frameCycleCount = 0L;

        // The m_disassembleAt and m_ignoreDisassembly are used to skip pin events
        private ushort disassembleAt = 0x0000;
        private bool ignoreDisassembly = false;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            this.CPU = new MC6809(this);
            this.disassembler = new Disassembler(this, this.CPU);
        }

        public MC6809 CPU { get; }
        public MC6850 ACIA { get; } = new MC6850();

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            // Get the CPU ready for action
            this.CPU.RaisePOWER();
            this.CPU.LowerRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
            this.CPU.RaiseFIRQ();
            this.CPU.RaiseHALT();

            // Get the ACIA ready for action
            this.Address.Word = 0b1010000000000000;
            this.ACIA.DATA = (byte)(MC6850.ControlRegister.CR0 | MC6850.ControlRegister.CR1);  // Master reset
            this.UpdateAciaPinsWrite();
            this.ACIA.CTS.Lower();
            this.ACIA.RaisePOWER();
            this.AccessAcia();
        }

        public override void LowerPOWER()
        {
            this.ACIA.LowerPOWER();
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            // Load our BASIC interpreter
            var directory = this.configuration.RomDirectory + "\\";
            this.LoadHexFile(directory + "ExBasROM.hex");

            // Catch a byte being transmitted
            this.ACIA.Transmitting += this.ACIA_Transmitting;

            // Marshal data from memory -> ACIA
            this.WrittenByte += this.Board_WrittenByte;

            // Marshal data from ACIA -> memory
            this.ReadingByte += this.Board_ReadingByte;

            // Keyboard wiring, check for input once per frame
            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;

            if (this.configuration.DebugMode)
            {
                // MC6809 disassembly wiring
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction;
                this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction_Debug;
            }

            if (this.configuration.TerminatesEarly)
            {
                // Early termination condition for CPU timing code
                this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction_Termination;
            }
        }

        private void CPU_ExecutedInstruction_Termination(object sender, EventArgs e)
        {
            this.totalCycleCount += (ulong)this.CPU.Cycles;
            if (this.totalCycleCount > Configuration.TerminationCycles)
            {
                this.LowerPOWER();
            }
        }

        private void CPU_ExecutedInstruction_Debug(object sender, EventArgs e)
        {
            if (this.ignoreDisassembly)
            {
                var disassembled = $"{this.disassembler.Trace(this.disassembleAt)}\t{this.ACIA.DumpStatus()}";
                System.Console.Out.WriteLine(disassembled);
            }
        }

        private void CPU_ExecutingInstruction(object sender, EventArgs e)
        {
            this.disassembleAt = this.CPU.PC.Word;
            this.ignoreDisassembly = this.disassembler.Ignore;
        }

        private void CPU_ExecutedInstruction(object sender, EventArgs e)
        {
            this.frameCycleCount -= this.CPU.Cycles;
            if (this.frameCycleCount < 0)
            {
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey();
                    this.ACIA.RDR = System.Convert.ToByte(key.KeyChar);
                    this.ACIA.MarkReceiveStarting();
                }

                this.frameCycleCount = (long)Configuration.FrameCycleInterval;
            }
        }

        private void Board_ReadingByte(object sender, EventArgs e)
        {
            this.UpdateAciaPinsRead();
            if (this.AccessAcia())
            {
                this.Poke(this.ACIA.DATA);
            }
        }

        private void Board_WrittenByte(object sender, EventArgs e)
        {
            this.UpdateAciaPinsWrite();
            if (this.ACIA.Selected)
            {
                this.ACIA.DATA = this.Data;
                this.AccessAcia();
            }
        }

        private void ACIA_Transmitting(object sender, EventArgs e)
        {
            System.Console.Out.Write(Convert.ToChar(this.ACIA.TDR));
            this.ACIA.MarkTransmitComplete();
        }

        public override MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x8000)
            {
                return new MemoryMapping(this.ram, 0x0000, Mask.Mask16, AccessLevel.ReadWrite);
            }

            if (absolute < 0xa000)
            {
                return new MemoryMapping(this.unused2000, 0x8000, Mask.Mask16, AccessLevel.ReadOnly);
            }

            if (absolute < 0xc000)
            {
                return new MemoryMapping(this.io, 0xa000, Mask.Mask16, AccessLevel.ReadWrite);
            }

            return new MemoryMapping(this.rom, 0xc000, Mask.Mask16, AccessLevel.ReadOnly);
        }

        // Use the bus data to update the ACIA access/address pins
        private void UpdateAciaPinsRead()
        {
            this.ACIA.RW.Raise();
            this.UpdateAciaPins();
        }

        private void UpdateAciaPinsWrite()
        {
            this.ACIA.RW.Lower();
            this.UpdateAciaPins();
        }

        private void UpdateAciaPins()
        {
            this.ACIA.DATA = this.Data;

            if ((this.Address.Word & (ushort)Bits.Bit0) != 0)
            {
                this.ACIA.RS.Raise();
            }
            else
            {
                this.ACIA.RS.Lower();
            }

            if ((this.Address.Word & (ushort)Bits.Bit15) != 0)
            {
                this.ACIA.CS0.Raise();
            }
            else
            {
                this.ACIA.CS0.Lower();
            }

            if ((this.Address.Word & (ushort)Bits.Bit13) != 0)
            {
                this.ACIA.CS1.Raise();
            }
            else
            {
                this.ACIA.CS1.Lower();
            }

            if ((this.Address.Word & (ushort)Bits.Bit14) != 0)
            {
                this.ACIA.CS2.Raise();
            }
            else
            {
                this.ACIA.CS2.Lower();
            }
        }

        private bool AccessAcia()
        {
            this.ACIA.E.Raise();
            this.ACIA.Tick();
            this.ACIA.E.Lower();
            return this.ACIA.Activated;
        }
    }
}