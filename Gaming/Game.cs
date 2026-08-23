namespace Gaming
{
    using EightBit;
    using SDL3;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public abstract class Game(bool verbose = false) : Device, IDisposable
    {
        private readonly Wrapper _wrapper = new(verbose);
        private readonly ScopedHandle _window = new(IntPtr.Zero, h => SDL.DestroyWindow(h));
        private readonly ScopedHandle _renderer = new(IntPtr.Zero, h => SDL.DestroyRenderer(h));
        private readonly ScopedHandle _bitmapTexture = new(IntPtr.Zero, h => SDL.DestroyTexture(h));
        private IntPtr _pixelFormat = IntPtr.Zero;
        private readonly SDL.PixelFormat _pixelType = SDL.PixelFormat.ARGB8888;
        private bool _vsync;
        private ulong _performanceFrequency;
        private double _targetFrameTime;
        private ulong _frameStartTime;
        private ulong _frameEndTime;
        private readonly SortedDictionary<uint, GameController> _gameControllers = [];
        private readonly Dictionary<uint, uint> _mappedControllers = [];
        private bool _disposed;

        protected ScopedHandle Window => this._window;

        protected ScopedHandle Renderer => this._renderer;

        protected ScopedHandle BitmapTexture => this._bitmapTexture;

        protected IntPtr PixelFormat => this._pixelFormat;

        protected abstract uint[] Pixels { get; }

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

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            this._window.Handle = SDL.CreateWindow(this.Title, this.WindowWidth, this.WindowHeight, 0L);
            Wrapper.MaybeThrowException(this._window, "Unable to create window");
            SDL.DisplayMode? currentDisplayMode = SDL.GetCurrentDisplayMode(SDL.GetDisplayForWindow(this._window));
            Wrapper.MaybeThrowException(currentDisplayMode.HasValue, "Unable to obtain window mode");
            this._renderer.Handle = SDL.CreateRenderer(this._window, null);
            Wrapper.MaybeThrowException(this._renderer, "Unable to create renderer: ");

            this._vsync = this.UseVSYNC;
            if (this._vsync)
            {
                float framesPerSecond = this.FramesPerSecond;
                float? refreshRate = currentDisplayMode?.RefreshRate;
                Debug.Assert(refreshRate.HasValue, "refresh rate is unavailable");
                this._vsync = Math.Abs(framesPerSecond - refreshRate.Value) < 0.001;
                if (this._vsync)
                {
                    SDL.LogInfo(SDL.LogCategory.Render, "Attempting to configure renderer VSYNC");
                    this._vsync = SDL.SetRenderVSync(this._renderer, 1);
                    if (!this._vsync)
                        SDL.LogWarn(SDL.LogCategory.Render, $"Unable to set render VSYNC ({SDL.GetError()})");
                }
                else
                {
                    SDL.LogWarn(SDL.LogCategory.Render, $"Display refresh rate is incompatible with required rate (this.FramesPerSecond)");
                }
            }

            this._pixelFormat = SDL.GetPixelFormatDetails(this._pixelType);
            Wrapper.MaybeThrowException(this._pixelFormat, "Unable to obtain pixel format details");
            this.ConfigureBackground();
            this.CreateBitmapTexture();
            this._performanceFrequency = SDL.GetPerformanceFrequency();
            this._targetFrameTime = 1.0 / (double) this.FramesPerSecond;
        }

        private void ConfigureBackground()
        {
            var success = SDL.SetRenderDrawColor(this._renderer, 0, 0, 0, byte.MaxValue);
            Wrapper.MaybeThrowException(success, "Unable to set render draw colour");
        }

        private void CreateBitmapTexture()
        {
            this._bitmapTexture.Handle = SDL.CreateTexture(this._renderer, this._pixelType, SDL.TextureAccess.Streaming, this.RasterWidth, this.RasterHeight);
            Wrapper.MaybeThrowException(this._bitmapTexture, "Unable to create bitmap texture");
        }

        public virtual void RunLoop()
        {
            while (this.Powered)
            {
                this.Update();
                this.Draw();
                _ = this.MaybeSynchronise();
            }
        }

        protected virtual void Update()
        {
            this._frameStartTime = SDL.GetPerformanceCounter();
            this.HandleEvents();
            this.RunVerticalBlank();
            this.RunRasterLines();
        }

        protected virtual void RunRasterLines()
        {
        }

        protected virtual void RunVerticalBlank()
        {
        }

        protected virtual void HandleEvents()
        {
            while (SDL.PollEvent(out var e))
            {
                switch ((SDL.EventType)e.Type)
                {
                    case SDL.EventType.Quit:
                        this.LowerPOWER();
                        break;
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
                    case SDL.EventType.JoystickAdded:
                        this.AddJoystick(e);
                        break;
                    case SDL.EventType.JoystickRemoved:
                        this.RemoveJoystick(e);
                        break;
                }
            }
        }

        protected virtual void Draw()
        {
            this.UpdateTexture();
            this.RenderTexture();
            this.DisplayTexture();
        }

        protected bool MaybeSynchronise()
        {
            bool synchronise = !this._vsync;
            if (synchronise)
                this.Synchronise();
            return synchronise;
        }

        protected void Synchronise()
        {
            this._frameEndTime = SDL.GetPerformanceCounter();
            double frameTimeTicks = this._frameEndTime - this._frameStartTime;
            SDL.LogDebug(SDL.LogCategory.Render, $"Frame time (ticks): {frameTimeTicks}");

            var frameTime = frameTimeTicks / this._performanceFrequency;
            SDL.LogDebug(SDL.LogCategory.Render, $"Frame time (seconds): {frameTime}");

            var gap = this._targetFrameTime - frameTime;
            SDL.LogDebug(SDL.LogCategory.Render, $"Timing gap (seconds): {gap}");

            if (gap > 0.0)
            {
                var delay = (uint)(gap * 1000.0);
                SDL.LogDebug(SDL.LogCategory.Render, $"Delay (ticks): {delay}");
                SDL.Delay(delay);
            }
    
            if (gap < 0.0)
                SDL.LogWarn(SDL.LogCategory.Render, "Running slowly");
        }

        protected virtual void RemoveJoystick(SDL.Event e)
        {
            var which = e.JDevice.Which;
            var found = this._gameControllers.TryGetValue(which, out var gameController);
            Debug.Assert(found);
            Debug.Assert(gameController != null, "controller is not null");
            _ = this._mappedControllers.Remove(gameController.GetJoystickId());
            _ = this._gameControllers.Remove(which);
            SDL.LogInfo(SDL.LogCategory.Input, $"Joystick device {which} removed ({this._gameControllers.Count} controllers available)");
        }

        protected virtual void AddJoystick(SDL.Event e)
        {
            var which = e.JDevice.Which;
            var found = this._gameControllers.ContainsKey(which);
            Debug.Assert(!found);
            GameController gameController = new(which);
            uint joystickId = gameController.GetJoystickId();
            this._gameControllers[which] = gameController;
            this._mappedControllers[joystickId] = which;
            SDL.LogInfo(SDL.LogCategory.Input, $"Joystick device {which} address ({this._gameControllers.Count} controllers available)");
        }

        public GameController Gamepad(uint which)
        {
            var success = this._gameControllers.TryGetValue(which, out var gameController);
            Wrapper.MaybeThrowException(success, "Unknown controller");
            Debug.Assert(gameController != null, "controller is not null");
            return gameController;
        }

        public uint MappedController(uint which)
        {
            var success = this._mappedControllers.TryGetValue(which, out var id);
            Wrapper.MaybeThrowException(success, "Unknown joystick");
            return id;
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
            var span = MemoryMarshal.Cast<uint, byte>(this.Pixels.AsSpan<uint>());
            var success = SDL.UpdateTexture(this._bitmapTexture, IntPtr.Zero, span, this.DisplayWidth * sizeof(uint));
            Wrapper.MaybeThrowException(success, "Unable to update texture");
        }

        protected void RenderTexture()
        {
            var success = SDL.RenderTexture(this._renderer, this._bitmapTexture, IntPtr.Zero, IntPtr.Zero);
            Wrapper.MaybeThrowException(success, "Unable to render texture");
        }

        protected void DisplayTexture()
        {
            var success = SDL.RenderPresent(this._renderer);
            Wrapper.MaybeThrowException(success, "Unable to present render to screen");
        }

        protected void ToggleFullscreen()
        {
            var fullscren = (SDL.GetWindowFlags(this._window) & SDL.WindowFlags.Fullscreen) > 0L;
            var success = SDL.SetWindowFullscreen(this._window, !fullscren);
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
                    this._bitmapTexture.Dispose();
                    this._renderer.Dispose();
                    this._window.Dispose();
                    this._wrapper.Dispose();
                }
            }
            this._disposed = true;
        }
    }
}
