namespace Fuse
{
    using System.Collections.Generic;

    public class Result
    {
        private readonly TestEvents events = new TestEvents();

        public bool Valid => !string.IsNullOrEmpty(this.Description);

        public string Description { get; private set; }

        public RegisterState RegisterState { get; } = new RegisterState();

        public List<MemoryDatum> MemoryData { get; } = new List<MemoryDatum>();

        public void Parse(Lines lines)
        {
            while (!lines.EndOfFile && !this.Valid)
            {
                this.Description = lines.ReadLine();
            }

            if (!this.Valid)
            {
                return;
            }

            this.events.Parse(lines);
            this.RegisterState.Parse(lines);

            var finished = false;
            do
            {
                var line = lines.ReadLine();
                finished = string.IsNullOrWhiteSpace(line);
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
