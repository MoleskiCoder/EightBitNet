// <copyright file="IntelOpCodeDecoded.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class IntelOpCodeDecoded
    {
        private int x;
        private int y;
        private int z;
        private int p;
        private int q;

        public IntelOpCodeDecoded()
        {
        }

        public IntelOpCodeDecoded(byte opcode)
        {
            this.x = (opcode & 0b11000000) >> 6;    // 0 - 3
            this.y = (opcode & 0b00111000) >> 3;    // 0 - 7
            this.z = opcode & 0b00000111;           // 0 - 7
            this.p = (this.y & 0b110) >> 1;         // 0 - 3
            this.q = this.y & 1;                    // 0 - 1
        }
    }
}
