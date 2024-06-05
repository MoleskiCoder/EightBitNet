namespace EightBit
{
    public class ProfileScopeEventArgs(string scope, int cycles, int count) : EventArgs
    {
        public string Scope { get; } = scope;

        public int Cycles { get; } = cycles;

        public int Count { get; } = count;
    }
}