namespace Fuse
{
    public class TestSuite
    {
        private readonly Tests tests = new Tests();
        private readonly ExpectedTestResults results = new ExpectedTestResults();

        public TestSuite(string path)
        {
            this.tests.Read(path + ".in");
            this.results.Read(path + ".expected");
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
                var result = this.results.Results[key];
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
