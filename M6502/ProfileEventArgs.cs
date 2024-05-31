namespace EightBit
{
    public class ProfileEventArgs(string output) : EventArgs
    {
        private readonly string output = output;

        public string Output
        {
            get
            {
                return this.output;
            }
        }
    }
}