// <copyright file="Memory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public interface IMemory
    {
        abstract int Size
        {
            get;
        }

        byte Peek(ushort address);

        void Poke(ushort address, byte value);

        ref byte Reference(ushort address);

        int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1);

        int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1);

        int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1);
    }
}
