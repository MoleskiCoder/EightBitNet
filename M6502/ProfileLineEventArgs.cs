namespace EightBit
{
    public class ProfileLineEventArgs(string source, int cycles, int count) : EventArgs
    {
        public string Source { get; } = source;

        public int Cycles { get; } = cycles;

        public int Count { get; } = count;
    }
}