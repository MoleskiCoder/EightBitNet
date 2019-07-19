namespace Fuse
{
    using System.Collections.Generic;
    using System.IO;

    public class Tests
    {
        public void Read(string path)
        {
            using (var file = new StreamReader(path))
            {
                this.Read(file);
            }
        }

        public Dictionary<string, Test> Container { get; } = new Dictionary<string, Test>();

        public void Read(StreamReader file)
        {
            while (!file.EndOfStream)
            {
                var test = new Test();
                test.Read(file);
                if (test.Valid)
                {
                    this.Container.Add(test.Description, test);
                }
            }
        }
    }
}
