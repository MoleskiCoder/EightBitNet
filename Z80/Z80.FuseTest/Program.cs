// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Fuse
{
    class Program
    {
        static void Main(string[] args)
        {
            var suite = new TestSuite("fuse-tests\\tests");
            suite.Read();
            suite.Parse();
            suite.Run();
        }
    }
}
