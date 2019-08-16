// <copyright file="TestEvent.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("Cycles = {Cycles}, Specifier = {Specifier}, Address = {Address}, Value = {Value}")]
    public class TestEvent
    {
        private int cycles;

        public TestEvent()
        {
        }

        public TestEvent(int cycles, string specifier, ushort address, byte value)
        {
            this.cycles = cycles;
            this.Specifier = specifier;
            this.Address = address;
            this.Value = value;
        }

        public int Cycles => this.cycles;

        public string Specifier { get; private set; }

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Mask16;

        public byte Value { get; private set; } = (byte)EightBit.Mask.Mask8;

        public bool TryParse(Lines lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            var returned = this.TryParseLine(lines.ReadLine());
            if (!returned)
            {
                lines.UnreadLine();
            }

            return returned;
        }

        private bool TryParseLine(string line)
        {
            var split = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return this.TryParseLine(split);
        }

        private bool TryParseLine(string[] tokens)
        {
            if (!int.TryParse(tokens[0], out this.cycles))
            {
                return false;
            }

            this.Specifier = tokens[1];

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
                    return false;
            }

            return true;
        }
    }
}
