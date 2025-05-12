// <copyright file="ProfileEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace MC6809
{
    public class ProfileEventArgs(string output) : EventArgs
    {
        public string Output { get; } = output;
    }
}