// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using MC6809.Test;

    internal static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            var board = new Board(configuration);
            var harness = new EightBit.TestHarness<Board, MC6809.MC6809>(board, board.CPU);
            harness.Run();
        }
    }
}
