namespace EightBit
{
    using System;

    public sealed class InputOutput
    {
        private byte[] input;
        private byte[] output;

        public InputOutput()
        {
            input = new byte[0x100];
            output = new byte[0x100];
        }

        public event EventHandler<PortEventArgs> ReadingPort;
        public event EventHandler<PortEventArgs> ReadPort;

        public event EventHandler<PortEventArgs> WritingPort;
        public event EventHandler<PortEventArgs> WrittenPort;

        byte Read(byte port) { return ReadInputPort(port); }
        void Write(byte port, byte value) { WriteOutputPort(port, value); }

        byte ReadInputPort(byte port)
        {
            OnReadingPort(port);
            var value = input[port];
            OnReadPort(port);
            return value;
        }

        void WriteInputPort(byte port, byte value) => input[port] = value;

        byte ReadOutputPort(byte port) => output[port];

        void WriteOutputPort(byte port, byte value)
        {
            OnWritingPort(port);
            output[port] = value;
            OnWrittenPort(port);
        }

        private void OnReadingPort(byte port) => ReadingPort?.Invoke(this, new PortEventArgs(port));
        private void OnReadPort(byte port) => ReadPort?.Invoke(this, new PortEventArgs(port));

        private void OnWritingPort(byte port) => WritingPort?.Invoke(this, new PortEventArgs(port));
        private void OnWrittenPort(byte port) => WrittenPort?.Invoke(this, new PortEventArgs(port));
    }
}
