namespace Fuse
{
    using System;
    using System.IO;

    public class TestEvent
    {
        private int cycles;

        public bool Valid { get; private set; } = false;

        public int Cycles => this.cycles;

        public string Specifier { get; private set; }

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Mask16;

        public byte Value { get; private set; } = (byte)EightBit.Mask.Mask8;

        public void Parse(Lines lines)
        {
            this.ParseLine(lines.ReadLine());
            if (!this.Valid)
            {
                lines.UnreadLine();
            }
        }

        private void ParseLine(string line)
        {
            var split = line.Split(new char[] { ' ', '\t' });
            this.ParseLine(split);
        }

        private void ParseLine(string[] tokens)
        {
            this.Valid = int.TryParse(tokens[0], out this.cycles);
            if (!this.Valid)
            {
                return;
            }

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
