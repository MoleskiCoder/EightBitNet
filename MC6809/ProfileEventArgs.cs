// <copyright file="ProfileEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    using System;

    public class ProfileEventArgs : EventArgs
    {
        public ProfileEventArgs(string output) => this.Output = output;

        public string Output { get; }
    }
}