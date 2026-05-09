// <copyright file="Board.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace MC6809.Test
{
    using EightBit;
    using System;

    internal sealed class Board : Bus
    {
        private readonly Configuration _configuration;
        private readonly Ram _ram = new(0x8000);                        // 0000 - 7FFF, 32K RAM
        private readonly UnusedMemory _unused2000 = new(0x2000, 0xff);  // 8000 - 9FFF, 8K unused
        private readonly Ram _io = new(0x2000);                         // A000 - BFFF, 8K serial interface, minimally decoded
        private readonly Rom _rom = new(0x4000);                        // C000 - FFFF, 16K ROM

        private readonly MemoryMapping _ramMapping;
        private readonly MemoryMapping _unusedMapping;
        private readonly MemoryMapping _ioMapping;
        private readonly MemoryMapping _romMapping;

        private readonly Disassembler _disassembler;
        private readonly Profiler _profiler;

        private ulong _totalCycleCount;
        private long _frameCycleCount;

        // The _disassembleAt and _ignoreDisassembly are used to skip pin events
        private ushort _disassembleAt;
        private bool _ignoreDisassembly;

        public Board(Configuration configuration)
        {
            this._configuration = configuration;
            this.CPU = new MC6809(this);
            this._disassembler = new Disassembler(this, this.CPU);
            this._profiler = new Profiler(this, this.CPU, this._disassembler);
            this._ramMapping = new MemoryMapping(this._ram, 0x0000, Mask.Sixteen, AccessLevel.ReadWrite);
            this._unusedMapping = new MemoryMapping(this._unused2000, 0x8000, Mask.Sixteen, AccessLevel.ReadOnly);
            this._ioMapping = new MemoryMapping(this._io, 0xa000, Mask.Sixteen, AccessLevel.ReadWrite);
            this._romMapping = new MemoryMapping(this._rom, 0xc000, Mask.Sixteen, AccessLevel.ReadOnly);
        }

        public MC6809 CPU { get; }

        public MC6850 ACIA { get; } = new MC6850();

        public override MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x8000)
            {
                return this._ramMapping;
            }

            if (absolute < 0xa000)
            {
                return this._unusedMapping;
            }

            if (absolute < 0xc000)
            {
                return this._ioMapping;
            }

            return this._romMapping;
        }

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
            Address.Word = 0b1010000000000000;
            Data = (byte)(MC6850.ControlRegister.CR0 | MC6850.ControlRegister.CR1);  // Master reset
            this.ACIA.CTS.Lower();
            this.ACIA.RW.Lower();
            this.UpdateAciaPins();
            this.ACIA.RaisePOWER();
            this.AccessAcia();
        }

        public override void LowerPOWER()
        {
            ////this.profiler.Generate();

            this.ACIA.LowerPOWER();
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialize()
        {
            Console.TreatControlCAsInput = true;

            // Load our BASIC interpreter
            var directory = this._configuration.RomDirectory + "\\";
            LoadHexFile(directory + "ExBasROM.hex");

            // Catch a byte being transmitted
            this.ACIA.Transmitting += ACIA_Transmitting;

            // Keyboard wiring, check for input once per frame
            this.CPU.ExecutedInstruction += CPU_ExecutedInstruction;

            // Marshal data from ACIA -> memory
            this.ReadingByte += Board_ReadingByte;

            // Marshal data from memory -> ACIA
            this.WrittenByte += Board_WrittenByte;

            if (this._configuration.DebugMode)
            {
                // MC6809 disassembly wiring
                this.CPU.ExecutingInstruction += CPU_ExecutingInstruction;
                this.CPU.ExecutedInstruction += CPU_ExecutedInstruction_Debug;
            }

            if (this._configuration.TerminatesEarly)
            {
                // Early termination condition for CPU timing code
                this.CPU.ExecutedInstruction += CPU_ExecutedInstruction_Termination;
            }

            ////this.profiler.Enable();
            ////this.profiler.EmitLine += this.Profiler_EmitLine;
        }

        private void Board_ReadingByte(object? sender, EventArgs e)
        {
            this.UpdateAciaPins();
            this.ACIA.RW.Raise();
            if (this.AccessAcia())
            {
                this.Poke(this.ACIA.DATA);
            }
        }

        private void Board_WrittenByte(object? sender, EventArgs e)
        {
            this.UpdateAciaPins();
            if (this.ACIA.Selected)
            {
                this.ACIA.RW.Lower();
                this.AccessAcia();
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
            this._totalCycleCount += (ulong)this.CPU.Cycles;
            if (this._totalCycleCount > Configuration.TerminationCycles)
            {
                this.LowerPOWER();
            }
        }

        private void CPU_ExecutedInstruction_Debug(object? sender, EventArgs e)
        {
            if (!this._ignoreDisassembly)
            {
                var disassembled = $"{this._disassembler.Trace(this._disassembleAt)}\t{this.ACIA.DumpStatus()}";
                Console.Error.WriteLine(disassembled);
            }
        }

        private void CPU_ExecutingInstruction(object? sender, EventArgs e)
        {
            this._disassembleAt = this.CPU.PC.Word;
            this._ignoreDisassembly = this._disassembler.Ignore || this._disassembler.Pause;
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e)
        {
            this._frameCycleCount -= this.CPU.Cycles;
            if (this._frameCycleCount < 0)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.F12)
                    {
                        this.LowerPOWER();
                    }

                    this.ACIA.RDR = Convert.ToByte(key.KeyChar);
                    this.ACIA.MarkReceiveStarting();
                }

                this._frameCycleCount = (long)Configuration.FrameCycleInterval;
            }
        }

        private void ACIA_Transmitting(object? sender, EventArgs e)
        {
            Console.Out.Write(Convert.ToChar(this.ACIA.TDR));
            this.ACIA.MarkTransmitComplete();
        }

        private void UpdateAciaPins()
        {
            this.ACIA.DATA = this.Data;
            this.ACIA.RS.Match(this.Address.Word & (ushort)Bits.Bit0);
            this.ACIA.CS0.Match(this.Address.Word & (ushort)Bits.Bit15);
            this.ACIA.CS1.Match(this.Address.Word & (ushort)Bits.Bit13);
            this.ACIA.CS2.Match(this.Address.Word & (ushort)Bits.Bit14);
        }

        private bool AccessAcia()
        {
            this.ACIA.E.Raise();
            try
            {
                this.ACIA.Tick();
                return this.ACIA.Activated;
            }
            finally
            {
                this.ACIA.E.Lower();
            }
        }
    }
}