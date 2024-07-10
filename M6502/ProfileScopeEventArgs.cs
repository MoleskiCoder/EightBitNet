namespace EightBit
{
    public class ProfileScopeEventArgs(int id, long cycles, long count) : CycleCountedEventArgs(cycles, count)
    {
        public int ID { get; } = id;
    }
}