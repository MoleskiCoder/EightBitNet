// <copyright file="AbstractRegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using EightBit;

    public abstract class AbstractRegisterState
    {
        private readonly List<Register16> registers = new List<Register16>();

        public ReadOnlyCollection<Register16> Registers => this.MutableRegisters.AsReadOnly();

        public bool Halted { get; protected set; } = false;

        public int TStates { get; protected set; } = -1;

        protected List<Register16> MutableRegisters => this.registers;

        public void Parse(Lines lines)
        {
            this.ParseExternalState(lines);
            this.ParseInternalState(lines);
        }

        protected void ParseInternalState(Lines lines) => this.ParseInternalState(lines.ReadLine());

        protected virtual void ParseInternalState(string line)
        {
            var tokens = line.Split(new char[] { ' ', '\t' });
            this.ParseInternalState(tokens);
        }

        protected abstract void ParseInternalState(string[] tokens);

        protected virtual void ParseExternalState(Lines lines)
        {
            var line = lines.ReadLine();
            foreach (var token in line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.registers.Add(new Register16(Convert.ToUInt16(token, 16)));
            }
        }
    }
}
