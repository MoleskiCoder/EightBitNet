// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.Test
{
    using EightBit;

    internal static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            var board = new Board(configuration);
            var harness = new TestHarness<Board, Z80>(board, board.CPU);
            harness.Run();
        }
    }
}
