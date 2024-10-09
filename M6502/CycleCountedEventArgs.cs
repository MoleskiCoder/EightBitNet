namespace M6502
{
    public class CycleCountedEventArgs(long cycles, long count) : EventArgs
    {
        public long Cycles { get; } = cycles;

        public long Count { get; } = count;
    }
}