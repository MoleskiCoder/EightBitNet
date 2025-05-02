namespace Z80.HarteTest
{
    internal sealed class Test
    {
        public string? Name { get; set; }

        public State? Initial { get; set; }

        public State? Final { get; set; }

        public List<List<object>>? Cycles { get; set; }

        public List<List<object>>? Ports { get; set; }

        public IEnumerable<Cycle> AvailableCycles()
        {
            if (this.Cycles is null)
            {
                throw new InvalidOperationException("Cycles have not been initialised");
            }

            foreach (var cycle in this.Cycles)
            {
                yield return new Cycle(cycle);
            }
        }

        public IEnumerable<Port> AvailablePorts()
        {
            if (this.Ports is null)
            {
                throw new InvalidOperationException("Ports have not been initialised");
            }

            foreach (var port in this.Ports)
            {
                yield return new Port(port);
            }
        }
    }
}
