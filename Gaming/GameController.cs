namespace Gaming
{
    using SDL3;
    using System;
    using System.Diagnostics;

    public class GameController : IDisposable
    {
        private readonly EightBit.ILogger _logger;
        private readonly uint _index;
        private readonly ScopedHandle _gamepad = new(SDL.CloseGamepad);
        private bool _disposed;

        public GameController(EightBit.ILogger logger, uint index)
        {
            this._logger = logger;
            this._index = index;
            this.Open();
        }

        private void Open()
        {
            Debug.Assert(SDL.IsGamepad(this._index));
            this._gamepad.Handle = SDL.OpenGamepad(this._index);
            Wrapper.MaybeThrowException(this._gamepad, "Unable to open gamepad");
            var gamepadName = SDL.GetGamepadName(this._gamepad);
            Wrapper.MaybeThrowException(gamepadName != null, "Unable to obtain gamepad name");
            this._logger.Inform($"Game controller name: {gamepadName}");
        }

        public void StartRumble()
        {
            var success = SDL.RumbleGamepad(this._gamepad, 0xFFFF, 0x0000, 1000);
            Wrapper.MaybeThrowException(success, "Unable to start haptic rumble");
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
                    this._gamepad.Dispose();
                }
                this._disposed = true;
            }
        }
    }
}
