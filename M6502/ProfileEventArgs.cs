namespace EightBit
{
    public class ProfileEventArgs(string output) : EventArgs
    {
        public string Output { get; } = output;
    }
}