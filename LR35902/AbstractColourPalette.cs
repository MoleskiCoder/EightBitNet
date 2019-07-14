// <copyright file="AbstractColourPalette.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    namespace GameBoy
    {
        public class AbstractColourPalette
        {
            private readonly uint[] colours = new uint[4];

            protected AbstractColourPalette()
            { }

            public uint Colour(int index) => this.colours[index];
        }
    }
}
