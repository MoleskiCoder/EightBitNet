namespace Gaming
{
    using EightBit;
    using SDL3;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;

    public abstract class Game(ILogger.LogLevel logging) : Device
    {
        private readonly Wrapper _wrapper = new(logging);
        private bool _vsync;

        private readonly SortedDictionary<uint, GameController> _gameControllers = [];

        public ILogger Logger => this._wrapper.Logger;

        protected ScopedHandle Window { get; } = new(SDL.DestroyWindow);

        protected ScopedHandle Renderer { get; } = new(SDL.DestroyRenderer);

        protected ScopedHandle BitmapTexture { get; } = new(SDL.DestroyTexture);

        protected abstract SDL.PixelFormat PixelFormat { get; }

        public abstract float FramesPerSecond { get; }

        public abstract bool UseVSYNC { get; }

        protected virtual int WindowWidth => this.DisplayWidth * this.DisplayScale;

        protected virtual int WindowHeight => this.DisplayHeight * this.DisplayScale;

        public virtual int DisplayWidth => this.RasterWidth;

        public virtual int DisplayHeight => this.RasterHeight;

        public abstract int DisplayScale { get; }

        public abstract int RasterWidth { get; }

        public abstract int RasterHeight { get; }

        public abstract string Title { get; }

        protected abstract uint[] Pixels();

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
            this.Logger.Context = this.Title;
            this.Logger.Verbosity = this._wrapper.Verbosity;

            this._wrapper.RaisePOWER();

            this.Window.Handle = SDL.CreateWindow(this.Title, this.WindowWidth, this.WindowHeight, 0L);
            Wrapper.MaybeThrowException(this.Window, "Unable to create window");
            var displayId = SDL.GetDisplayForWindow(this.Window);
            Wrapper.MaybeThrowException(displayId != 0, "Unable to obtain display ID for window");
            var currentDisplayMode = SDL.GetCurrentDisplayMode(displayId);
            Wrapper.MaybeThrowException(currentDisplayMode.HasValue, "Unable to obtain window mode");
            this.Renderer.Handle = SDL.CreateRenderer(this.Window, null);
            Wrapper.MaybeThrowException(this.Renderer, "Unable to create renderer: ");

            this._vsync = this.UseVSYNC;
            if (this._vsync)
            {
                var framesPerSecond = this.FramesPerSecond;
                var refreshRate = currentDisplayMode?.RefreshRate;
                Debug.Assert(refreshRate.HasValue, "refresh rate is unavailable");
                this._vsync = Math.Abs(framesPerSecond - refreshRate.Value) < 0.001;
                if (this._vsync)
                {
                    this.Logger.Inform("Attempting to configure renderer VSYNC");
                    this._vsync = SDL.SetRenderVSync(this.Renderer, 1);
                    if (!this._vsync)
                        this.Logger.Warn($"Unable to set render VSYNC ({SDL.GetError()})");
                }
                else
                {
                    this.Logger.Warn($"Display refresh rate is incompatible with required rate ({this.FramesPerSecond})");
                }
            }

            if (!this._vsync)
            {
                this.Logger.Inform("Setting callback rate hint");
                var success = SDL.SetHint("SDL_MAIN_CALLBACK_RATE", this.FramesPerSecond.ToString(CultureInfo.InvariantCulture));
                Wrapper.MaybeThrowException(success, "Unable to set event loop callback rate hint");
            }

            this.ConfigureBackground();
            this.CreateBitmapTexture();
        }

        public virtual void Terminate()
        {
            this.BitmapTexture.Dispose();
            this.Renderer.Dispose();
            this.Window.Dispose();
            this._wrapper.LowerPOWER();
        }

        private void ConfigureBackground()
        {
            var success = SDL.SetRenderDrawColor(this.Renderer, 0, 0, 0, byte.MaxValue);
            Wrapper.MaybeThrowException(success, "Unable to set render draw colour");
        }

        private void CreateBitmapTexture()
        {
            BitmapTexture.Handle = SDL.CreateTexture(this.Renderer, this.PixelFormat, SDL.TextureAccess.Streaming, this.RasterWidth, this.RasterHeight);
            Wrapper.MaybeThrowException(BitmapTexture, "Unable to create bitmap texture");
        }

        public virtual SDL.AppResult RunFrame()
        {
            this.Update();
            this.Draw();
            return SDL.AppResult.Continue;
        }

        protected virtual void Update()
        {
            this.RunVerticalBlank();
            this.RunRasterLines();
        }

        protected virtual void RunRasterLines()
        {
        }

        protected virtual void RunVerticalBlank()
        {
        }

        public virtual SDL.AppResult HandleEvent(SDL.Event e)
        {
            switch ((SDL.EventType)e.Type)
            {
                case SDL.EventType.Quit:
                    return SDL.AppResult.Success;
                case SDL.EventType.KeyDown:
                    _ = this.HandleKeyDown(e.Key.Key);
                    break;
                case SDL.EventType.KeyUp:
                    _ = this.HandleKeyUp(e.Key.Key);
                    break;
                case SDL.EventType.JoystickButtonDown:
                    _ = this.HandleJoyButtonDown(e.JButton);
                    break;
                case SDL.EventType.JoystickButtonUp:
                    _ = this.HandleJoyButtonUp(e.JButton);
                    break;
                case SDL.EventType.GamepadButtonDown:
                    _ = this.HandleGamepadButtonDown(e.GButton);
                    break;
                case SDL.EventType.GamepadButtonUp:
                    _ = this.HandleGamepadButtonUp(e.GButton);
                    break;
                case SDL.EventType.GamepadAdded:
                    this.AddGampad(e);
                    break;
                case SDL.EventType.GamepadRemoved:
                    this.RemoveGamepad(e);
                    break;
            }
            return SDL.AppResult.Continue;
        }

        protected virtual void Draw()
        {
            this.UpdateTexture();
            this.RenderTexture();
            this.DisplayTexture();
        }

        protected virtual void RemoveGamepad(SDL.Event e)
        {
            var which = e.JDevice.Which;
            var found = this._gameControllers.TryGetValue(which, out var gameController);
            Debug.Assert(found);
            Debug.Assert(gameController != null, "controller is not null");
            _ = this._gameControllers.Remove(which);
            this.Logger.Inform($"Joystick device {which} removed ({this._gameControllers.Count} controllers available)");
        }

        protected virtual void AddGampad(SDL.Event e)
        {
            var which = e.JDevice.Which;
            var found = this._gameControllers.ContainsKey(which);
            Debug.Assert(!found);
            GameController gameController = new(this.Logger, which);
            this._gameControllers[which] = gameController;
            this.Logger.Inform($"Joystick device {which} address ({this._gameControllers.Count} controllers available)");
        }

        public GameController Gamepad(uint which)
        {
            var success = this._gameControllers.TryGetValue(which, out var gameController);
            Wrapper.MaybeThrowException(success, "Unknown controller");
            Debug.Assert(gameController != null, "controller is not null");
            return gameController;
        }

        public uint ChooseControllerIndex(int who)
        {
            int count = this._gameControllers.Count;
            int num;
            switch (count)
            {
                case 0:
                    return uint.MaxValue;
                case 1:
                    num = 1;
                    break;
                default:
                    num = who == 1 ? 1 : 0;
                    break;
            }
            return num != 0 ? this._gameControllers.First().Key : count > 1 ? this._gameControllers.Skip(1).First().Key : uint.MaxValue;
        }

        public GameController? ChooseController(int who)
        {
            var key = this.ChooseControllerIndex(who);
            if (key == uint.MaxValue)
                return  null;
            var found = this._gameControllers.TryGetValue(key, out var gameController);
            Debug.Assert(found);
            return gameController;
        }

        protected void UpdateTexture()
        {
            var span = MemoryMarshal.Cast<uint, byte>(this.Pixels().AsSpan<uint>());
            var success = SDL.UpdateTexture(this.BitmapTexture, IntPtr.Zero, span, this.DisplayWidth * sizeof(uint));
            Wrapper.MaybeThrowException(success, "Unable to update texture");
        }

        protected void RenderTexture()
        {
            var success = SDL.RenderTexture(this.Renderer, this.BitmapTexture, IntPtr.Zero, IntPtr.Zero);
            Wrapper.MaybeThrowException(success, "Unable to render texture");
        }

        protected void DisplayTexture()
        {
            var success = SDL.RenderPresent(this.Renderer);
            Wrapper.MaybeThrowException(success, "Unable to present render to screen");
        }

        protected void ToggleFullscreen()
        {
            var fullscreen = (SDL.GetWindowFlags(this.Window) & SDL.WindowFlags.Fullscreen) > 0L;
            var success = SDL.SetWindowFullscreen(this.Window, !fullscreen);
            Wrapper.MaybeThrowException(success, "Failed to toggle window full screen setting");
            Wrapper.MaybeThrowException(success ? SDL.ShowCursor() : SDL.HideCursor(), "Failed to toggle cursor show/hide");
        }

        protected virtual bool HandleKeyDown(SDL.Keycode key) => key == SDL.Keycode.F12;

        protected virtual bool HandleKeyUp(SDL.Keycode key)
        {
            if (key != SDL.Keycode.F12)
                return false;
            this.ToggleFullscreen();
            return true;
        }

        protected virtual bool HandleJoyButtonDown(SDL.JoyButtonEvent e) => false;

        protected virtual bool HandleJoyButtonUp(SDL.JoyButtonEvent e) => false;

        protected virtual bool HandleGamepadButtonDown(SDL.GamepadButtonEvent e) => false;

        protected virtual bool HandleGamepadButtonUp(SDL.GamepadButtonEvent e) => false;
    }
}
