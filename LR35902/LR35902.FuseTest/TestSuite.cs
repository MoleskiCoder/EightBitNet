// <copyright file="TestSuite.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.FuseTest
{
    using Fuse;

    public class TestSuite<T>
        where T : IRegisterState, new()
    {
        private readonly Tests<T> tests;
        private readonly Results<T> results;

        public TestSuite(string path)
        {
            this.tests = new Tests<T>(path + ".in");
            this.results = new Results<T>(path + ".expected");
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
                Console.Out.WriteLine($"** Checking: {key}");

                var input = test.Value;
                var result = this.results.Container[key];
                var runner = new TestRunner<T>(input, result);

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

            Console.Out.WriteLine($"+++ Failed test count: {failedCount}");
            Console.Out.WriteLine($"+++ Unimplemented test count: {unimplementedCount}");
        }
    }
}
