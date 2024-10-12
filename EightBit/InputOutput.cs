// <copyright file="InputOutput.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class InputOutput
    {
        private readonly byte[] _input = new byte[0x100];
        private readonly byte[] _output = new byte[0x100];

        public event EventHandler<PortEventArgs>? ReadingPort;

        public event EventHandler<PortEventArgs>? ReadPort;

        public event EventHandler<PortEventArgs>? WritingPort;

        public event EventHandler<PortEventArgs>? WrittenPort;

        public byte Read(byte port) => this.ReadInputPort(port);

        public void Write(byte port, byte value) => this.WriteOutputPort(port, value);

        public byte ReadInputPort(byte port)
        {
            this.OnReadingPort(port);
            var value = this._input[port];
            this.OnReadPort(port);
            return value;
        }

        public void WriteInputPort(byte port, byte value) => this._input[port] = value;

        public byte ReadOutputPort(byte port) => this._output[port];

        public void WriteOutputPort(byte port, byte value)
        {
            this.OnWritingPort(port);
            this._output[port] = value;
            this.OnWrittenPort(port);
        }

        private void OnReadingPort(byte port) => ReadingPort?.Invoke(this, new PortEventArgs(port));

        private void OnReadPort(byte port) => ReadPort?.Invoke(this, new PortEventArgs(port));

        private void OnWritingPort(byte port) => WritingPort?.Invoke(this, new PortEventArgs(port));

        private void OnWrittenPort(byte port) => WrittenPort?.Invoke(this, new PortEventArgs(port));
    }
}
