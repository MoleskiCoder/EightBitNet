// <copyright file="ProfileLineEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    using System;

    public class ProfileLineEventArgs : EventArgs
    {
        public ProfileLineEventArgs(string source, ulong cycles)
        {
            this.Source = source;
            this.Cycles = cycles;
        }

        public string Source { get; }

        public ulong Cycles { get; }
    }
}