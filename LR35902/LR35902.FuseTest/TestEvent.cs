namespace Fuse
{
    using System;
    using System.IO;

    public class TestEvent
    {
        public bool Valid { get; private set; } = false;

        public int Cycles { get; private set; } = -1;

        public string Specifier { get; private set; }

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Mask16;

        public ushort Value { get; private set; } = (byte)EightBit.Mask.Mask8;

        public void Read(StreamReader file)
        {
            var prior = file.BaseStream.Position;
            this.ParseLine(file.ReadLine());
            if (!this.Valid)
            {
                file.BaseStream.Seek(prior, SeekOrigin.Begin);
            }
        }

        private void ParseLine(string line)
        {
            var split = line.Split(new char[] { ' ', '\t' });
            this.ParseLine(split);
        }

        private void ParseLine(string[] tokens)
        {
            this.Cycles = int.Parse(tokens[0]);
            this.Specifier = tokens[1];

            this.Valid = true;
            switch (this.Specifier)
            {
                case "MR":
                case "MW":
                    this.Address = Convert.ToUInt16(tokens[2], 16);
                    this.Value = Convert.ToByte(tokens[3], 16);
                    break;

                case "MC":
                case "PC":
                    this.Address = Convert.ToUInt16(tokens[2], 16);
                    break;

                case "PR":
                case "PW":
                    this.Address = Convert.ToUInt16(tokens[2], 16);
                    this.Value = Convert.ToByte(tokens[3], 16);
                    break;

                default:
                    this.Valid = false;
                    break;
            }
        }
    }
}
