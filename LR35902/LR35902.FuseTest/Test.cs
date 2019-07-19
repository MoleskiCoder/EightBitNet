namespace Fuse
{
    using System.Collections.Generic;
    using System.IO;

    public class Test
    {
        public bool Valid => !string.IsNullOrEmpty(this.Description);

        public string Description { get; private set; }

        public RegisterState RegisterState { get; } = new RegisterState();

        public List<MemoryDatum> MemoryData { get; } = new List<MemoryDatum>();

        public void Read(StreamReader file)
        {
            while ((string.IsNullOrWhiteSpace(this.Description) || this.Description.StartsWith(";")) && !file.EndOfStream)
            {
                this.Description = file.ReadLine();
            }

            if (!this.Valid)
            {
                return;
            }

            this.RegisterState.Read(file);

            var finished = false;
            do
            {
                var line = file.ReadLine();
                finished = line == "-1";
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
