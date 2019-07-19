namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class ExpectedTestResult
    {
        private readonly TestEvents events = new TestEvents();

        public bool Finish { get; private set; } = false;

        public string Description { get; private set; }

        public RegisterState RegisterState { get; } = new RegisterState();

        public List<MemoryDatum> MemoryData { get; } = new List<MemoryDatum>();

        public void Read(StreamReader file)
        {
            this.Finish = false;
            do
            {
                this.Description = file.ReadLine();
                this.Finish = file.EndOfStream;
            }
            while (string.IsNullOrEmpty(this.Description) && !this.Finish);

            if (this.Finish)
            {
                return;
            }

            this.events.Read(file);
            this.RegisterState.Read(file);

            var line = file.ReadLine();
            if (line.Length > 0)
            {
                throw new InvalidOperationException("EOL swallow failure!!");
            }

            var finished = false;
            do
            {
                line = file.ReadLine();
                finished = string.IsNullOrEmpty(line);
                if (!finished)
                {
                    var datum = new MemoryDatum();
                    datum.Parse(line);
                    this.MemoryData.Add(datum);
                }
            }
            while (!finished);
        }
    }
}
