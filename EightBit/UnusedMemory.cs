// <copyright file="UnusedMemory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class UnusedMemory(int size, byte unchanging) : IMemory
    {
        private readonly int _size = size;
        private readonly byte _unchanging = unchanging;

        public int Size => this._size;

        public int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new NotImplementedException();

        public int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new NotImplementedException();

        public int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new NotImplementedException();

        public byte Peek(ushort address) => this._unchanging;

        public void Poke(ushort address, byte value) => throw new NotImplementedException();

        public ref byte Reference(ushort address) => throw new NotImplementedException();
    }
}
