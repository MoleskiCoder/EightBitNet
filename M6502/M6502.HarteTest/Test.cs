namespace M6502.HarteTest
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by JSON deserializer")]
    internal sealed class Test
    {
        public string? Name { get; set; }

        public State? Initial { get; set; }

        public State? Final { get; set; }

        public List<List<object>>? Cycles { get; set; }

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
    }
}
