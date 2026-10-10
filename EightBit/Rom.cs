// <copyright file="Rom.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;

    public class Rom(int size = 0) : IMemory
    {
        private byte[] _bytes = new byte[size];

        public int Size => this._bytes.Length;

        public static int Load(Stream file, ref byte[] output, int writeOffset = 0, int readOffset = 0, int limit = -1, int maximumSize = -1)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(output);

            var size = (int)file.Length;

            if ((maximumSize > 0) && ((size - readOffset) > maximumSize))
            {
                throw new InvalidOperationException("File is too large");
            }

            if ((limit < 0) || (limit > size))
            {
                limit = size;
            }

            var extent = limit + writeOffset;
            if (output.Length < extent)
            {
                var updated = new byte[extent];
                Array.Copy(output, updated, output.Length);
                output = updated;
            }

            var position = file.Seek(readOffset, SeekOrigin.Begin);
            Debug.Assert(position == readOffset);
            using (var reader = new BinaryReader(file))
            {
                var actual = reader.Read(output, writeOffset, limit);
                Debug.Assert(actual <= limit);
            }

            return size;
        }

        public static int Load(string path, ref byte[] output, int writeOffset = 0, int readOffset = 0, int limit = -1, int maximumSize = -1)
        {
            using var file = File.Open(path, FileMode.Open);
            return Load(file, ref output, writeOffset, readOffset, limit, maximumSize);
        }

        public int Load(FileStream file, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            var maximumSize = this.Size - writeOffset;
            return Load(file, ref this.Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            var maximumSize = this.Size - writeOffset;
            return Load(path, ref this.Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            ArgumentNullException.ThrowIfNull(from);

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

        public byte Peek(ushort address) => this.Bytes()[address];

        public void Poke(ushort address, byte value) => this.Bytes()[address] = value;

        public virtual ref byte Reference(ushort address) => throw new NotImplementedException();

        protected ref byte[] Bytes() => ref this._bytes;
    }
}
