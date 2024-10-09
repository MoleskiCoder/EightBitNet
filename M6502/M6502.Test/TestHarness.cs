// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    using System;
    using System.Diagnostics;

    internal sealed class TestHarness(Configuration configuration)
    {
        private readonly Stopwatch timer = new();
        private readonly Board board = new(configuration);
        private long totalCycles;
        private TimeSpan totalUserProcessorTime;

        public long ElapsedMilliseconds => this.timer.ElapsedMilliseconds;

        public float ElapsedSeconds => this.ElapsedMilliseconds / 1000.0f;

        public float CyclesPerSecond => this.totalCycles / this.ElapsedSeconds;

        public void Run()
        {
            using var process = Process.GetCurrentProcess();
            {
                this.timer.Start();
                var startingUsage = process.UserProcessorTime;

                this.board.Initialize();
                this.board.RaisePOWER();

                var cpu = this.board.CPU;
                while (cpu.Powered)
                {
                    this.totalCycles += cpu.Step();
                }

                var finishingUsage = process.UserProcessorTime;
                this.timer.Stop();
                this.totalUserProcessorTime = finishingUsage - startingUsage;
            }

            System.Console.Out.WriteLine();

            System.Console.Out.WriteLine($"Guest cycles = {this.totalCycles:N0}");
            System.Console.Out.WriteLine($"Seconds = {this.ElapsedSeconds}");

            System.Console.Out.WriteLine($"{this.CyclesPerSecond / 1000000} MHz");
            System.Console.Out.WriteLine($"Processor time = {this.totalUserProcessorTime.TotalSeconds:g}");
        }
    }
}
