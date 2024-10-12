// <copyright file="AbstractRegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using EightBit;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public abstract class AbstractRegisterState
    {
        public ReadOnlyCollection<Register16> Registers => this.MutableRegisters.AsReadOnly();

        public bool Halted { get; protected set; }

        public int TStates { get; protected set; } = -1;

        protected List<Register16> MutableRegisters { get; } = [];

        private static readonly char[] separator = [' ', '\t'];

        public void Parse(Lines lines)
        {
            this.ParseExternalState(lines);
            this.ParseInternalState(lines);
        }

        protected void ParseInternalState(Lines lines)
        {
            ArgumentNullException.ThrowIfNull(lines);
            this.ParseInternalState(lines.ReadLine());
        }

        protected virtual void ParseInternalState(string? line)
        {
            ArgumentNullException.ThrowIfNull(line);
            var tokens = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.ParseInternalState(tokens);
        }

        protected abstract void ParseInternalState(string[] tokens);

        protected virtual void ParseExternalState(Lines lines)
        {
            ArgumentNullException.ThrowIfNull(lines);
            var line = lines.ReadLine();
            foreach (var token in line.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                this.MutableRegisters.Add(new Register16(Convert.ToUInt16(token, 16)));
            }
        }
    }
}
