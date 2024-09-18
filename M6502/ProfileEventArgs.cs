namespace EightBit
{
    public sealed class ProfileEventArgs(string output) : EventArgs
    {
        public string Output { get; } = output;
    }
}