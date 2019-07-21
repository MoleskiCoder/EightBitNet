namespace Fuse
{
    using System;
    using System.Collections.Generic;

    public class Test
    {
        private readonly List<MemoryDatum> memoryData = new List<MemoryDatum>();

        public string Description { get; private set; }

        public RegisterState RegisterState { get; } = new RegisterState();

        public IReadOnlyCollection<MemoryDatum> MemoryData => this.memoryData.AsReadOnly();

        public bool TryParse(Lines lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            while (!lines.EndOfFile && string.IsNullOrEmpty(this.Description))
            {
                this.Description = lines.ReadLine();
            }

            if (string.IsNullOrEmpty(this.Description))
            {
                return false;
            }

            this.RegisterState.Parse(lines);

            var finished = false;
            do
            {
                var line = lines.ReadLine();
                finished = line == "-1";
                if (!finished)
                {
                    var datum = new MemoryDatum();
                    datum.Parse(line);
                    this.memoryData.Add(datum);
                }
            }
            while (!finished);

            return true;
        }
    }
}
