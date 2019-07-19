namespace Fuse
{
    using System.Collections.Generic;
    using System.IO;

    public class TestEvents
    {
        public List<TestEvent> Events { get; } = new List<TestEvent>();

        public void Read(StreamReader file)
        {
            var complete = false;
            do
            {
                var e = new TestEvent();
                e.Read(file);
		        complete = !e.Valid;
                if (!complete)
                {
                    this.Events.Add(e);
                }
            }
            while (!complete);
        }
    }
}
