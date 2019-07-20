namespace Fuse
{
    using System.Collections.Generic;
    using System.IO;

    public class Lines
    {
        private readonly string path;
        private readonly List<string> lines = new List<string>();
        private int position = -1;

        public Lines(string path) => this.path = path;

        public bool EndOfFile => this.position == this.lines.Count;

        public void Read()
        {
            using (var reader = new StreamReader(this.path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var ignored = line.StartsWith(";");
                    if (!ignored)
                    {
                        this.lines.Add(line);
                    }
                }
            }
            if (this.lines.Count > 0)
            {
                this.position = 0;
            }
        }

        public string ReadLine()
        {
            try
            {
                return this.PeekLine();
            }
            finally
            {
                this.Increment();
            }
        }

        public void UnreadLine() => this.Decrement();

        public string PeekLine() => this.lines[this.position];

        private void Increment() => ++this.position;

        private void Decrement() => --this.position;
    }
}
