// <copyright file="DisplayCharacteristics.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace LR35902
{
    public static class DisplayCharacteristics
    {
        public const int BufferWidth = 256;
        public const int BufferHeight = 256;

        public const int BufferCharacterWidth = BufferWidth / 8;
        public const int BufferCharacterHeight = BufferHeight / 8;

        public const int RasterWidth = 160;
        public const int RasterHeight = 144;

        public const int PixelCount = RasterWidth * RasterHeight;
    }
}
