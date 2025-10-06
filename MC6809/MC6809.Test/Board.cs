// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace MC6809.Test
{
    using EightBit;
    using System;

    public sealed class Board : Bus
    {
        private readonly Configuration configuration;
        private readonly Ram ram = new(0x8000);                                 // 0000 - 7FFF, 32K RAM
        private readonly UnusedMemory unused2000 = new(0x2000, 0xff);  // 8000 - 9FFF, 8K unused
        private readonly Ram io = new(0x2000);                                  // A000 - BFFF, 8K serial interface, minimally decoded
        private readonly Rom rom = new(0x4000);                                 // C000 - FFFF, 16K ROM

        private readonly Disassembler disassembler;
        private readonly Profiler profiler;

        private ulong totalCycleCount = 0UL;
        private long frameCycleCount = 0L;

        // The m_disassembleAt and m_ignoreDisassembly are used to skip pin events
        private ushort disassembleAt = 0x0000;
        private bool ignoreDisassembly = false;

        public Board(Configuration configuration)
        {
            this.configuration = configuration;
            CPU = new MC6809(this);
            disassembler = new Disassembler(this, CPU);
            profiler = new Profiler(this, CPU, disassembler);
        }

        public MC6809 CPU { get; }

        public MC6850 ACIA { get; } = new MC6850();

        public override MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x8000)
            {
                return new MemoryMapping(ram, 0x0000, Mask.Sixteen, AccessLevel.ReadWrite);
            }

            if (absolute < 0xa000)
            {
                return new MemoryMapping(unused2000, 0x8000, Mask.Sixteen, AccessLevel.ReadOnly);
            }

            if (absolute < 0xc000)
            {
                return new MemoryMapping(io, 0xa000, Mask.Sixteen, AccessLevel.ReadWrite);
            }

            return new MemoryMapping(rom, 0xc000, Mask.Sixteen, AccessLevel.ReadOnly);
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            // Get the CPU ready for action
            CPU.RaisePOWER();
            CPU.LowerRESET();
            CPU.RaiseINT();
            CPU.RaiseNMI();
            CPU.RaiseFIRQ();
            CPU.RaiseHALT();

            // Get the ACIA ready for action
            Address.Word = 0b1010000000000000;
            Data = (byte)(MC6850.ControlRegister.CR0 | MC6850.ControlRegister.CR1);  // Master reset
            ACIA.CTS.Lower();
            ACIA.RW.Lower();
            UpdateAciaPins();
            ACIA.RaisePOWER();
            AccessAcia();
        }

        public override void LowerPOWER()
        {
            ////this.profiler.Generate();

            ACIA.LowerPOWER();
            CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            Console.TreatControlCAsInput = true;

            // Load our BASIC interpreter
            var directory = configuration.RomDirectory + "\\";
            LoadHexFile(directory + "ExBasROM.hex");

            // Catch a byte being transmitted
            ACIA.Transmitting += ACIA_Transmitting;

            // Keyboard wiring, check for input once per frame
            CPU.ExecutedInstruction += CPU_ExecutedInstruction;

            // Marshal data from ACIA -> memory
            this.ReadingByte += Board_ReadingByte;

            // Marshal data from memory -> ACIA
            this.WrittenByte += Board_WrittenByte;

            if (configuration.DebugMode)
            {
                // MC6809 disassembly wiring
                CPU.ExecutingInstruction += CPU_ExecutingInstruction;
                CPU.ExecutedInstruction += CPU_ExecutedInstruction_Debug;
            }

            if (configuration.TerminatesEarly)
            {
                // Early termination condition for CPU timing code
                CPU.ExecutedInstruction += CPU_ExecutedInstruction_Termination;
            }

            ////this.profiler.Enable();
            ////this.profiler.EmitLine += this.Profiler_EmitLine;
        }

        private void Board_ReadingByte(object? sender, EventArgs e)
        {
            UpdateAciaPins();
            ACIA.RW.Raise();
            if (AccessAcia())
            {
                Poke(ACIA.DATA);
            }
        }

        private void Board_WrittenByte(object? sender, EventArgs e)
        {
            UpdateAciaPins();
            if (ACIA.Selected)
            {
                ACIA.RW.Lower();
                AccessAcia();
            }
        }

        private void Profiler_EmitLine(object? sender, ProfileLineEventArgs e)
        {
            var cycles = e.Cycles;
            var disassembled = e.Source;
            Console.Error.WriteLine(disassembled);
        }

        private void CPU_ExecutedInstruction_Termination(object? sender, EventArgs e)
        {
            totalCycleCount += (ulong)CPU.Cycles;
            if (totalCycleCount > Configuration.TerminationCycles)
            {
                LowerPOWER();
            }
        }

        private void CPU_ExecutedInstruction_Debug(object? sender, EventArgs e)
        {
            if (!ignoreDisassembly)
            {
                var disassembled = $"{disassembler.Trace(disassembleAt)}\t{ACIA.DumpStatus()}";
                Console.Error.WriteLine(disassembled);
            }
        }

        private void CPU_ExecutingInstruction(object? sender, EventArgs e)
        {
            disassembleAt = CPU.PC.Word;
            ignoreDisassembly = disassembler.Ignore || disassembler.Pause;
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e)
        {
            frameCycleCount -= CPU.Cycles;
            if (frameCycleCount < 0)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.F12)
                    {
                        LowerPOWER();
                    }

                    ACIA.RDR = Convert.ToByte(key.KeyChar);
                    ACIA.MarkReceiveStarting();
                }

                frameCycleCount = (long)Configuration.FrameCycleInterval;
            }
        }

        private void ACIA_Transmitting(object? sender, EventArgs e)
        {
            Console.Out.Write(Convert.ToChar(ACIA.TDR));
            ACIA.MarkTransmitComplete();
        }

        private void UpdateAciaPins()
        {
            ACIA.DATA = Data;
            ACIA.RS.Match(Address.Word & (ushort)Bits.Bit0);
            ACIA.CS0.Match(Address.Word & (ushort)Bits.Bit15);
            ACIA.CS1.Match(Address.Word & (ushort)Bits.Bit13);
            ACIA.CS2.Match(Address.Word & (ushort)Bits.Bit14);
        }

        private bool AccessAcia()
        {
            ACIA.E.Raise();
            try
            {
                ACIA.Tick();
                return ACIA.Activated;
            }
            finally
            {
                ACIA.E.Lower();
            }
        }
    }
}