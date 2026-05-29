// <copyright file="Bus.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;
    using System.Diagnostics;

    public abstract class Bus : EightBit.Bus
    {
        public const int CyclesPerSecond = 4 * 1024 * 1024;
        public const int FramesPerSecond = 60;
        public const int CyclesPerFrame = CyclesPerSecond / FramesPerSecond;
        public const int TotalLineCount = 154;
        public const int CyclesPerLine = CyclesPerFrame / TotalLineCount;
        public const int RomPageSize = 0x4000;

        private readonly Rom _bootRom = new(0x100);                          // 0x0000 - 0x00ff
        private readonly List<Rom> _gameRomBanks = [];                       // 0x0000 - 0x3fff, 0x4000 - 0x7fff (switchable)
        private readonly List<Ram> _ramBanks = [];                           // 0xa000 - 0xbfff (switchable)
        private readonly UnusedMemory _unmapped2000 = new(0x2000, 0xff);     // 0xa000 - 0xbfff
        private readonly Ram _lowInternalRam = new(0x2000);                  // 0xc000 - 0xdfff (mirrored at 0xe000)
        private readonly UnusedMemory _unmapped60 = new(0x60, 0xff);         // 0xfea0 - 0xfeff
        private readonly Ram _highInternalRam = new(0x80);                   // 0xff80 - 0xffff

        private bool _enabledLCD;
        private bool _rom;
        private bool _banked;
        private bool _ram;
        private bool _battery;

        private bool _higherRomBank = true;
        private bool _ramBankSwitching = false;

        private int _romBank = 1;
        private int _ramBank = 0;

        private int _allowed = 0;

        private readonly MemoryMapping _bootRomMapping;
        private readonly MemoryMapping _vRamMapping;
        private readonly MemoryMapping _lowInternalRamMapping;
        private readonly MemoryMapping _lowInternalRamMirrorMapping;
        private readonly MemoryMapping _oamRamMapping;
        private readonly MemoryMapping _unmapped2000Mapping;
        private readonly MemoryMapping _unmapped60Mapping;
        private readonly MemoryMapping _ioMapping;
        private readonly MemoryMapping _highInternalRamMapping;
        private readonly List<MemoryMapping> _gameRomBankMappings = [];
        private readonly List<MemoryMapping> _ramBankMappings = [];

        protected Bus(bool ioTriggers = true)
        {
            this.IO = new IoRegisters(this, ioTriggers);
            this.CPU = new LR35902(this);
            this.CPU.MachineTicked += this.CPU_MachineTicked;

            this._bootRomMapping = new(this._bootRom, 0x0000, Mask.Sixteen, AccessLevel.ReadOnly);
            this._vRamMapping = new(this.VRAM, 0x8000, 0xffff, AccessLevel.ReadWrite);
            this._lowInternalRamMapping = new(this._lowInternalRam, 0xc000, 0xffff, AccessLevel.ReadWrite);
            this._lowInternalRamMirrorMapping = new(this._lowInternalRam, 0xe000, 0xffff, AccessLevel.ReadWrite);
            this._oamRamMapping = new(this.OAMRAM, 0xfe00, 0xffff, AccessLevel.ReadWrite);
            this._unmapped2000Mapping = new(this._unmapped2000, 0xa000, 0xffff, AccessLevel.ReadOnly);
            this._unmapped60Mapping = new(this._unmapped60, 0xfea0, 0xffff, AccessLevel.ReadOnly);
            this._ioMapping = new(this.IO, IoRegisters.BASE, 0xffff, AccessLevel.ReadWrite);
            this._highInternalRamMapping = new(this._highInternalRam, 0xff80, 0xffff, AccessLevel.ReadWrite);

            this.CPU.WrittenMemory += Bus_WrittenByte;
        }

        public LR35902 CPU { get; }

        public Ram VRAM { get; } = new Ram(0x2000);

        public Ram OAMRAM { get; } = new Ram(0xa0);

        public IoRegisters IO { get; }

        public bool GameRomDisabled { get; private set; }

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

        public void DisableGameRom() => this.GameRomDisabled = true;

        public void EnableGameRom() => this.GameRomDisabled = false;

        public void LoadBootRom(string path) => this._bootRom.Load(path);

        public void LoadGameRom(string path)
        {
            this._gameRomBanks.Clear();
            this._gameRomBankMappings.Clear();

            const int bankSize = 0x4000;

            var rom = new Rom();
            var size = rom.Load(path, 0, 0, bankSize);
            this._gameRomBanks.Add(rom);

            var banks = size / bankSize;
            this._gameRomBankMappings.Add(new(rom, 0x0000, 0xffff, AccessLevel.ReadOnly));
            Debug.Assert(_gameRomBankMappings.Count == 1);

            for (var bank = 1; bank < banks; ++bank)
            {
                var bankedROM = new Rom();
                bankedROM.Load(path, 0, bankSize * bank, bankSize);
                this._gameRomBanks.Add(bankedROM);
                this._gameRomBankMappings.Add(new(bankedROM, 0x4000, 0xffff, AccessLevel.ReadOnly));
            }

            Debug.Assert(_gameRomBankMappings.Count == banks);

            this.ValidateCartridgeType();
        }

        public void RunRasterLines()
        {
            this._enabledLCD = (this.IO.Peek(IoRegisters.LCDC) & (byte)LcdcControls.LCD_EN) != 0;
            this.IO.ResetLY();
            this.RunRasterLines(DisplayCharacteristics.RasterHeight);
        }

        public void RunVerticalBlankLines()
        {
            var lines = TotalLineCount - DisplayCharacteristics.RasterHeight;
            this.RunVerticalBlankLines(lines);
        }

        public override MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x100 && this.IO.BootRomEnabled)
                return this._bootRomMapping;

            if (absolute < 0x4000 && this.GameRomEnabled)
                return this._gameRomBankMappings[0];

            if (absolute < 0x8000 && this.GameRomEnabled)
                return this._gameRomBankMappings[this._romBank];

            if (absolute < 0xa000)
                return this._vRamMapping;

            if (absolute < 0xc000)
                return this._ramBankMappings.Count == 0 ? this._unmapped2000Mapping : this._ramBankMappings[this._ramBank];

            if (absolute < 0xe000)
                return this._lowInternalRamMapping;

            if (absolute < 0xfe00)
                return this._lowInternalRamMirrorMapping;

            if (absolute < 0xfea0)
                return this._oamRamMapping;

            if (absolute < IoRegisters.BASE)
                return this._unmapped60Mapping;

            if (absolute < 0xff80)
                return this._ioMapping;

            return this._highInternalRamMapping;
        }

        private void Bus_WrittenByte(object? sender, EventArgs e)
        {
            var address = this.Address.Joined;
            var value = this.Data;

            switch (address & 0xe000)
            {
                case 0x0000:
                    // Register 0: RAMCS gate data
                    if (this._ram)
                    {
                        throw new InvalidOperationException("Register 0: RAMCS gate data: Not handled!");
                    }

                    break;
                case 0x2000:
                    // Register 1: ROM bank code
                    if (this._banked && this._higherRomBank)
                    {
                        // assert((absolute >= 0x2000) && (absolute < 0x4000));
                        // assert((value > 0) && (value < 0x20));
                        this._romBank = value & (byte)Mask.Five;
                    }

                    break;
                case 0x4000:
                    // Register 2: ROM bank selection
                    if (this._banked)
                    {
                        throw new InvalidOperationException("Register 2: ROM bank selection: Not handled!");
                    }

                    break;
                case 0x6000:
                    // Register 3: ROM/RAM change
                    if (this._banked)
                    {
                        switch (value & (byte)Mask.One)
                        {
                            case 0:
                                this._higherRomBank = true;
                                this._ramBankSwitching = false;
                                break;
                            case 1:
                                this._higherRomBank = false;
                                this._ramBankSwitching = true;
                                break;
                            default:
                                throw new InvalidOperationException("Unreachable");
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        private void CPU_MachineTicked(object? sender, EventArgs e)
        {
            this.IO.IncrementTimers();
            this.IO.TransferDma();
        }

        private void ValidateCartridgeType()
        {
            this._rom = this._banked = this._ram = this._battery = false;

            // ROM type
            this._rom = this._gameRomBanks[0].Peek(0x147) switch
            {
                (byte)CartridgeType.ROM => true,
                (byte)CartridgeType.ROM_MBC1 => this._banked = true,
                (byte)CartridgeType.ROM_MBC1_RAM => this._banked = this._ram = true,
                (byte)CartridgeType.ROM_MBC1_RAM_BATTERY => this._banked = this._ram = this._battery = true,
                _ => throw new InvalidOperationException("Unhandled cartridge ROM type"),
            };

            // ROM size
            {
                var romSizeSpecification = this.Peek(0x148);
                int gameRomBanks;
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

                        gameRomBanks = 1 << romSizeSpecification + 1;
                        if (gameRomBanks != this._gameRomBanks.Count)
                        {
                            throw new InvalidOperationException("ROM size specification mismatch");
                        }

                        break;
                }

                // RAM size
                {
                    var ramSizeSpecification = this._gameRomBanks[0].Peek(0x149);
                    switch (ramSizeSpecification)
                    {
                        case 0:
                            break;
                        case 1:
                            this._ramBanks.Clear();
                            this._ramBanks.Add(new Ram(2 * 1024));
                            break;
                        case 2:
                            this._ramBanks.Clear();
                            this._ramBanks.Add(new Ram(8 * 1024));
                            break;
                        case 3:
                            this._ramBanks.Clear();
                            for (var i = 0; i < 4; ++i)
                            {
                                this._ramBanks.Add(new Ram(8 * 1024));
                            }

                            break;
                        case 4:
                            this._ramBanks.Clear();
                            for (var i = 0; i < 16; ++i)
                            {
                                this._ramBanks.Add(new Ram(8 * 1024));
                            }

                            break;
                        default:
                            throw new InvalidOperationException("Invalid RAM size specification");
                    }

                    this._ramBankMappings.Clear();
                    foreach (var bank in this._ramBanks)
                    {
                        this._ramBankMappings.Add(new MemoryMapping(bank, 0xa000, 0xffff, AccessLevel.ReadWrite));
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
            if (this._enabledLCD)
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

            this._allowed += suggested;
            if (this._enabledLCD)
            {
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit6) != 0 && this.IO.Peek(IoRegisters.LYC) == this.IO.Peek(IoRegisters.LY))
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                // Mode 2, OAM unavailable
                this.IO.UpdateLcdStatusMode(LcdStatusMode.SearchingOamRam);
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit5) != 0)
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                this._allowed -= this.CPU.Run(80); // ~19us

                // Mode 3, OAM/VRAM unavailable
                this.IO.UpdateLcdStatusMode(LcdStatusMode.TransferringDataToLcd);
                this._allowed -= this.CPU.Run(170);    // ~41us

                // Mode 0
                this.IO.UpdateLcdStatusMode(LcdStatusMode.HBlank);
                if ((this.IO.Peek(IoRegisters.STAT) & (byte)Bits.Bit3) != 0)
                {
                    this.IO.TriggerInterrupt(Interrupts.DisplayControlStatus);
                }

                this._allowed -= this.CPU.Run(this._allowed);  // ~48.6us

                this.IO.IncrementLY();
            }
            else
            {
                this._allowed -= this.CPU.Run(CyclesPerLine);
            }
        }
    }
}
