namespace EightBit
{
    using System;

    public class ProfileEventArgs : EventArgs
    {
        public ProfileEventArgs(string output) => this.Output = output;

        public string Output { get; }
    }
}