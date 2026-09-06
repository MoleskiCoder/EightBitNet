namespace Gaming
{
    using SDL3;
    using System;
    using EightBit;

    internal sealed class SdlLogger(string context) : AbstractLogger(context), ILogger
    {
        public static ILogger.LogLevel Translate(SDL.LogPriority priority)
        {
            switch (priority)
            {
                case SDL.LogPriority.Debug:
                    return ILogger.LogLevel.Debugging;
                case SDL.LogPriority.Info:
                    return ILogger.LogLevel.Information;
                case SDL.LogPriority.Warn:
                    return ILogger.LogLevel.Warning;
                case SDL.LogPriority.Critical:
                    return ILogger.LogLevel.Critical;
                default:
                    throw new InvalidOperationException($"Unhandled logging priority ({priority})");
            }
        }

        public static SDL.LogPriority Translate(ILogger.LogLevel priority)
        {
            switch (priority)
            {
                case ILogger.LogLevel.Debugging:
                    return SDL.LogPriority.Debug;
                case ILogger.LogLevel.Information:
                    return SDL.LogPriority.Info;
                case ILogger.LogLevel.Warning:
                    return SDL.LogPriority.Warn;
                case ILogger.LogLevel.Critical:
                    return SDL.LogPriority.Critical;
                default:
                    throw new InvalidOperationException($"Unhandled logging priority ({priority})");
            }
        }

        public override void Log(string context, string message, ILogger.LogLevel level)
        {
            SDL.LogMessage(SDL.LogCategory.Application, Translate(level), $"{context}: {message}");
        }
    }
}
