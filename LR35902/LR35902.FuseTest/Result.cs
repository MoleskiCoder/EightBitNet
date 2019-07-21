namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Result
    {
        private readonly TestEvents events = new TestEvents();
        private readonly List<MemoryDatum> memoryData = new List<MemoryDatum>();

        public bool Valid => !string.IsNullOrEmpty(this.Description);

        public string Description { get; private set; }

        public RegisterState RegisterState { get; } = new RegisterState();

        public ReadOnlyCollection<MemoryDatum> MemoryData => this.memoryData.AsReadOnly();

        public void Parse(Lines lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

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
                    this.memoryData.Add(datum);
                }
            }
            while (!finished);
        }
    }
}
