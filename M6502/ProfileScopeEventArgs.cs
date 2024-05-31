namespace EightBit
{
    public class ProfileScopeEventArgs(string scope, int cycles, int count) : EventArgs
    {
        private readonly string scope = scope;
        private readonly int cycles = cycles;
        private readonly int count = count;

        public string Scope
        {
            get
            {
                return this.scope;
            }
        }

        public int Cycles
        {
            get
            {
                return this.cycles;
            }
        }

        public int Count
        {
            get
            {
                return this.count;
            }
        }
    }
}