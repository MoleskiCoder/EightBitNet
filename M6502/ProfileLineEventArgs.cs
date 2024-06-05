namespace EightBit
{
    public class ProfileLineEventArgs(string source, int cycles) : EventArgs
    {
        public string Source { get; } = source;

        public int Cycles { get; } = cycles;
    }
}