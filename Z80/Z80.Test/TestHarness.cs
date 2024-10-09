// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.Test
{
    using System;
    using System.Diagnostics;

    internal class TestHarness(Configuration configuration) : IDisposable
    {
        private readonly Stopwatch timer = new();
        private readonly Board board = new(configuration);
        private long totalCycles;
        private TimeSpan totalUserProcessorTime;

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public long ElapsedMilliseconds => timer.ElapsedMilliseconds;

        public float ElapsedSeconds => ElapsedMilliseconds / 1000.0f;

        public float CyclesPerSecond => totalCycles / ElapsedSeconds;

        public void Run()
        {
            using var process = Process.GetCurrentProcess();
            {
                timer.Start();
                var startingUsage = process.UserProcessorTime;

                board.Initialize();
                board.RaisePOWER();

                var cpu = board.CPU;
                while (cpu.Powered)
                {
                    totalCycles += cpu.Step();
                }

                var finishingUsage = process.UserProcessorTime;
                timer.Stop();
                totalUserProcessorTime = finishingUsage - startingUsage;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    System.Console.Out.WriteLine();

                    System.Console.Out.WriteLine($"Guest cycles = {totalCycles:N0}");
                    System.Console.Out.WriteLine($"Seconds = {ElapsedSeconds}");

                    System.Console.Out.WriteLine($"{CyclesPerSecond / 1000000} MHz");
                    System.Console.Out.WriteLine($"Processor time = {totalUserProcessorTime.TotalSeconds:g}");
                }

                disposed = true;
            }
        }
    }
}
