namespace EightBit
{
    public sealed class ConsoleLogger(string context = "") : AbstractLogger(context), ILogger
    {
        public override void Log(string context, string message, ILogger.LogLevel level)
        {
            Console.WriteLine($"{context}: {level}: {message}");
        }
    }
}
