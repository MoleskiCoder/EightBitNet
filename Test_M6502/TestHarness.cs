namespace Test
{
    internal class TestHarness
    {
        private Board board;

        public TestHarness(Configuration configuration)
        {
            board = new Board(configuration);
        }

        public void Run()
        {
            board.Initialize();
            board.RaisePOWER();

            var cpu = board.CPU;

            while (cpu.Powered)
                cpu.Step();
        }
    }
}
