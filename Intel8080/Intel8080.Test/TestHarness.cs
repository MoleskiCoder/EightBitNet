// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Intel8080.Test
{
    using System.Diagnostics;

    internal class TestHarness(Configuration configuration)
    {
        private readonly Stopwatch timer = new();
        private readonly Board board = new(configuration);
        private long totalCycles;

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

            System.Console.Out.WriteLine($"\n\nGuest cycles = {this.totalCycles}");
            System.Console.Out.WriteLine($"Seconds = {this.timer.ElapsedMilliseconds / 1000.0}");
        }
    }
}
