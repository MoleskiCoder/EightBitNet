// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit.GameBoy
{
    using LR35902;
    using System;
    using System.Collections.Generic;

    public abstract class Bus : EightBit.Bus
    {
        public const int CyclesPerSecond = 4 * 1024 * 1024;
        public const int FramesPerSecond = 60;
        public const int CyclesPerFrame = CyclesPerSecond / FramesPerSecond;
        public const int TotalLineCount = 154;
        public const int CyclesPerLine = CyclesPerFrame / TotalLineCount;
        public const int RomPageSize = 0x4000;

        private readonly Rom bootRom = new(0x100);                                  // 0x0000 - 0x00ff
        private readonly List<Rom> gameRomBanks = new();                      // 0x0000 - 0x3fff, 0x4000 - 0x7fff (switchable)
        private readonly List<Ram> ramBanks = new();                          // 0xa000 - 0xbfff (switchable)
        private readonly UnusedMemory unmapped2000 = new(0x2000, 0xff);    // 0xa000 - 0xbfff
        private readonly Ram lowInternalRam = new(0x2000);                          // 0xc000 - 0xdfff (mirrored at 0xe000)
        private readonly UnusedMemory unmapped60 = new(0x60, 0xff);        // 0xfea0 - 0xfeff
        private readonly Ram highInternalRam = new(0x80);                           // 0xff80 - 0xffff

        private bool enabledLCD = false;

        private bool disabledGameRom = false;

        private bool rom = false;
        private bool banked = false;
        private bool ram = false;
        private bool battery = false;

        private bool higherRomBank = true;
        private bool ramBankSwitching = false;

        private int romBank = 1;
        private int ramBank = 0;

        private int allowed = 0;

        protected Bus()
        {
            this.IO = new IoRegisters(this);
            this.CPU = new LR35902(this);
        }

        public LR35902 CPU { get; }

        public Ram VRAM { get; } = new Ram(0x2000);

        public Ram OAMRAM { get; } = new Ram(0xa0);

        public IoRegisters IO { get; }

        public bool GameRomDisabled => this.disabledGameRom;

        public bool GameRomEnabled => !this.GameRomDisabled;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.RaiseINT();
            this.Reset();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public void Reset()
        {
            this.IO.Reset();
            this.CPU.LowerRESET();
        }

        public void DisableGameRom() => this.disabledGameRom = true;

        public void EnableGameRom() => this.disabledGameRom = false;

        public void LoadBootRom(string path) => this.bootRom.Load(path);

        public void LoadGameRom(string path)
        {
            const int bankSize = 0x4000;
            this.gameRomBanks.Clear();
            this.gameRomBanks.Add(new Rom());
            var size = this.gameRomBanks[0].Load(path, 0, 0, bankSize);
            var banks = size / bankSize;
            for (var bank = 1; bank < banks; ++bank)
            {
                this.gameRomBanks.Add(new Rom());
                this.gameRomBanks[bank].Load(path, 0, bankSize * bank, bankSize);
            }

            this.ValidateCartridgeType();
        }

        public void RunRasterLines()
        {
            this.enabledLCD = (this.IO.Peek(IoRegisters.LCDC) & (byte)LcdcControls.LcdEnable) != 0;
            this.IO.ResetLY();
            this.RunRasterLines(DisplayCharacteristics.RasterHeight);
        }

        public void RunVerticalBlankLines()
        {
            var lines = TotalLineCount - DisplayCharacteristics.RasterHeight;
            this.RunVerticalBlankLines(lines);
        }

        public override MemoryMapping Mapping(ushort address)
        {
            if ((address < 0x100) && this.IO.BootRomEnabled)
            {
                return new MemoryMapping(this.bootRom, 0x0000, Mask.Sixteen, AccessLevel.ReadOnly);
            }

            if ((address < 0x4000) && this.GameRomEnabled)
            {
                return new MemoryMapping(this.gameRomBanks[0], 0x0000, 0xffff, AccessLevel.ReadOnly);
            }

            if ((address < 0x8000) && this.GameRomEnabled)
            {
                return new MemoryMapping(this.gameRomBanks[this.romBank], 0x4000, 0xffff, AccessLevel.ReadOnly);
            }

            if (address < 0xa000)
            {
                return new MemoryMapping(this.VRAM, 0x8000, 0xffff, AccessLevel.ReadWrite);
            }

            if (address < 0xc000)
            {
                if (this.ramBanks.Count == 0)
                {
                    return new MemoryMapping(this.unmapped2000, 0xa000, 0xffff, AccessLevel.ReadOnly);
                }
                else
                {
                    return new MemoryMapping(this.ramBanks[this.ramBank], 0xa000, 0xffff, AccessLevel.ReadWrite);
                }
            }

            if (address < 0xe000)
            {
                return new MemoryMapping(this.lowInternalRam, 0xc000, 0xffff, AccessLevel.ReadWrite);
            }

            if (address < 0xfe00)
            {
                return new MemoryMapping(this.lowInternalRam, 0xe000, 0xffff, AccessLevel.ReadWrite); // Low internal RAM mirror
            }

            if (address < 0xfea0)
            {
                return new MemoryMapping(this.OAMRAM, 0xfe00, 0xffff, AccessLevel.ReadWrite);
            }

            if (address < IoRegisters.BASE)
            {
                return new MemoryMapping(this.unmapped60, 0xfea0, 0xffff, AccessLevel.ReadOnly);
            }

            if (address < 0xff80)
            {
                return new MemoryMapping(this.IO, IoRegisters.BASE, 0xffff, AccessLevel.ReadWrite);
            }

            return new MemoryMapping(this.highInternalRam, 0xff80, 0xffff, AccessLevel.ReadWrite);
        }

        protected override void OnWrittenByte()
        {
            base.OnWrittenByte();

            var address = this.Address.Word;
            var value = this.Data;

            switch (address & 0xe000)
            {
                case 0x0000:
                    // Register 0: RAMCS gate data
                    if (this.ram)
                    {
                        throw new InvalidOperationException("Register 0: RAMCS gate data: Not handled!");
                    }

                    break;
                case 0x2000:
                    // Register 1: ROM bank code
                    if (this.banked && this.higherRomBank)
                    {
                        // assert((address >= 0x2000) && (address < 0x4000));
                        // assert((value > 0) && (value < 0x20));
                        this.romBank = value & (byte)Mask.Five;
                    }

                    break;
                case 0x4000:
                    // Register 2: ROM bank selection
                    if (this.banked)
                    {
                        throw new InvalidOperationException("Register 2: ROM bank selection: Not handled!");
                    }

                    break;
                case 0x6000:
                    // Register 3: ROM/RAM change
                    if (this.banked)
                    {
                        switch (value & (byte)Mask.One)
                        {
                            case 0:
                                this.higherRomBank = true;
                                this.ramBankSwitching = false;
                                break;
                            case 1:
                                this.higherRomBank = false;
                                this.ramBankSwitching = true;
                                break;
                            default:
                                throw new InvalidOperationException("Unreachable");
                        }
                    }

                    break;
            }
        }

        private void ValidateCartridgeType()
        {
            this.rom = this.banked = this.ram = this.battery = false;

            // ROM type
            switch (this.gameRomBanks[0].Peek(0x147))
            {
                case (byte)CartridgeType.ROM:
                    this.rom = true;
                    break;
                case (byte)CartridgeType.ROM_MBC1:
                    this.rom = this.banked = true;
                    break;
                case (byte)CartridgeType.ROM_MBC1_RAM:
                    this.rom = this.banked = this.ram = true;
                    break;
                case (byte)CartridgeType.ROM_MBC1_RAM_BATTERY:
                    this.rom = this.banked = this.ram = this.battery = true;
                    break;
                default:
                    throw new InvalidOperationException("Unhandled cartridge ROM type");
            }

            // ROM size
            {
                var gameRomBanks = 0;
                var romSizeSpecification = this.Peek(0x148);
                switch (romSizeSpecification)
                {
                    case 0x52:
                        gameRomBanks = 72;
                        break;
                    case 0x53:
                        gameRomBanks = 80;
                        break;
                    case 0x54:
                        gameRomBanks = 96;
                        break;
                    default:
                        if (romSizeSpecification > 6)
                        {
                            throw new InvalidOperationException("Invalid ROM size specification");
                        }

                        gameRomBanks = 1 << (romSizeSpecification + 1);
                        if (gameRomBanks != this.gameRomBanks.Count)
                        {
                            throw new InvalidOperationException("ROM size specification mismatch");
                        }

                        break;
                }

                // RAM size
                {
                    var ramSizeSpecification = this.gameRomBanks[0].Peek(0x149);
                    switch (ramSizeSpecification)
                    {
                        case 0:
                            break;
                        case 1:
                            this.ramBanks.Clear();
                            this.ramBanks.Add(new Ram(2 * 1024));
                            break;
                        case 2:
                            this.ramBanks.Clear();
                            this.ramBanks.Add(new Ram(8 * 1024));
                            break;
                        case 3:
                            this.ramBanks.Clear();
                            for (var i = 0; i < 4; ++i)
                            {
                                this.ramBanks.Add(new Ram(8 * 1024));
                            }

                            break;
                        case 4:
                            this.ramBanks.Clear();
                            for (var i = 0; i < 16; ++i)
                            {
                                this.ramBanks.Add(new Ram(8 * 1024));
                            }

                            break;
                        default:
                            throw new InvalidOperationException("Invalid RAM size specification");
                    }
                }
            }
        }

        private void RunRasterLines(int lines)
        {
            for (var line = 0; line < lines; ++line)
            {
                this.RunRasterLine(CyclesPerLine);
            }
        }

        private void RunVerticalBlankLines(int lines)
        {
            /*
            Vertical Blank interrupt is triggered when the LCD
            controller enters the VBL screen mode (mode 1, LY=144).
            This happens once per frame, so this interrupt is
            triggered 59.7 times per second. During this period the
            VRAM and OAM can be accessed freely, so it's the best
            time to update graphics (for example, use the OAM DMA to
            update sprites for next frame, or update tiles to make
            animations).
            This period lasts 4560 clocks in normal speed mode and
            9120 clocks in double speed mode. That's exactly the
            time needed to draw 10 scanlines.
            The VBL interrupt isn't triggered when the LCD is
            powered off or on, even when it was on VBL mode.
            It's only triggered when the VBL period starts.
            */
            if (this.enabledLCD)
            {
                this.IO.UpdateLcdStatusMode(LcdStatusMode.VBlank);
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit4) != 0)
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                this.IO.TriggerInterrupt(Interrupts.VerticalBlank);
            }

            this.RunRasterLines(lines);
        }

        private void RunRasterLine(int suggested)
        {
            /*
            A scanline normally takes 456 clocks (912 clocks in double speed
            mode) to complete. A scanline starts in mode 2, then goes to
            mode 3 and, when the LCD controller has finished drawing the
            line (the timings depend on lots of things) it goes to mode 0.
            During lines 144-153 the LCD controller is in mode 1.
            Line 153 takes only a few clocks to complete (the exact
            timings are below). The rest of the clocks of line 153 are
            spent in line 0 in mode 1!

            During mode 0 and mode 1 the CPU can access both VRAM and OAM.
            During mode 2 the CPU can only access VRAM, not OAM.
            During mode 3 OAM and VRAM can't be accessed.
            In GBC mode the CPU can't access Palette RAM(FF69h and FF6Bh)
            during mode 3.
            A scanline normally takes 456 clocks(912 clocks in double speed mode) to complete.
            A scanline starts in mode 2, then goes to mode 3 and , when the LCD controller has
            finished drawing the line(the timings depend on lots of things) it goes to mode 0.
            During lines 144 - 153 the LCD controller is in mode 1.
            Line 153 takes only a few clocks to complete(the exact timings are below).
            The rest of the clocks of line 153 are spent in line 0 in mode 1!
            */

            this.allowed += suggested;
            if (this.enabledLCD)
            {
                if (((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit6) != 0) && (this.IO.Peek(IoRegisters.LYC) == this.IO.Peek(IoRegisters.LY)))
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                // Mode 2, OAM unavailable
                this.IO.UpdateLcdStatusMode(LcdStatusMode.SearchingOamRam);
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit5) != 0)
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                this.allowed -= this.CPU.Run(80); // ~19us

                // Mode 3, OAM/VRAM unavailable
                this.IO.UpdateLcdStatusMode(LcdStatusMode.TransferringDataToLcd);
                this.allowed -= this.CPU.Run(170);    // ~41us

                // Mode 0
                this.IO.UpdateLcdStatusMode(LcdStatusMode.HBlank);
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit3) != 0)
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                this.allowed -= this.CPU.Run(this.allowed);  // ~48.6us

                this.IO.IncrementLY();
            }
            else
            {
                this.allowed -= this.CPU.Run(this.allowed);
            }
        }
    }
}
