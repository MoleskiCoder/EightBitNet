namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using EightBit;

    public class RegisterState
    {
        public enum Register
        {
            AF, BC, DE, HL, SP, PC
        };

        private readonly List<Register16> registers = new List<Register16>();

        public ReadOnlyCollection<Register16> Registers => this.registers.AsReadOnly();

        public bool Halted { get; private set; } = false;

        public int TStates { get; private set; } = -1;

        public void Parse(Lines lines)
        {
            this.ParseExternalState(lines);
            this.ParseInternalState(lines);
        }

        private void ParseInternalState(Lines lines) => this.ParseInternalState(lines.ReadLine());

        private void ParseInternalState(string line)
        {
            var tokens = line.Split(new char[] { ' ', '\t' });
            this.ParseInternalState(tokens);
        }

        private void ParseInternalState(string[] tokens)
        {
            this.Halted = Convert.ToInt32(tokens[0], CultureInfo.InvariantCulture) == 1;
            this.TStates = Convert.ToInt32(tokens[1], CultureInfo.InvariantCulture);
        }

        private void ParseExternalState(Lines lines)
        {
            var line = lines.ReadLine();
            foreach (var token in line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.registers.Add(new Register16(Convert.ToUInt16(token, 16)));
            }
        }
    }
}
