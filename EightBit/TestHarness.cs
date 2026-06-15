// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;

    public class TestHarness<TBus, TProcessor>(TBus board, TProcessor cpu, int targetClocksPerSecond = 2_000_000) where TBus : Bus where TProcessor : Processor
    {
        private readonly Stopwatch _timer = new();
        private readonly TBus _board = board;
        private readonly TProcessor _cpu = cpu;
        private readonly int _targetClocksPerSecond = targetClocksPerSecond;
        private long _totalCycles;

        public long ElapsedMilliseconds => this._timer.ElapsedMilliseconds;

        public TimeSpan ActualElapsed => TimeSpan.FromMilliseconds(this.ElapsedMilliseconds);

        public double CyclesPerSecond => this._totalCycles / this.ActualElapsed.TotalSeconds;

        public double ActualClockSpeed => this.CyclesPerSecond / 1_000_000;

        public int TargetClocksPerSecond => this._targetClocksPerSecond;

        public double TargetClockSpeed => this.TargetClocksPerSecond / 1_000_000;

        public long TotalCycles => this._totalCycles;

        public TimeSpan TargetElapsed => TimeSpan.FromSeconds(this.TotalCycles / (double)this.TargetClocksPerSecond);

        public void Run()
        {
            this._board.Initialize();
            this._board.RaisePOWER();

            this._timer.Start();
            while (this._cpu.Powered)
            {
                this._totalCycles += this._cpu.Step();
            }
            this._timer.Stop();

            Console.Out.WriteLine();

            Console.Out.WriteLine($"Guest cycles = {this._totalCycles:N0}");
            Console.Out.WriteLine($"Elapsed time (at {this.ActualClockSpeed:g3}MHz actual) = {this.ActualElapsed}");
            Console.Out.WriteLine($"Elapsed time (at {this.TargetClockSpeed:g3}MHz target) = {this.TargetElapsed}");
        }
    }
}
