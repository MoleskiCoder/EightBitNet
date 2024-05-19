// <copyright file="UnusedMemory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.IO;

    public class UnusedMemory(int size, byte unchanging) : Memory
    {
        private readonly int size = size;
        private readonly byte unchanging = unchanging;

        public override int Size => this.size;

        public override int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();

        public override int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();

        public override int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1) => throw new System.NotImplementedException();

        public override byte Peek(ushort address) => this.unchanging;

        protected override void Poke(ushort address, byte value) => throw new System.NotImplementedException();
    }
}
