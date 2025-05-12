// <copyright file="ProfileScopeEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    public class ProfileScopeEventArgs(string scope, ulong cycles, ulong count) : EventArgs
    {
        public string Scope { get; } = scope;

        public ulong Cycles { get; } = cycles;

        public ulong Count { get; } = count;
    }
}