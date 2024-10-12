// <copyright file="Device.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class Device
    {
        private PinLevel _powerLine;

        public event EventHandler<EventArgs>? RaisingPOWER;

        public event EventHandler<EventArgs>? RaisedPOWER;

        public event EventHandler<EventArgs>? LoweringPOWER;

        public event EventHandler<EventArgs>? LoweredPOWER;

        public bool Powered => this.POWER.Raised();

        public ref PinLevel POWER => ref this._powerLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaisePOWER()
        {
            if (this.POWER.Lowered())
            {
                this.OnRaisingPOWER();
                this.POWER.Raise();
                this.OnRaisedPOWER();
            }
        }

        public virtual void LowerPOWER()
        {
            if (this.POWER.Raised())
            {
                this.OnLoweringPOWER();
                this.POWER.Lower();
                this.OnLoweredPOWER();
            }
        }

        protected virtual void OnRaisingPOWER() => RaisingPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedPOWER() => RaisedPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringPOWER() => LoweringPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredPOWER() => LoweredPOWER?.Invoke(this, EventArgs.Empty);
    }
}
