// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Intel8080.Test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            var harness = new TestHarness(configuration);
            harness.Run();
        }
    }
}
