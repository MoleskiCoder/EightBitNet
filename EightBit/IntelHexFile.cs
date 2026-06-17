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

        public bool Debug { get; set; }

        public bool Strict { get; set; }

        private bool EOF => this._eof;

        public ushort ExtendedSegmentAddress => this._extendedSegmentAddress;

        public ushort CodeSegment => this._codeSegment;

        public ushort ProgramCounter => this._programCounter;

        public ushort ExtendedLinearAddressHigh => this._extendedLinearAddressHigh;

        public uint StartLinearAddress => this._startLinearAddress;

        private void Log(string message)
        {
            if (this.Debug)
                Console.WriteLine($"Intel Hex File: {message}");
        }

        private void MaybeThrow(string message)
        {
            if (this.Strict)
                throw new InvalidDataException(message);
            Log(message);
        }

        private bool Check(bool failure, string message)
        {
            if (failure)
                this.MaybeThrow(message);
            return failure;
        }

        public IEnumerable<Tuple<ushort, byte[]>> Parse()
        {
            this._eof = false;

            this.Log($"Opening: {this._path}");
            using var reader = File.OpenText(this._path);

            this.Log("Reading");
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine() ?? throw new InvalidDataException("Early EOF detected");
                var parsed = this.Parse(line);
                if (parsed is not null)
                    yield return parsed;
            }
            this.Check(!this.EOF, "File is missing an EOF record");
            this.Log("Finished");
        }

        private static byte CalculateChecksum(byte[] data)
        {
            ushort sum = 0;
            foreach (var datum in data)
                sum += datum;
            return (byte)-Chip.LowByte(sum);
        }

        private bool VerifyChecksum(byte desired, byte[] data)
        {
            var calculated = CalculateChecksum(data);
            return !this.Check(calculated != desired, "Checksum failure"); ;
        }

        private Tuple<ushort, byte[]>? Parse(string line)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(line);

            if (this.Check(this.EOF, "Reading past EOF marker"))
                return null;

            var colon = line[..1];
            if (this.Check(colon != ":", "Record does not begin with a colon"))
                return null;

            var useful = line[1..];
            var characterCount = useful.Length;
            if (this.Check(characterCount % 2 != 0, "Record data cannot be parsed as two character hex numbers"))
                return null;

            var byteCount = characterCount / 2;
            var bytes = new byte[byteCount];
            for (int index = 0; index < byteCount; ++index)
            {
                var position = index * 2;
                var extracted = useful.Substring(position, 2);
                var datum = Convert.ToByte(extracted, 16);
                bytes[index] = datum;
            }

            if (this.Check(byteCount < 5, "Not enough bytes in record"))
                return null;

            var count = bytes[0];
            var address = Chip.MakeShort(bytes[2], bytes[1]);
            var type = bytes[3];
            var lastIndex = byteCount - 1;
            var checksum = bytes[lastIndex];
            var data = bytes[4..lastIndex];
            if (this.Check(count != data.Length, "Record count does not match data length"))
                return null;

            // All data, except the desired checksum (obviously!)
            if (!VerifyChecksum(checksum, bytes[..lastIndex]))
                return null;

            switch (type)
            {
                // Data
                case 0x00:
                    this.Log($"Type: {type:x2}, Address: {address:x4}, Count: {count}");
                    return new Tuple<ushort, byte[]>(address, data);

                // End-of-file
                case 0x01:
                    this.Check(this.EOF, "HEX file contains multiple EOF markers");
                    this._eof = true;
                    this.Log($"Type: {type:x2}, EOF");
                    return null;

                // Extended segment address
                case 0x02:
                    if (this.Check(count != 2, "Byte count is invalid for field type 2 (Extended Segment Address)"))
                        return null;
                    this._extendedSegmentAddress = Chip.MakeShort(data[1], bytes[0]);
                    this.Log($"Type: {type:x2}, Extended segment address: {this.ExtendedSegmentAddress:x4}");
                    return null;

                // Start segment address
                case 0x03:
                    if (this.Check(count != 4, "Byte count is invalid for field type 3 (Start Segment Address)"))
                        return null;
                    this._codeSegment = Chip.MakeShort(data[1], bytes[0]);
                    this._programCounter = Chip.MakeShort(data[2], bytes[3]);
                    this.Log($"Type: {type:x2}, CS: {this.CodeSegment:x4}, PC: {this.ProgramCounter:x4}");
                    return null;

                // Extended linear address
                case 0x04:
                    if (this.Check(count != 2, "Byte count is invalid for field type 4 (Extended Linear Address)"))
                        return null;
                    this._extendedLinearAddressHigh = Chip.MakeShort(data[1], bytes[0]);
                    this.Log($"Type: {type:x2}, Extended linear address high: {this.ExtendedLinearAddressHigh:x4}");
                    return null;

                // Start linear address
                case 0x05:
                    if (this.Check(count != 4, "Byte count is invalid for field type 5 (Start Linear Address)"))
                        return null;
                    {
                        var high = Chip.MakeShort(data[1], bytes[0]);
                        var low = Chip.MakeShort(data[2], bytes[3]);
                        this._startLinearAddress = Chip.MakeInteger(low, high);
                    }
                    this.Log($"Type: {type:x2}, Start linear address: {this.StartLinearAddress:x8}");
                    return null;

                default:
                    this.MaybeThrow("Unhandled HEX file record.");
                    return null;
            }
        }
    }
}
