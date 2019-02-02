using System;

namespace EightBit
{
    public sealed class PortEventArgs : EventArgs
    {
        private byte port;

        public PortEventArgs(byte value)
        {
            port = value;
        }

        public byte Port { get { return port; } }
    }
}
