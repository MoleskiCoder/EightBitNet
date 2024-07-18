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
        private long totalCycles = 0;

        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Run()
        {
            this.board.Initialize();
            this.board.RaisePOWER();

            var cpu = this.board.CPU;

            this.timer.Start();
            while (cpu.Powered)
            {
                this.totalCycles += cpu.Step();
            }

            this.timer.Stop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    System.Console.Out.WriteLine($"\n\nGuest cycles = {this.totalCycles}");
                    System.Console.Out.WriteLine($"Seconds = {this.timer.ElapsedMilliseconds / 1000.0}");
                }

                this.disposed = true;
            }
        }
    }
}
