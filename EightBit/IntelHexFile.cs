﻿// <copyright file="IntelHexFile.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class IntelHexFile(string path)
    {
        private readonly string _path = path;
        private bool _eof;

        public IEnumerable<Tuple<ushort, byte[]>> Parse()
        {
            this._eof = false;
            using var reader = File.OpenText(this._path);
            while (!reader.EndOfStream && !this._eof)
            {
                var line = reader.ReadLine() ?? throw new InvalidOperationException("Early EOF detected");
                var parsed = this.Parse(line);
                if (parsed is not null)
                {
                    yield return parsed;
                }
            }

            if (!this._eof)
            {
                throw new InvalidOperationException("File is missing an EOF record");
            }
        }

        private static byte[] ParseDataRecord(string line, byte count)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(line);

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
            ArgumentNullException.ThrowIfNullOrEmpty(line);

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
                    this._eof = true;
                    return null;

                default:
                    throw new InvalidOperationException("Unhandled hex file record.");
            }
        }
    }
}
