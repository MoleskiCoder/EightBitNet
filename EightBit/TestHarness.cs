// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Diagnostics;

    public class TestHarness<TBus, TProcessor>(TBus board, TProcessor cpu) where TBus : Bus where TProcessor : Processor
    {
        private readonly Stopwatch _timer = new();
        private readonly TBus _board = board;
        private readonly TProcessor _cpu = cpu;
        private long _totalCycles;
        private TimeSpan _totalUserProcessorTime;

        public long ElapsedMilliseconds => this._timer.ElapsedMilliseconds;

        public float ElapsedSeconds => this.ElapsedMilliseconds / 1000.0f;

        public float CyclesPerSecond => this._totalCycles / this.ElapsedSeconds;

        public void Run()
        {
            using var process = Process.GetCurrentProcess();
            {
                this._timer.Start();
                var startingUsage = process.UserProcessorTime;

                this._board.Initialize();
                this._board.RaisePOWER();

                while (this._cpu.Powered)
                {
                    this._totalCycles += this._cpu.Step();
                }

                var finishingUsage = process.UserProcessorTime;
                this._timer.Stop();
                this._totalUserProcessorTime = finishingUsage - startingUsage;
            }

            Console.Out.WriteLine();

            Console.Out.WriteLine($"Guest cycles = {this._totalCycles:N0}");
            Console.Out.WriteLine($"Seconds = {this.ElapsedSeconds}");

            Console.Out.WriteLine($"{this.CyclesPerSecond / 1000000} MHz");
            Console.Out.WriteLine($"Processor time = {this._totalUserProcessorTime.TotalSeconds:g}");
        }
    }
}
