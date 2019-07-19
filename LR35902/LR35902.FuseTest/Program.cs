namespace Fuse
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            TestSuite testSuite = new TestSuite("fuse-tests\\tests");
            testSuite.Run();
        }
    }
}
