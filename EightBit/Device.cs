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
                RaisingPOWER?.Invoke(this, EventArgs.Empty);
                this.POWER.Raise();
                RaisedPOWER?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void LowerPOWER()
        {
            if (this.POWER.Raised())
            {
                LoweringPOWER?.Invoke(this, EventArgs.Empty);
                this.POWER.Lower();
                LoweredPOWER?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
