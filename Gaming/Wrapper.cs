namespace Gaming
{
    using SDL3;
    using EightBit;
    using System;

    public class Wrapper(SDL.LogPriority logging = SDL.LogPriority.Warn) : Device
    {
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
            SDL.SetLogPriorities(logging);
            SDL.LogInfo(SDL.LogCategory.Application, "SDL library initialised");
        }

        public virtual void Terminate()
        {
            SDL.LogInfo(SDL.LogCategory.Application, "SDL library terminating");
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
