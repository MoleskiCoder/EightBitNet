namespace EightBit
{
    public sealed class ProfileScopeEventArgs(int id, long cycles, long count) : CycleCountedEventArgs(cycles, count)
    {
        public int ID { get; } = id;
    }
}