namespace EightBit
{
    using System;

    public class Device
    {
        private PinLevel powerLine;

        protected Device() { }

        public event EventHandler<EventArgs> RaisingPOWER;
        public event EventHandler<EventArgs> RaisedPOWER;
        public event EventHandler<EventArgs> LoweringPOWER;
        public event EventHandler<EventArgs> LoweredPOWER;

        public ref PinLevel POWER() => ref powerLine;

        public bool Powered => POWER().Raised();

        public virtual void RaisePOWER()
        {
            OnRaisingPOWER();
            POWER().Raise();
            OnRaisedPOWER();
        }

        public virtual void LowerPOWER()
        {
            OnLoweringPOWER();
            POWER().Lower();
            OnLoweredPOWER();
        }

        protected virtual void OnRaisingPOWER() => RaisingPOWER?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRaisedPOWER() => RaisedPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringPOWER() => LoweringPOWER?.Invoke(this, EventArgs.Empty);
        protected virtual void OnLoweredPOWER() => LoweredPOWER?.Invoke(this, EventArgs.Empty);
    }
}
