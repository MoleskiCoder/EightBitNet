// <copyright file="IntelHexFile.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class IntelHexFile(string path)
    {
        private readonly string _path = path;
        private bool _eof;
        private ushort _extendedSegmentAddress;
        private ushort _codeSegment;
        private ushort _programCounter;
        private ushort _extendedLinearAddressHigh;
        private uint _startLinearAddress;

        private bool EOF => this._eof;

        public ushort ExtendedSegmentAddress => this._extendedSegmentAddress;

        public ushort CodeSegment => this._codeSegment;

        public ushort ProgramCounter => this._programCounter;

        public ushort ExtendedLinearAddressHigh => this._extendedLinearAddressHigh;

        public uint StartLinearAddress => this._startLinearAddress;

        public IEnumerable<Tuple<ushort, byte[]>> Parse()
        {
            this._eof = false;
            using var reader = File.OpenText(this._path);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine() ?? throw new InvalidDataException("Early EOF detected");
                var parsed = this.Parse(line);
                if (parsed is not null)
                {
                    yield return parsed;
                }
            }

            if (!this.EOF)
            {
                throw new InvalidDataException("File is missing an EOF record");
            }
        }

        private static byte CalculateChecksum(byte[] data)
        {
            ushort sum = 0;
            foreach (var datum in data)
                sum += datum;
            return (byte)-Chip.LowByte(sum);
        }

        private static void VerifyChecksum(byte desired, byte[] data)
        {
            var calculated = CalculateChecksum(data);
            if (calculated != desired)
                throw new InvalidDataException("Checksum failure");
        }

        private Tuple<ushort, byte[]>? Parse(string line)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(line);

            if (this._eof)
                throw new InvalidDataException("Reading past EOF marker");

            var colon = line[..1];
            if (colon != ":")
                throw new InvalidDataException("Record does not begin with a colon");

            var useful = line[1..];
            var characterCount = useful.Length;
            if (characterCount % 2 != 0)
                throw new InvalidDataException("Record data cannot be parsed as two character hex numbers");

            var byteCount = characterCount / 2;
            var bytes = new byte[byteCount];
            for (int index = 0; index < byteCount; ++index)
            {
                var position = index * 2;
                var extracted = useful.Substring(position, 2);
                var datum = Convert.ToByte(extracted, 16);
                bytes[index] = datum;
            }

            if (byteCount < 5)
                throw new InvalidDataException("Not enough bytes in record");

            var count = bytes[0];
            var address = Chip.MakeShort(bytes[2], bytes[1]);
            var type = bytes[3];
            var lastIndex = byteCount - 1;
            var checksum = bytes[lastIndex];
            var data = bytes[4..lastIndex];
            if (count != data.Length)
                throw new InvalidDataException("Record count does not match data length");

            VerifyChecksum(checksum, bytes[..lastIndex]); // All data, except the desired checksum (obviously!)

            switch (type)
            {
                // Data
                case 0x00:
                    return new Tuple<ushort, byte[]>(address, data);

                // End-of-file
                case 0x01:
                    if (this.EOF)
                        throw new InvalidDataException("HEX file contains multiple EOF markers");
                    this._eof = true;
                    return null;

                // Extended segment address
                case 0x02:
                    if (count != 2)
                        throw new InvalidDataException("Byte count is invalid for field type 2 (Extended Segment Address)");
                    this._extendedSegmentAddress = Chip.MakeShort(data[1], bytes[0]);
                    return null;

                // Start segment address
                case 0x03:
                    if (count != 4)
                        throw new InvalidDataException("Byte count is invalid for field type 3 (Start Segment Address)");
                    this._codeSegment = Chip.MakeShort(data[1], bytes[0]);
                    this._programCounter = Chip.MakeShort(data[2], bytes[3]);
                    return null;

                // Extended linear address
                case 0x04:
                    if (count != 2)
                        throw new InvalidDataException("Byte count is invalid for field type 4 (Extended Linear Address)");
                    this._extendedLinearAddressHigh = Chip.MakeShort(data[1], bytes[0]);
                    return null;

                // Start linear address
                case 0x05:
                    if (count != 4)
                        throw new InvalidDataException("Byte count is invalid for field type 5 (Start Linear Address)");
                    {
                        var high = Chip.MakeShort(data[1], bytes[0]);
                        var low = Chip.MakeShort(data[2], bytes[3]);
                        this._startLinearAddress = Chip.MakeInteger(low, high);
                    }
                    return null;

                default:
                    throw new InvalidOperationException("Unhandled HEX file record.");
            }
        }
    }
}
