namespace EightBit
{
    public class ProfileLineEventArgs(string source, long cycles, long count) : CycleCountedEventArgs(cycles, count)
    {
        public string Source { get; } = source;
    }
}