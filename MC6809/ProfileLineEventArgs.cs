// <copyright file="ProfileLineEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    public class ProfileLineEventArgs(string source, ulong cycles) : EventArgs
    {
        public string Source { get; } = source;

        public ulong Cycles { get; } = cycles;
    }
}