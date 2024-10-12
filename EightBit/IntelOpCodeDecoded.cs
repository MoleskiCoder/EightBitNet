// <copyright file="IntelOpCodeDecoded.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class IntelOpCodeDecoded
    {
        public IntelOpCodeDecoded(byte opCode)
        {
            this.X = (opCode & 0b11000000) >> 6;    // 0 - 3
            this.Y = (opCode & 0b00111000) >> 3;    // 0 - 7
            this.Z = opCode & 0b00000111;           // 0 - 7
            this.P = (this.Y & 0b110) >> 1;         // 0 - 3
            this.Q = this.Y & 0b1;                  // 0 - 1
        }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public int P { get; }

        public int Q { get; }
    }
}
