namespace EightBit
{
    public abstract class AbstractLogger(string context)
    {
        private string _context = context;
        private ILogger.LogLevel _verbosity = ILogger.LogLevel.Critical;

        public ILogger.LogLevel Verbosity
        {
            get => this._verbosity;
            set => this._verbosity = value;
        }

        public string Context
        {
            get => this._context;
            set => this._context = value;
        }

        public abstract void Log(string context, string message, ILogger.LogLevel level);
    }
}
