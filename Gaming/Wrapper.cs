namespace Gaming
{
    using SDL3;
    using EightBit;
    using System;

    public class Wrapper(ILogger.LogLevel logging = ILogger.LogLevel.Warning) : Device
    {
        private readonly ILogger _logger = new SdlLogger("Unnamed Game");

        private ILogger.LogLevel _verbosity = logging;

        public ILogger Logger => this._logger;

        public ILogger.LogLevel Verbosity => this._verbosity;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.Initialise();
        }

        public override void LowerPOWER()
        {
            this.Terminate();
            base.LowerPOWER();
        }

        public virtual void Initialise()
        {
            var success = SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Gamepad | SDL.InitFlags.Haptic);
            Wrapper.MaybeThrowException(success, "Unable to initialise SDL library");
            SDL.SetLogPriorities(SdlLogger.Translate(this.Verbosity));
            this.Logger.Inform("SDL library initialised");
        }

        public virtual void Terminate()
        {
            this.Logger.Inform("SDL library terminating");
            SDL.Quit();
        }

        public static void ThrowException(string failure)
        {
            throw new InvalidOperationException($"SDL: {failure}: {SDL.GetError()}");
        }

        public static void MaybeThrowException(bool success, string failure)
        {
            if (!success)
                Wrapper.ThrowException(failure);
        }

        public static void MaybeThrowException(IntPtr handle, string failure)
        {
            MaybeThrowException(handle != IntPtr.Zero, failure);
        }
    }
}
