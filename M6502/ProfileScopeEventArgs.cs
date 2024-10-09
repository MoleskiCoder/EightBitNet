namespace M6502
{
    public sealed class ProfileScopeEventArgs(int id, long cycles, long count) : CycleCountedEventArgs(cycles, count)
    {
        public int ID { get; } = id;
    }
}