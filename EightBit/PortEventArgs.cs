// <copyright file="PortEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public sealed class PortEventArgs : EventArgs
    {
        private byte port;

        public PortEventArgs(byte value) => this.port = value;

        public byte Port => this.port;
    }
}
