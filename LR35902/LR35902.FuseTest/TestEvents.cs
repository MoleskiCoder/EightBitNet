namespace Fuse
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class TestEvents
    {
        private readonly List<TestEvent> container = new List<TestEvent>();

        public ReadOnlyCollection<TestEvent> Container => this.container.AsReadOnly();

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
                    this.container.Add(e);
                }
            }
            while (!complete);
        }
    }
}
