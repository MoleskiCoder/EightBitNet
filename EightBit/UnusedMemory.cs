namespace EightBit
{
    using System.IO;

    public class UnusedMemory : Memory
    {
        private readonly int size;
        private readonly byte unchanging;

        UnusedMemory(int size, byte unchanging)
        {
            this.size = size;
            this.unchanging = unchanging;
        }

        public override int Size => size;

        public override int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();
        public override int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();
        public override int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();

        public override byte Peek(ushort address) => unchanging;
        protected override void Poke(ushort address, byte value) => throw new System.NotImplementedException();
    }
}
