namespace Gaming
{
    using SDL3;
    using System;
    using System.Diagnostics;

    public class GameController : IDisposable
    {
        private readonly uint _index;
        private readonly ScopedHandle _gamepad = new(SDL.CloseGamepad);
        private readonly ScopedHandle _haptic = new(SDL.CloseHaptic);
        private bool _hapticRumbleSupported;
        private bool _disposed;

        public GameController(uint index)
        {
            this._index = index;
            this.Open();
        }

        private void Open()
        {
            var joysticks = SDL.GetJoysticks(out var count);
            Wrapper.MaybeThrowException(joysticks != null, "Unable to obtain joystick information");
            Debug.Assert(count > 0, "count > 0");
            Debug.Assert(this._index < count, "this._index < count");
            if (SDL.IsGamepad(this._index))
            {
                this._gamepad.Handle = SDL.OpenGamepad(this._index);
                Wrapper.MaybeThrowException(this._gamepad, "Unable to open gamepad");
                this.OpenHaptic();
                var gamepadName = SDL.GetGamepadName(this._gamepad);
                Wrapper.MaybeThrowException(gamepadName != null, "Unable to obtain gamepad name");
                SDL.LogInfo(SDL.LogCategory.Input, "Game controller name: " + gamepadName);
            }
            else
            {
                SDL.LogWarn(SDL.LogCategory.Input, "Joystick is not a game controller");
            }
        }

        private void Close()
        {
            this._gamepad.Dispose();
            this.CloseHaptic();
        }

        private void OpenHaptic()
        {
            this._haptic.Handle = SDL.OpenHaptic((int)this._index); // XXXX Possible SDL3-cs int cast bug
            Wrapper.MaybeThrowException(this._haptic, "Unable to open haptic gamepad");
            Wrapper.MaybeThrowException(SDL.InitHapticRumble(this._haptic), "Unable to initialise haptic gamepad");
            this._hapticRumbleSupported = SDL.HapticRumbleSupported(this._haptic);
        }

        private void CloseHaptic()
        {
            this._haptic.Dispose();
            this._hapticRumbleSupported = false;
        }

        public void StartRumble()
        {
            if (this._hapticRumbleSupported && !SDL.PlayHapticRumble(this._haptic, 1f, 1000U))
                SDL.LogWarn(SDL.LogCategory.Input, "Unable to start haptic rumble: " + SDL.GetError());
        }

        public void StopRumble()
        {
            if (this._hapticRumbleSupported && !SDL.StopHapticRumble(this._haptic))
                SDL.LogWarn(SDL.LogCategory.Input, "Unable to stop haptic rumble: " + SDL.GetError());
        }

        public static uint BuildJoystickId(IntPtr gamepad)
        {
            var gamepadJoystick = SDL.GetGamepadJoystick(gamepad);
            Wrapper.MaybeThrowException(gamepadJoystick, "Unable to obtain joystick from gamepad");
            return SDL.GetJoystickID(gamepadJoystick);
        }

        public uint GetJoystickId() => GameController.BuildJoystickId((IntPtr) this._gamepad);

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
                    this.Close();
                }
                this._disposed = true;
            }
        }
    }
}
