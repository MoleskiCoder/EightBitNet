// <copyright file="PortEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public sealed class PortEventArgs : EventArgs
    {
        public PortEventArgs(byte value) => this.Port = value;

        public byte Port { get; }
    }
}
