﻿// <copyright file="IntelHexFile.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class IntelHexFile(string path) : IDisposable
    {
        private readonly StreamReader reader = File.OpenText(path);
        private bool eof;
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<Tuple<ushort, byte[]>> Parse()
        {
            this.eof = false;
            while (!this.reader.EndOfStream && !this.eof)
            {
                var line = this.reader.ReadLine() ?? throw new InvalidOperationException("Early EOF detected");
                var parsed = this.Parse(line);
                if (parsed != null)
                {
                    yield return parsed;
                }
            }

            if (!this.eof)
            {
                throw new InvalidOperationException("File is missing an EOF record");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.reader.Dispose();
                }

                this.disposed = true;
            }
        }

        private static byte[] ParseDataRecord(string line, byte count)
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new ArgumentNullException(nameof(line));
            }

            var requiredLength = 9 + 2 + (count * 2);
            if (line.Length != requiredLength)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Invalid hex file: line is not the required length");
            }

            var data = new byte[count];
            for (var i = 0; i < count; ++i)
            {
                var position = 9 + (i * 2);
                var extracted = line.Substring(position, 2);
                data[i] = Convert.ToByte(extracted, 16);
            }

            return data;
        }

        private Tuple<ushort, byte[]>? Parse(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new ArgumentNullException(nameof(line));
            }

            var colon = line[..1];
            if (colon != ":")
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Invalid hex file: line does not begin with a colon");
            }

            var countString = line.Substring(1, 2);
            var count = Convert.ToByte(countString, 16);

            var addressString = line.Substring(3, 4);
            var address = Convert.ToUInt16(addressString, 16);

            var recordTypeString = line.Substring(7, 2);
            switch (Convert.ToByte(recordTypeString, 16))
            {
                case 0x00:
                    return new Tuple<ushort, byte[]>(address, ParseDataRecord(line, count));

                case 0x01:
                    this.eof = true;
                    return null;

                default:
                    throw new InvalidOperationException("Unhandled hex file record.");
            }
        }
    }
}
