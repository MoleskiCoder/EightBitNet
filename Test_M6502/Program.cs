namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new Configuration();

            configuration.DebugMode = true;

            var harness = new TestHarness(configuration);
            harness.Run();
        }
    }
}
