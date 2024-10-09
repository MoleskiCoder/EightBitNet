// <copyright file="Memory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public abstract class Memory
    {
        public abstract int Size
        {
            get;
        }

        public abstract byte Peek(ushort address);

        public virtual ref byte Reference(ushort address) => throw new NotImplementedException();

        public abstract int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1);

        public abstract int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1);

        public abstract int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1);

        protected abstract void Poke(ushort address, byte value);
    }
}
