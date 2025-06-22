// <copyright file="InputOutput.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public sealed class InputOutput
    {
        private readonly byte[] _input = new byte[0x10000];
        private readonly byte[] _output = new byte[0x10000];

        public event EventHandler<PortEventArgs>? ReadingPort;

        public event EventHandler<PortEventArgs>? ReadPort;

        public event EventHandler<PortEventArgs>? WritingPort;

        public event EventHandler<PortEventArgs>? WrittenPort;

        public byte Read(ushort port) => this.ReadInputPort(port);

        public byte Read(Register16 port) => this.Read(port.Word);

        public void Write(ushort port, byte value) => this.WriteOutputPort(port, value);

        public void Write(Register16 port, byte value) => this.Write(port.Word, value);

        public byte ReadInputPort(ushort port)
        {
            ReadingPort?.Invoke(this, new PortEventArgs(port));
            try
            {
                return this._input[port];
            }
            finally
            {
                ReadPort?.Invoke(this, new PortEventArgs(port));
            }
        }

        public void WriteInputPort(ushort port, byte value) => this._input[port] = value;

        public byte ReadOutputPort(ushort port) => this._output[port];

        public void WriteOutputPort(ushort port, byte value)
        {
            WritingPort?.Invoke(this, new PortEventArgs(port));
            try
            {
                this._output[port] = value;
            }
            finally
            {
                WrittenPort?.Invoke(this, new PortEventArgs(port));
            }
        }
    }
}
