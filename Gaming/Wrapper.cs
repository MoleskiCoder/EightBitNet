namespace Gaming
{
    using SDL3;
    using System;

    public class Wrapper : IDisposable
    {
        private bool _disposed;

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

        public Wrapper(bool verbose)
        {
            var success = SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Gamepad | SDL.InitFlags.Haptic);
            Wrapper.MaybeThrowException(success, "Unable to initialise SDL library");
            SDL.SetLogPriorities(verbose ? SDL.LogPriority.Trace : SDL.LogPriority.Warn);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    SDL.Quit();
                }
                this._disposed = true;
            }
        }
    }
}
