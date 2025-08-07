// <copyright file="PortEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class PortEventArgs(Register16 value) : EventArgs
    {
        public Register16 Port { get; } = value;
    }
}
