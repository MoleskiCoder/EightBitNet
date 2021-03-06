﻿// <copyright file="ProfileScopeEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    using System;

    public class ProfileScopeEventArgs : EventArgs
    {
        public ProfileScopeEventArgs(string scope, ulong cycles, ulong count)
        {
            this.Scope = scope;
            this.Cycles = cycles;
            this.Count = count;
        }

        public string Scope { get; }

        public ulong Cycles { get; }

        public ulong Count { get; }
    }
}