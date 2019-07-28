// <copyright file="Display.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit.GameBoy
{
    public sealed class Display
    {
        public static readonly int BufferWidth = 256;
        public static readonly int BufferHeight = 256;
        public static readonly int BufferCharacterWidth = BufferWidth / 8;
        public static readonly int BufferCharacterHeight = BufferHeight / 8;
        public static readonly int RasterWidth = 160;
        public static readonly int RasterHeight = 144;
        public static readonly int PixelCount = RasterWidth * RasterHeight;

        private readonly Bus bus;
        private readonly Ram oam;
        private readonly Ram vram;
        private readonly AbstractColourPalette colours;
        private readonly ObjectAttribute[] objectAttributes = new ObjectAttribute[40];
        private byte control;
        private byte scanLine = 0;

        public Display(AbstractColourPalette colours, Bus bus, Ram oam, Ram vram)
        {
            this.colours = colours;
            this.bus = bus;
            this.oam = oam;
            this.vram = vram;
        }

        public uint[] Pixels { get; } = new uint[PixelCount];

        public void Render()
        {
            this.scanLine = this.bus.IO.Peek(IoRegisters.LY);
            if (this.scanLine < RasterHeight)
            {
                this.control = this.bus.IO.Peek(IoRegisters.LCDC);
                if ((this.control & (byte)LcdcControl.LcdEnable) != 0)
                {
                    if ((this.control & (byte)LcdcControl.DisplayBackground) != 0)
                    {
                        this.RenderBackground();
                    }

                    if ((this.control & (byte)LcdcControl.ObjectEnable) != 0)
                    {
                        this.RenderObjects();
                    }
                }
            }
        }

        public void LoadObjectAttributes()
        {
            for (var i = 0; i < 40; ++i)
            {
                this.objectAttributes[i] = new ObjectAttribute(this.oam, (ushort)(4 * i));
            }
        }

        private int[] CreatePalette(ushort address)
        {
            var raw = this.bus.IO.Peek(address);
            return new int[4]
            {
                raw & 0b11,
                (raw & 0b1100) >> 2,
                (raw & 0b110000) >> 4,
                (raw & 0b11000000) >> 6,
            };
        }

        private void RenderBackground()
        {
            var palette = this.CreatePalette(IoRegisters.BGP);

            var window = (this.control & (byte)LcdcControl.WindowEnable) != 0;
            var bgArea = (this.control & (byte)LcdcControl.BackgroundCodeAreaSelection) != 0 ? 0x1c00 : 0x1800;
            var bgCharacters = (this.control & (byte)LcdcControl.BackgroundCharacterDataSelection) != 0 ? 0 : 0x800;

            var wx = this.bus.IO.Peek(IoRegisters.WX);
            var wy = this.bus.IO.Peek(IoRegisters.WY);

            var offsetX = window ? wx - 7 : 0;
            var offsetY = window ? wy : 0;

            var scrollX = this.bus.IO.Peek(IoRegisters.SCX);
            var scrollY = this.bus.IO.Peek(IoRegisters.SCY);

            this.RenderBackground(bgArea, bgCharacters, offsetX - scrollX, offsetY - scrollY, palette);
        }

        private void RenderBackground(int bgArea, int bgCharacters, int offsetX, int offsetY, int[] palette)
        {
            var row = (this.scanLine - offsetY) / 8;
            var address = bgArea + (row * BufferCharacterWidth);

            for (var column = 0; column < BufferCharacterWidth; ++column)
            {
                var character = this.vram.Peek((ushort)address++);
                var definition = new CharacterDefinition(this.vram, (ushort)(bgCharacters + (16 * character)));
                this.RenderTile(8, (column * 8) + offsetX, (row * 8) + offsetY, false, false, false, palette, definition);
            }
        }

        private void RenderObjects()
        {
            var objBlockHeight = (this.control & (byte)LcdcControl.ObjectBlockCompositionSelection) != 0 ? 16 : 8;

            var palettes = new int[2][];
            palettes[0] = this.CreatePalette(IoRegisters.OBP0);
            palettes[1] = this.CreatePalette(IoRegisters.OBP1);

            var characterAddressMultiplier = objBlockHeight == 8 ? 16 : 8;

            for (var i = 0; i < 40; ++i)
            {
                var current = this.objectAttributes[i];

                var spriteY = current.PositionY;
                var drawY = spriteY - 16;

                if ((this.scanLine >= drawY) && (this.scanLine < (drawY + objBlockHeight)))
                {
                    var spriteX = current.PositionX;
                    var drawX = spriteX - 8;

                    var sprite = current.Pattern;
                    var definition = new CharacterDefinition(this.vram, (ushort)(characterAddressMultiplier * sprite));
                    var palette = palettes[current.Palette];
                    var flipX = current.FlipX;
                    var flipY = current.FlipY;

                    this.RenderTile(objBlockHeight, drawX, drawY, flipX, flipY, true, palette, definition);
                }
            }
        }

        private void RenderTile(int height, int drawX, int drawY, bool flipX, bool flipY, bool allowTransparencies, int[] palette, CharacterDefinition definition)
        {
            const int width = 8;
            const int flipMaskX = width - 1;
            var flipMaskY = height - 1;

            var y = this.scanLine;

            var cy = y - drawY;
            if (flipY)
            {
                cy = ~cy & flipMaskY;
            }

            var rowDefinition = definition.Get(cy);

            var lineAddress = y * RasterWidth;
            for (var cx = 0; cx < width; ++cx)
            {
                var x = drawX + (flipX ? ~cx & flipMaskX : cx);
                if (x >= RasterWidth)
                {
                    break;
                }

                var colour = rowDefinition[cx];
                if (!allowTransparencies || (allowTransparencies && (colour > 0)))
                {
                    var outputPixel = lineAddress + x;
                    this.Pixels[outputPixel] = this.colours.Colour(palette[colour]);
                }
            }
        }
    }
}
