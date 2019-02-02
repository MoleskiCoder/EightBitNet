namespace EightBit
{
    using System;

    public class ClockedChip : Chip
    {
        private int cycles;

        protected ClockedChip() { }

        public int Cycles { get => cycles; protected set => cycles = value; }

        public event EventHandler<EventArgs> Ticked;
  
        public void Tick(int extra)
        {
            for (int i = 0; i < extra; ++i)
                Tick();
        }

        public void Tick()
        {
            ++Cycles;
            OnTicked();
        }

        protected virtual void OnTicked() => Ticked?.Invoke(this, EventArgs.Empty);

        protected void ResetCycles() => Cycles = 0;
    }
}
