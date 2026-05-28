// <copyright file="ClockedChip.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

namespace EightBit
{
    public class ClockedChip : Chip
    {
        protected ClockedChip()
        {
        }

        public event EventHandler<EventArgs>? Ticked;

        public int Cycles { get; protected set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(int extra = 1)
        {
            this.Cycles += extra;
            for (var i = 0; i < extra; ++i)
            {
                this.Ticked?.Invoke(this, EventArgs.Empty);
            }
        }

        protected void ResetCycles() => this.Cycles = 0;
    }
}
