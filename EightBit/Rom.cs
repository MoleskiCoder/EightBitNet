// <copyright file="Rom.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Rom(int size = 0) : Memory
    {
        private byte[] _bytes = new byte[size];

        public override int Size => _bytes.Length;

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
            var maximumSize = Size - writeOffset;
            return Load(file, ref Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public override int Load(string path, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            var maximumSize = Size - writeOffset;
            return Load(path, ref Bytes(), writeOffset, readOffset, limit, maximumSize);
        }

        public override int Load(byte[] from, int writeOffset = 0, int readOffset = 0, int limit = -1)
        {
            ArgumentNullException.ThrowIfNull(from);

            if (limit < 0)
            {
                limit = Math.Min(from.Length, Size - readOffset);
            }

            var extent = limit + writeOffset;
            if (Size < extent)
            {
                var updated = new byte[extent];
                Array.Copy(Bytes(), updated, Size);
                Bytes() = updated;
            }

            Array.Copy(from, readOffset, Bytes(), writeOffset, limit);

            return limit;
        }

        public override byte Peek(ushort address) => Bytes()[address];

        protected ref byte[] Bytes() => ref _bytes;

        protected override void Poke(ushort address, byte value) => Bytes()[address] = value;
    }
}
