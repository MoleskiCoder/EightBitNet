namespace Fuse
{
    using System.Collections.Generic;

    public class TestEvents
    {
        public List<TestEvent> Container { get; } = new List<TestEvent>();

        public void Parse(Lines lines)
        {
            var complete = false;
            do
            {
                var e = new TestEvent();
                e.Parse(lines);
		        complete = !e.Valid;
                if (!complete)
                {
                    this.Container.Add(e);
                }
            }
            while (!complete);
        }
    }
}
