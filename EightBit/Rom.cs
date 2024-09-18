// <copyright file="Rom.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;
    using System.IO;

    public class Rom(int size = 0) : Memory
    {
        private byte[] bytes = new byte[size];

        public override int Size => this.bytes.Length;

        public static int Load(Stream file, ref byte[] output, int writeOffset = 0, int readOffset = 0, int limit = -1, int maximumSize = -1)
        {
            var size = (int)file.Length;

            if ((maximumSize > 0) && ((size - readOffset) > maximumSize))
            {
                throw new InvalidOperationException("File is too large");
            }

            if ((limit < 0) || (limit > size))
            {
                limit = (int)size;
            }

            var extent = limit + writeOffset;
            if (output.Length < extent)
            {
                var updated = new byte[extent];
                Array.Copy(output, updated, output.Length);
                output = updated;
            }

            file.Seek(readOffset, SeekOrigin.Begin);
            using (var reader = new BinaryReader(file))
            {
                reader.Read(output, writeOffset, limit);
            }

            return size;
        }

        public static int Load(string path, ref byte[] output, int writeOffset = 0, int readOffset = 0, int limit = -1, int maximumSize = -1)
        {
            using var file = File.Open(path, FileMode.Open);
            return Load(file, ref output, writeOffset, readOffset, limit, maximumSize);
        }

        public override int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            var maximumSize = this.Size - writeOffset;
            return Load(file, ref this.Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public override int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            var maximumSize = this.Size - writeOffset;
            return Load(path, ref this.Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public override int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            if (limit < 0)
            {
                limit = Math.Min(from.Length, this.Size - readOffset);
            }

            var extent = limit + writeOffset;
            if (this.Size < extent)
            {
                var updated = new byte[extent];
                Array.Copy(this.Bytes(), updated, this.Size);
                this.Bytes() = updated;
            }

            Array.Copy(from, readOffset, this.Bytes(), writeOffset, limit);

            return limit;
        }

        public override byte Peek(ushort address) => this.Bytes()[address];

        protected ref byte[] Bytes() => ref this.bytes;

        protected override void Poke(ushort address, byte value) => this.Bytes()[address] = value;
    }
}
