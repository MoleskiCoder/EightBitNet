// <copyright file="IntelHexFile.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class IntelHexFile : IDisposable
    {
        private readonly StreamReader reader;
        private bool disposed = false;

        public IntelHexFile(string path) => this.reader = File.OpenText(path);

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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

        public IEnumerable<Tuple<ushort, byte[]>> Parse()
        {
            var eof = false;
            while (!this.reader.EndOfStream && !eof)
            {
                var line = this.reader.ReadLine();
                var parsed = this.Parse(line);
                eof = parsed == null;
                if (!eof)
                {
                    yield return parsed;
                }
            }
        }

        private Tuple<ushort, byte[]> Parse(string line)
        {
            var colon = line.Substring(0, 1);
            if (colon != ":")
            {
                throw new System.InvalidOperationException("Invalid hex file: line does not begin with a colon");
            }

            var countString = line.Substring(1, 2);
            var count = Convert.ToByte(countString, 16);

            var addressString = line.Substring(3, 4);
            var address = Convert.ToUInt16(addressString, 16);

            var recordTypeString = line.Substring(7, 2);
            var recordType = Convert.ToByte(recordTypeString, 16);

            switch (recordType)
            {
                case 0x00:
                    return ParseDataRecord(line, address, count);

                case 0x01:
                    return null;

                default:
                    throw new InvalidOperationException("Unhandled hex file record.");
            }
        }

        private static Tuple<ushort, byte[]> ParseDataRecord(string line, ushort address, byte count)
        {
            var data = new byte[count];
            var requiredLength = 9 + 2 + (count * 2);
            if (line.Length != requiredLength)
            {
                throw new InvalidOperationException("Invalid hex file: line is not the required length");
            }

            for (var i = 0; i < count; ++i)
            {
                var position = 9 + (i * 2);
                var datumString = line.Substring(position, 2);
                data[i] = Convert.ToByte(datumString, 16);
            }

            return new Tuple<ushort, byte[]>(address, data);
        }
    }
}
