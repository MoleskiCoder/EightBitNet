namespace EightBit
{
    public interface ILogger
    {
        enum LogLevel { Debugging, Information, Warning, Critical, }

        abstract string Context { get; set; }

        abstract LogLevel Verbosity { get; set; }

        abstract bool Strict { get; set; }

        abstract void Log(string context, string message, LogLevel level);

        void Log(string message, LogLevel level) => this.Log(this.Context, message, level);

        void MaybeLog(string message, LogLevel level)
        {
            if (level >= this.Verbosity)
                this.Log(message, level);
        }

        void Debug(string message) => this.MaybeLog(message, level: LogLevel.Debugging);

        void Inform(string message) => this.MaybeLog(message, level: LogLevel.Information);

        void Warn(string message) => this.MaybeLog(message, level: LogLevel.Warning);

        void Scream(string message) => this.MaybeLog(message, level: LogLevel.Critical);

        void MaybeThrow(string message)
        {
            if (this.Strict)
                throw new InvalidDataException(message);
            Warn(message);
        }

        bool Check(bool failure, string message)
        {
            if (failure)
                this.MaybeThrow(message);
            return failure;
        }
    }
}
