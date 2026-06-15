// <copyright file="ProfileLineEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    public class ProfileLineEventArgs(string source, long cycles) : EventArgs
    {
        public string Source { get; } = source;

        public long Cycles { get; } = cycles;
    }
}