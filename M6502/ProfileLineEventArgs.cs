namespace EightBit
{
    public class ProfileLineEventArgs(string source, int cycles) : EventArgs
    {
        private readonly string source = source;
        private readonly int cycles = cycles;

        public string Source
        {
            get
            {
                return this.source;
            }
        }

        public int Cycles
        {
            get
            {
                return this.cycles;
            }
        }
    }
}