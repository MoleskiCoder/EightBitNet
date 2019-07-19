namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using EightBit;

    public class RegisterState
    {
        public enum Register
        {
            AF, BC, DE, HL, SP, PC
        };

        public List<Register16> Registers { get; } = new List<Register16>();
        public bool Halted { get; private set; } = false;
        public int TStates { get; private set; } = -1;

        public void Read(StreamReader file)
        {
            this.ReadExternalState(file);
            this.ReadInternalState(file);
        }

        private void ReadInternalState(StreamReader file)
        {
            var line = file.ReadLine();
            var tokens = line.Split(new char[] { ' ', '\t' });

            this.Halted = Convert.ToInt32(tokens[0]) == 1;
            this.TStates = Convert.ToInt32(tokens[1]);
        }

        private void ReadExternalState(StreamReader file)
        {
            var line = file.ReadLine();
            foreach (var token in line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.Registers.Add(new Register16(Convert.ToUInt16(token, 16)));
            }
        }
    }
}
