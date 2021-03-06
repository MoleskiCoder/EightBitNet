﻿// <copyright file="TestSuite.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    public class TestSuite
    {
        private readonly Tests<RegisterState> tests;
        private readonly Results<RegisterState> results;

        public TestSuite(string path)
        {
            this.tests = new Tests<RegisterState>(path + ".in");
            this.results = new Results<RegisterState>(path + ".expected");
        }

        public void Read()
        {
            this.tests.Read();
            this.results.Read();
        }

        public void Parse()
        {
            this.tests.Parse();
            this.results.Parse();
        }

        public void Run()
        {
            var failedCount = 0;
            var unimplementedCount = 0;
            foreach (var test in this.tests.Container)
            {
                var key = test.Key;
                System.Console.Out.WriteLine($"** Checking: {key}");

                var input = test.Value;
                var result = this.results.Container[key];
                var runner = new TestRunner(input, result);

                runner.Run();
                if (runner.Failed)
                {
                    ++failedCount;
                }

                if (runner.Unimplemented)
                {
                    ++unimplementedCount;
                }
            }

            System.Console.Out.WriteLine($"+++ Failed test count: {failedCount}");
            System.Console.Out.WriteLine($"+++ Unimplemented test count: {unimplementedCount}");
        }
    }
}
