namespace Fuse
{
    using System.Collections.Generic;

    public class Results
    {
        private readonly Lines lines;

        public Dictionary<string, Result> Container { get; } = new Dictionary<string, Result>();

        public Results(string path) => this.lines = new Lines(path);

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var result = new Result();
                result.Parse(this.lines);
                if (result.Valid)
                {
                    this.Container.Add(result.Description, result);
                }
            }
        }
    }
}
