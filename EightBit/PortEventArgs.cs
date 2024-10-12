// <copyright file="PortEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class PortEventArgs(byte value) : EventArgs
    {
        public byte Port { get; } = value;
    }
}
