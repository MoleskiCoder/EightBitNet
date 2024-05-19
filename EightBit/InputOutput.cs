// <copyright file="InputOutput.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public sealed class InputOutput
    {
        private readonly byte[] input = new byte[0x100];
        private readonly byte[] output = new byte[0x100];

        public InputOutput()
        {
        }

        public event EventHandler<PortEventArgs>? ReadingPort;

        public event EventHandler<PortEventArgs>? ReadPort;

        public event EventHandler<PortEventArgs>? WritingPort;

        public event EventHandler<PortEventArgs>? WrittenPort;

        public byte Read(byte port) => this.ReadInputPort(port);

        public void Write(byte port, byte value) => this.WriteOutputPort(port, value);

        public byte ReadInputPort(byte port)
        {
            this.OnReadingPort(port);
            var value = this.input[port];
            this.OnReadPort(port);
            return value;
        }

        public void WriteInputPort(byte port, byte value) => this.input[port] = value;

        public byte ReadOutputPort(byte port) => this.output[port];

        public void WriteOutputPort(byte port, byte value)
        {
            this.OnWritingPort(port);
            this.output[port] = value;
            this.OnWrittenPort(port);
        }

        private void OnReadingPort(byte port) => this.ReadingPort?.Invoke(this, new PortEventArgs(port));

        private void OnReadPort(byte port) => this.ReadPort?.Invoke(this, new PortEventArgs(port));

        private void OnWritingPort(byte port) => this.WritingPort?.Invoke(this, new PortEventArgs(port));

        private void OnWrittenPort(byte port) => this.WrittenPort?.Invoke(this, new PortEventArgs(port));
    }
}
