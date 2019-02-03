namespace EightBit
{
    class IntelOpCodeDecoded
    {
        public int x;
        public int y;
        public int z;
        public int p;
        public int q;

        public IntelOpCodeDecoded() { }

        public IntelOpCodeDecoded(byte opcode)
        {
            x = (opcode & 0b11000000) >> 6;	// 0 - 3
            y = (opcode & 0b00111000) >> 3;	// 0 - 7
            z = (opcode & 0b00000111);		// 0 - 7
            p = (y & 0b110) >> 1;			// 0 - 3
            q = (y & 1);					// 0 - 1
        }
    }
}
