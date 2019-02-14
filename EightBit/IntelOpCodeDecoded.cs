// <copyright file="IntelOpCodeDecoded.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class IntelOpCodeDecoded
    {
        private readonly int x;
        private readonly int y;
        private readonly int z;
        private readonly int p;
        private readonly int q;

        public IntelOpCodeDecoded()
        {
        }

        public IntelOpCodeDecoded(byte opCode)
        {
            this.x = (opCode & 0b11000000) >> 6;    // 0 - 3
            this.y = (opCode & 0b00111000) >> 3;    // 0 - 7
            this.z = opCode & 0b00000111;           // 0 - 7
            this.p = (this.y & 0b110) >> 1;         // 0 - 3
            this.q = this.y & 1;                    // 0 - 1
        }
    }
}
