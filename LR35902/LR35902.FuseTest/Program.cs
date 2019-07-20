namespace Fuse
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var suite = new TestSuite("fuse-tests\\tests");
            suite.Read();
            suite.Parse();
            suite.Run();
        }
    }
}
