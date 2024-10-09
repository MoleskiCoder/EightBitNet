namespace M6502
{
    public sealed class ProfileEventArgs(string output) : EventArgs
    {
        public string Output { get; } = output;
    }
}