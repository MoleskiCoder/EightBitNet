// <copyright file="InputOutput.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.Diagnostics;

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


        public byte Read(Register16 port) => this.ReadInputPort(port);

        public byte ReadInputPort(Register16 port)
        {
            Debug.Assert(port is not null, "port cannot be null");
            this.ReadingPort?.Invoke(this, new PortEventArgs(port));
            var value = this._input[port.Joined];
            this.ReadPort?.Invoke(this, new PortEventArgs(port));
            return value;
        }

        public void WriteInputPort(ushort port, byte value) => this._input[port] = value;

        public void WriteInputPort(Register16 port, byte value)
        {
            Debug.Assert(port is not null, "port cannot be null");
            this.WriteInputPort(port.Joined, value);
        }

        public void Write(Register16 port, byte value) => this.WriteOutputPort(port, value);

        public void WriteOutputPort(Register16 port, byte value)
        {
            Debug.Assert(port is not null, "port cannot be null");
            this.WritingPort?.Invoke(this, new PortEventArgs(port));
            this._output[port.Joined] = value;
            this.WrittenPort?.Invoke(this, new PortEventArgs(port));
        }

        public byte ReadOutputPort(ushort port) => this._output[port];

        public byte ReadOutputPort(Register16 port)
        {
            Debug.Assert(port is not null, "port cannot be null");
            return this.ReadOutputPort(port.Joined);
        }
    }
}
