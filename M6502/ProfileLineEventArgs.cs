namespace EightBit
{
    public sealed class ProfileLineEventArgs(ushort address, string source, long cycles, long count, Dictionary<int, long> cycleDistributions) : CycleCountedEventArgs(cycles, count)
    {
        public ushort Address { get; } = address;

        public string Source { get; } = source;

        public Dictionary<int, long> CycleDistributions { get; } = cycleDistributions;
    }
}