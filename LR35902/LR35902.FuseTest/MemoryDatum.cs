namespace Fuse
{
    using System;
    using System.Collections.Generic;

    public class MemoryDatum
    {
        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Mask16;

        public List<byte> Bytes { get; } = new List<byte>();

        public void Parse(string line)
        {
            var tokens = line.Split(new char[] { ' ', '\t' });
            this.Parse(tokens);
        }

        public void Parse(string[] tokens)
        {
            this.Address = Convert.ToUInt16(tokens[0], 16);

            var finished = false;
            for (var i = 1; !finished && (i < tokens.Length); ++i)
            {
                var token = tokens[i];
                finished = token == "-1";
                if (!finished)
                {
                    this.Bytes.Add(Convert.ToByte(token, 16));
                }
            }
        }
    }
}
