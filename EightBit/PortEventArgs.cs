// <copyright file="PortEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class PortEventArgs(ushort value) : EventArgs
    {
        public ushort Port { get; } = value;
    }
}
