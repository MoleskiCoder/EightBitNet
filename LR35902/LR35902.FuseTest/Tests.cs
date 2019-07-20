namespace Fuse
{
    using System.Collections.Generic;

    public class Tests
    {
        private readonly Lines lines;

        public Dictionary<string, Test> Container { get; } = new Dictionary<string, Test>();

        public Tests(string path) => this.lines = new Lines(path);

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var test = new Test();
                test.Parse(this.lines);
                if (test.Valid)
                {
                    this.Container.Add(test.Description, test);
                }
            }
        }
    }
}
