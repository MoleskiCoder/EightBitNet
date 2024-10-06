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

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Sixteen;

        public ReadOnlyCollection<byte> Bytes => this.bytes.AsReadOnly();

        public void Parse(string line)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(line);
            var tokens = line.Split(new char[] { ' ', '\t' });
            this.Parse(tokens);
        }

        public void Parse(string[] tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);

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
