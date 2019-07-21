// <copyright file="MemoryDatum.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class MemoryDatum
    {
        private readonly List<byte> bytes = new List<byte>();

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Mask16;

        public ReadOnlyCollection<byte> Bytes => this.bytes.AsReadOnly();

        public void Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentNullException(nameof(line));
            }

            var tokens = line.Split(new char[] { ' ', '\t' });
            this.Parse(tokens);
        }

        public void Parse(string[] tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            this.Address = Convert.ToUInt16(tokens[0], 16);

            var finished = false;
            for (var i = 1; !finished && (i < tokens.Length); ++i)
            {
                var token = tokens[i];
                finished = token == "-1";
                if (!finished)
                {
                    this.bytes.Add(Convert.ToByte(token, 16));
                }
            }
        }
    }
}
