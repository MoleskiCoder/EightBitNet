// <copyright file="CharacterDefinition.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902
{
    using EightBit;

    public sealed class CharacterDefinition(Ram vram, ushort address)
    {
        private readonly Ram vram = vram;
        private readonly ushort address = address;

        public int[] Get(int row)
        {
            var returned = new int[8];

            var planeAddress = (ushort)(this.address + (row * 2));

            var planeLow = this.vram.Peek(planeAddress);
            var planeHigh = this.vram.Peek(++planeAddress);

            for (var bit = 0; bit < 8; ++bit)
            {
                var mask = Chip.Bit(bit);

                var bitLow = (planeLow & mask) != 0 ? 1 : 0;
                var bitHigh = (planeHigh & mask) != 0 ? 0b10 : 0;

                var index = 7 - bit;
                returned[index] = bitHigh | bitLow;
            }

            return returned;
        }
    }
}
