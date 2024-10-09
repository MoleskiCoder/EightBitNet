namespace M6502.HarteTest
{
    public sealed class Test
    {
        public string? Name { get; set; }

        public State? Initial { get; set; }

        public State? Final { get; set; }

        public List<List<object>>? Cycles { get; set; }

        public IEnumerable<Cycle> AvailableCycles()
        {
            if (Cycles == null)
            {
                throw new InvalidOperationException("Cycles have not been initialised");
            }

            foreach (var cycle in Cycles)
            {
                yield return new Cycle(cycle);
            }
        }
    }
}
