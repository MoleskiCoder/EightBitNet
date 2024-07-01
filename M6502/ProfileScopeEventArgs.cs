namespace EightBit
{
    public class ProfileScopeEventArgs(int id, int cycles, int count) : EventArgs
    {
        public int ID { get; } = id;

        public int Cycles { get; } = cycles;

        public int Count { get; } = count;
    }
}