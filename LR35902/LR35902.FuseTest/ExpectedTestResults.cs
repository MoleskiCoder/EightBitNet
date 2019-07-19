namespace Fuse
{
    using System.Collections.Generic;
    using System.IO;

    public class ExpectedTestResults
    {
        public Dictionary<string, ExpectedTestResult> Results { get; } = new Dictionary<string, ExpectedTestResult>();

        public void Read(string path)
        {
            using (var file = new StreamReader(path))
            {
                this.Read(file);
            }
        }

        private void Read(StreamReader file)
        {
            var finished = false;
            while (!file.EndOfStream)
            {
                var result = new ExpectedTestResult();
                result.Read(file);
                finished = result.Finish;
                if (!finished)
                {
                    this.Results[result.Description] = result;
                }
            }
        }
    }
}
