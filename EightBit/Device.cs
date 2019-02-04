// <copyright file="Device.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Device
    {
        private PinLevel powerLine;

        protected Device()
        {
        }

        public event EventHandler<EventArgs> RaisingPOWER;

        public event EventHandler<EventArgs> RaisedPOWER;

        public event EventHandler<EventArgs> LoweringPOWER;

        public event EventHandler<EventArgs> LoweredPOWER;

        public bool Powered => this.POWER().Raised();

        public ref PinLevel POWER() => ref this.powerLine;

        public virtual void RaisePOWER()
        {
            this.OnRaisingPOWER();
            this.POWER().Raise();
            this.OnRaisedPOWER();
        }

        public virtual void LowerPOWER()
        {
            this.OnLoweringPOWER();
            this.POWER().Lower();
            this.OnLoweredPOWER();
        }

        protected virtual void OnRaisingPOWER() => this.RaisingPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedPOWER() => this.RaisedPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringPOWER() => this.LoweringPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredPOWER() => this.LoweredPOWER?.Invoke(this, EventArgs.Empty);
    }
}
