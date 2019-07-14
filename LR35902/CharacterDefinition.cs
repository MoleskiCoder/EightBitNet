// <copyright file="CharacterDefinition.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    namespace GameBoy
    {
        public sealed class CharacterDefinition
        {
            private readonly Ram vram;
            private readonly ushort address;

            public CharacterDefinition(Ram vram, ushort address)
            {
                this.vram = vram;
                this.address = address;
            }

            public int[] Get(int row)
            {
                var returned = new int[8];

                var planeAddress = (ushort)(this.address + (row * 2));

                var planeLow = this.vram.Peek(planeAddress);
                var planeHigh = this.vram.Peek(++planeAddress);

                for (var bit = 0; bit < 8; ++bit)
                {
                    var mask = 1 << bit;

                    var bitLow = (planeLow & mask) != 0 ? 1 : 0;
                    var bitHigh = (planeHigh & mask) != 0 ? 0b10 : 0;

                    returned[7 - bit] = bitHigh | bitLow;
                }

                return returned;
            }
        }
    }
}
