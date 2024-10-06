// <copyright file="TestEvent.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using EightBit;

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

        public TestEvent(int cycles, string specifier, ushort address)
        : this(cycles, specifier, address, (byte)Mask.Eight) => this.ContentionEvent = true;

        public int Cycles => this.cycles;

        public string Specifier { get; private set; }

        public ushort Address { get; private set; } = (ushort)EightBit.Mask.Sixteen;

        public byte Value { get; private set; } = (byte)EightBit.Mask.Eight;

        private bool ContentionEvent { get; set; } = false;

        public bool TryParse(Lines lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            var returned = this.TryParseLine(lines.ReadLine());
            if (!returned)
            {
                lines.UnreadLine();
            }

            return returned;
        }

        public override string ToString()
        {
            var possible = $"Cycles = {this.Cycles}, Specifier = {this.Specifier}, Address = {this.Address:X4}";
            if (!this.ContentionEvent)
            {
                possible += $", Value = {this.Value:X2}";
            }

            return possible;
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
