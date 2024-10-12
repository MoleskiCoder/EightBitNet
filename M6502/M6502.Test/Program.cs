// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = false;
#endif
            var board = new Board(configuration);
            var harness = new EightBit.TestHarness<Board, M6502.Core>(board, board.CPU);
            harness.Run();
        }
    }
}
