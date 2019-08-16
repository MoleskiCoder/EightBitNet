﻿// <copyright file="IoRegisters.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit.GameBoy
{
    using System;

    public sealed class IoRegisters : EightBit.Ram
    {
        public const int BASE = 0xFF00;

        // Port/Mode Registers
        public const int P1 = 0x0;       // R/W Mask5
        public const int SB = 0x1;       // R/W Mask8
        public const int SC = 0x2;       // R/W Bit7 | Bit0

        // Timer control
        public const int DIV = 0x4;      // R/W Mask8
        public const int TIMA = 0x5;     // R/W Mask8
        public const int TMA = 0x6;      // R/W Mask8
        public const int TAC = 0x7;      // R/W Mask3

        // Interrupt Flags
        public const int IF = 0xF;       // R/W Mask5
        public const int IE = 0xFF;      // R/W Mask5

        // Sound Registers
        public const int NR10 = 0x10;    // R/W Mask7
        public const int NR11 = 0x11;    // R/W Bit7 | Bit6
        public const int NR12 = 0x12;    // R/W Mask8
        public const int NR13 = 0x13;    // W   0
        public const int NR14 = 0x14;    // R/W Bit6
        public const int NR21 = 0x16;    // R/W Bit7 | Bit6
        public const int NR22 = 0x17;    // R/W Mask8
        public const int NR23 = 0x18;    // W   0
        public const int NR24 = 0x19;    // R/W Bit6
        public const int NR30 = 0x1A;    // R/W Bit7
        public const int NR31 = 0x1B;    // R/W Mask8
        public const int NR32 = 0x1C;    // R/W Bit6 | Bit5
        public const int NR33 = 0x1D;    // W   0
        public const int NR34 = 0x1E;    // R/W Bit6
        public const int NR41 = 0x20;    // R/W Mask6
        public const int NR42 = 0x21;    // R/W Mask8
        public const int NR43 = 0x22;    // R/W Mask8
        public const int NR44 = 0x23;    // R/W Bit6
        public const int NR50 = 0x24;    // R/W Mask8
        public const int NR51 = 0x25;    // R/W Mask8
        public const int NR52 = 0x26;    // R/W Mask8   Mask8

        public const int WAVE_PATTERN_RAM_START = 0x30;
        public const int WAVE_PATTERN_RAM_END = 0x3F;

        // LCD Display Registers
        public const int LCDC = 0x40;    // R/W Mask8
        public const int STAT = 0x41;    // R/W Mask7
        public const int SCY = 0x42;     // R/W Mask8
        public const int SCX = 0x43;     // R/W Mask8
        public const int LY = 0x44;      // R   Mask8   zeroed
        public const int LYC = 0x45;     // R/W Mask8
        public const int DMA = 0x46;     // W   0
        public const int BGP = 0x47;     // R/W Mask8
        public const int OBP0 = 0x48;    // R/W Mask8
        public const int OBP1 = 0x49;    // R/W Mask8
        public const int WY = 0x4A;      // R/W Mask8
        public const int WX = 0x4B;      // R/W Mask8

        // Boot rom control
        public const int BOOT_DISABLE = 0x50;

        private readonly Bus bus;
        private readonly Register16 divCounter = new Register16(0xab, 0xcc);
        private readonly Register16 dmaAddress = new Register16();

        private int timerCounter = 0;
        private int timerRate = 0;

        private bool dmaTransferActive = false;

        private bool scanP15 = false;
        private bool scanP14 = false;

        private bool p15 = true;  // misc keys
        private bool p14 = true;  // direction keys
        private bool p13 = true;  // down/start
        private bool p12 = true;  // up/select
        private bool p11 = true;  // left/b
        private bool p10 = true;  // right/a

        public IoRegisters(Bus bus)
        : base(0x80)
        {
            this.bus = bus;
            this.bus.ReadingByte += this.Bus_ReadingByte;
            this.bus.WrittenByte += this.Bus_WrittenByte;
        }

        public event EventHandler<LcdStatusModeEventArgs> DisplayStatusModeUpdated;

        public bool BootRomDisabled { get; private set; } = false;

        public bool BootRomEnabled => !this.BootRomDisabled;

        public int TimerClock => this.TimerControl & (byte)Mask.Mask2;

        public bool TimerEnabled => !this.TimerDisabled;

        public bool TimerDisabled => (this.TimerControl & (byte)Bits.Bit2) == 0;

        public int TimerClockTicks
        {
            get
            {
                switch (this.TimerClock)
                {
                    case 0b00:
                        return 1024;    // 4.096 Khz
                    case 0b01:
                        return 16;      // 262.144 Khz
                    case 0b10:
                        return 64;      // 65.536 Khz
                    case 0b11:
                        return 256;     // 16.384 Khz
                }

                throw new InvalidOperationException("Invalid timer clock specification");
            }
        }

        private ref byte TimerControl => ref this.Reference(TAC);

        private ref byte TimerModulo => ref this.Reference(TMA);

        private ref byte TimerCounter => ref this.Reference(TIMA);

        public void Reset()
        {
            this.Poke(NR52, 0xf1);
            this.Poke(LCDC, (byte)(LcdcControl.DisplayBackground | LcdcControl.BackgroundCharacterDataSelection | LcdcControl.LcdEnable));
            this.divCounter.Word = 0xabcc;
            this.timerCounter = 0;
        }

        public void TransferDma()
        {
            if (this.dmaTransferActive)
            {
                this.bus.OAMRAM.Poke(this.dmaAddress.Low, this.bus.Peek(this.dmaAddress));
                this.dmaTransferActive = ++this.dmaAddress.Low < 0xa0;
            }
        }

        public void TriggerInterrupt(Interrupts cause) => this.Reference(IF) |= (byte)cause;

        public void CheckTimers(int cycles)
        {
            this.IncrementDIV(cycles);
            this.CheckTimer(cycles);
        }

        public void IncrementDIV(int cycles)
        {
            this.divCounter.Word += (ushort)cycles;
            this.Poke(DIV, this.divCounter.High);
        }

        public void IncrementTIMA()
        {
            var updated = this.TimerCounter + 1;
            if ((updated & (int)Bits.Bit8) != 0)
            {
                this.TriggerInterrupt(Interrupts.TimerOverflow);
                updated = this.TimerModulo;
            }

            this.TimerCounter = Chip.LowByte(updated);
        }

        public void IncrementLY() => this.Poke(LY, (byte)((this.Peek(LY) + 1) % GameBoy.Bus.TotalLineCount));

        public void ResetLY() => this.Poke(LY, 0);

        public void UpdateLcdStatusMode(LcdStatusMode mode)
        {
            var current = this.Peek(STAT) & unchecked((byte)~Mask.Mask2);
            this.Poke(STAT, (byte)(current | (int)mode));
            this.OnDisplayStatusModeUpdated(mode);
        }

        public void DisableBootRom() => this.BootRomDisabled = true;

        public void EnableBootRom() => this.BootRomDisabled = false;

        public void PressRight()
        {
            this.p14 = this.p10 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseRight() => this.p14 = this.p10 = true;

        public void PressLeft()
        {
            this.p14 = this.p11 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseLeft() => this.p14 = this.p11 = true;

        public void PressUp()
        {
            this.p14 = this.p12 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseUp() => this.p14 = this.p12 = true;

        public void PressDown()
        {
            this.p14 = this.p13 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseDown() => this.p14 = this.p13 = true;

        public void PressA()
        {
            this.p15 = this.p10 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseA() => this.p15 = this.p10 = true;

        public void PressB()
        {
            this.p15 = this.p11 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseB() => this.p15 = this.p11 = true;

        public void PressSelect()
        {
            this.p15 = this.p12 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseSelect() => this.p15 = this.p12 = true;

        public void PressStart()
        {
            this.p15 = this.p13 = false;
            this.TriggerKeypadInterrupt();
        }

        public void ReleaseStart() => this.p15 = this.p13 = true;

        private void OnDisplayStatusModeUpdated(LcdStatusMode mode) => this.DisplayStatusModeUpdated?.Invoke(this, new LcdStatusModeEventArgs(mode));

        private void CheckTimer(int cycles)
        {
            if (this.TimerEnabled)
            {
                this.timerCounter -= cycles;
                if (this.timerCounter <= 0)
                {
                    this.timerCounter += this.timerRate;
                    this.IncrementTIMA();
                }
            }
        }

        private void ApplyMask(ushort address, byte masking) => this.Poke(address, (byte)(this.Peek(address) | ~masking));

        private void TriggerKeypadInterrupt() => this.TriggerInterrupt(Interrupts.KeypadPressed);

        private void Bus_WrittenByte(object sender, System.EventArgs e)
        {
            var address = this.bus.Address.Word;
            var value = this.bus.Data;
            var port = (ushort)(address - BASE);

            switch (port)
            {
            case P1:
                this.scanP14 = (value & (byte)Bits.Bit4) == 0;
                this.scanP15 = (value & (byte)Bits.Bit5) == 0;
                break;

            case SB: // R/W
            case SC: // R/W
                break;

            case DIV: // R/W
                this.Poke(port, 0);
                this.timerCounter = this.divCounter.Word = 0;
                break;
            case TIMA: // R/W
                break;
            case TMA: // R/W
                break;
            case TAC: // R/W
                this.timerRate = this.TimerClockTicks;
                break;

            case IF: // R/W
                break;

            case LCDC:
            case STAT:
            case SCY:
            case SCX:
                break;
            case DMA:
                this.dmaAddress.Word = Chip.PromoteByte(value);
                this.dmaTransferActive = true;
                break;
            case LY: // R/O
                this.Poke(port, 0);
                break;
            case BGP:
            case OBP0:
            case OBP1:
            case WY:
            case WX:
                break;

            case BOOT_DISABLE:
                this.BootRomDisabled = value != 0;
                break;
            }
        }

        private void Bus_ReadingByte(object sender, System.EventArgs e)
        {
            var address = this.bus.Address.Word;
            var io = (address >= BASE) && (address < 0xff80);
            if (io)
            {
                var port = (ushort)(address - BASE);
                switch (port)
                {
                    // Port/Mode Registers
                    case P1:
                        {
                            var directionKeys = this.scanP14 && !this.p14;
                            var miscKeys = this.scanP15 && !this.p15;
                            var live = directionKeys || miscKeys;
                            var rightOrA = (live && !this.p10) ? 0 : Bits.Bit0;
                            var leftOrB = (live && !this.p11) ? 0 : Bits.Bit1;
                            var upOrSelect = (live && !this.p12) ? 0 : Bits.Bit2;
                            var downOrStart = (live && !this.p13) ? 0 : Bits.Bit3;
                            var lowNibble = (byte)(rightOrA | leftOrB | upOrSelect | downOrStart);
                            var highNibble = (byte)Chip.PromoteNibble((byte)Mask.Mask4);
                            var value = (byte)(lowNibble | highNibble);
                            this.Poke(port, value);
                        }

                        break;
                    case SB:
                        break;
                    case SC:
                        this.ApplyMask(port, (byte)(Bits.Bit7 | Bits.Bit0));
                        break;

                    // Timer control
                    case DIV:
                    case TIMA:
                    case TMA:
                        break;
                    case TAC:
                        this.ApplyMask(port, (byte)Mask.Mask3);
                        break;

                    // Interrupt Flags
                    case IF:
                        this.ApplyMask(port, (byte)Mask.Mask5);
                        break;

                    // LCD Display Registers
                    case LCDC:
                        break;
                    case STAT:
                        this.ApplyMask(port, (byte)Mask.Mask7);
                        break;
                    case SCY:
                    case SCX:
                    case LY:
                    case LYC:
                    case DMA:
                    case BGP:
                    case OBP0:
                    case OBP1:
                    case WY:
                    case WX:
                        break;

                    default:
                        this.ApplyMask(port, 0);
                        break;
                }
            }
        }
    }
}
