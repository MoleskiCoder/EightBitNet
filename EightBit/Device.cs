// <copyright file="Device.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public class Device
    {
        private PinLevel _powerLine;

        public event EventHandler<EventArgs>? RaisingPOWER;

        public event EventHandler<EventArgs>? RaisedPOWER;

        public event EventHandler<EventArgs>? LoweringPOWER;

        public event EventHandler<EventArgs>? LoweredPOWER;

        public bool Powered => POWER.Raised();

        public ref PinLevel POWER => ref _powerLine;

        public virtual void RaisePOWER()
        {
            if (POWER.Lowered())
            {
                OnRaisingPOWER();
                POWER.Raise();
                OnRaisedPOWER();
            }
        }

        public virtual void LowerPOWER()
        {
            if (POWER.Raised())
            {
                OnLoweringPOWER();
                POWER.Lower();
                OnLoweredPOWER();
            }
        }

        protected virtual void OnRaisingPOWER() => RaisingPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedPOWER() => RaisedPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringPOWER() => LoweringPOWER?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredPOWER() => LoweredPOWER?.Invoke(this, EventArgs.Empty);
    }
}
