// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace M6502.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

            ////configuration.DebugMode = true;

            var harness = new TestHarness(configuration);
            harness.Run();
        }
    }
}
