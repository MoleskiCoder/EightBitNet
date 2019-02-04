// <copyright file="TestHarness.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Test
{
    internal class TestHarness
    {
        private Board board;

        public TestHarness(Configuration configuration)
        {
            this.board = new Board(configuration);
        }

        public void Run()
        {
            this.board.Initialize();
            this.board.RaisePOWER();

            var cpu = this.board.CPU;

            while (cpu.Powered)
            {
                cpu.Step();
            }
        }
    }
}
