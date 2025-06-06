﻿// <copyright file="ClockedChip.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public class ClockedChip : Chip
    {
        protected ClockedChip()
        {
        }

        public event EventHandler<EventArgs>? Ticked;

        public int Cycles { get; protected set; }

        public void Tick(int extra)
        {
            for (var i = 0; i < extra; ++i)
            {
                this.Tick();
            }
        }

        public void Tick()
        {
            ++this.Cycles;
            Ticked?.Invoke(this, EventArgs.Empty);
        }

        protected void ResetCycles() => this.Cycles = 0;
    }
}
