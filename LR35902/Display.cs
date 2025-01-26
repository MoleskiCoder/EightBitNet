// <copyright file="Display.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    public sealed class Display<T>(AbstractColourPalette<T> colours, Bus bus, Ram oam, Ram vram)
    {
        private readonly Bus bus = bus;
        private readonly Ram oam = oam;
        private readonly Ram vram = vram;
        private readonly AbstractColourPalette<T> colours = colours;
        private readonly ObjectAttribute[] objectAttributes = new ObjectAttribute[40];
        private byte control;
        private byte scanLine;

        public T[] Pixels { get; } = new T[DisplayCharacteristics.PixelCount];

        public void Render()
        {
            this.scanLine = this.bus.IO.Peek(IoRegisters.LY);
            if (this.scanLine < DisplayCharacteristics.RasterHeight)
            {
                this.control = this.bus.IO.Peek(IoRegisters.LCDC);
                if ((this.control & (byte)LcdcControls.LCD_EN) != 0)
                {
                    if ((this.control & (byte)LcdcControls.BG_EN) != 0)
                    {
                        this.RenderBackground();
                    }

                    if ((this.control & (byte)LcdcControls.OBJ_EN) != 0)
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
            return
            [
                raw & 0b11,
                (raw & 0b1100) >> 2,
                (raw & 0b110000) >> 4,
                (raw & 0b11000000) >> 6,
            ];
        }

        private void RenderBackground()
        {
            var palette = this.CreatePalette(IoRegisters.BGP);

            var window = (this.control & (byte)LcdcControls.WIN_EN) != 0;
            var bgArea = (this.control & (byte)LcdcControls.BG_MAP) != 0 ? 0x1c00 : 0x1800;
            var bgCharacters = (this.control & (byte)LcdcControls.TILE_SEL) != 0 ? 0 : 0x800;

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
            var address = bgArea + (row * DisplayCharacteristics.BufferCharacterWidth);

            for (var column = 0; column < DisplayCharacteristics.BufferCharacterWidth; ++column)
            {
                var character = this.vram.Peek((ushort)address++);
                var definition = new CharacterDefinition(this.vram, (ushort)(bgCharacters + (16 * character)));
                this.RenderBackgroundTile(
                    (column * 8) + offsetX, (row * 8) + offsetY,
                    palette,
                    definition);
            }
        }

        private void RenderObjects()
        {
            var objBlockHeight = (this.control & (byte)LcdcControls.OBJ_SIZE) != 0 ? 16 : 8;

            var palettes = new int[2][];
            palettes[0] = this.CreatePalette(IoRegisters.OBP0);
            palettes[1] = this.CreatePalette(IoRegisters.OBP1);

            var characterAddressMultiplier = objBlockHeight == 8 ? 16 : 8;

            for (var i = 0; i < 40; ++i)
            {
                var current = this.objectAttributes[i];

                var spriteY = current.PositionY;
                var drawY = spriteY - 16;

                if (this.scanLine >= drawY && this.scanLine < drawY + objBlockHeight)
                {
                    var spriteX = current.PositionX;
                    var drawX = spriteX - 8;

                    var sprite = current.Pattern;
                    var definition = new CharacterDefinition(this.vram, (ushort)(characterAddressMultiplier * sprite));
                    var palette = palettes[current.Palette];
                    var flipX = current.FlipX;
                    var flipY = current.FlipY;

                    this.RenderSpriteTile(
                        objBlockHeight,
                        drawX, drawY,
                        flipX, flipY,
                        palette,
                        definition);
                }
            }
        }

        private void RenderSpriteTile(
            int height,
            int drawX, int drawY,
            bool flipX, bool flipY,
            int[] palette,
            CharacterDefinition definition) => this.RenderTile(
                height,
                drawX, drawY,
                flipX, flipY, true,
                palette,
                definition);

        private void RenderBackgroundTile(
            int drawX, int drawY,
            int[] palette,
            CharacterDefinition definition) => this.RenderTile(
                8,
                drawX, drawY,
                false, false, false,
                palette,
                definition);

        private void RenderTile(
            int height,
            int drawX, int drawY,
            bool flipX, bool flipY,
            bool allowTransparencies,
            int[] palette,
            CharacterDefinition definition)
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

            var lineAddress = y * DisplayCharacteristics.RasterWidth;
            for (var cx = 0; cx < width; ++cx)
            {
                var x = drawX + (flipX ? ~cx & flipMaskX : cx);
                if (x >= DisplayCharacteristics.RasterWidth)
                {
                    break;
                }

                var colour = rowDefinition[cx];
                if (!allowTransparencies || (allowTransparencies && colour > 0))
                {
                    var outputPixel = lineAddress + x;
                    this.Pixels[outputPixel] = this.colours.Colour(palette[colour]);
                }
            }
        }
    }
}
