namespace M6502.HarteTest
{
    internal sealed class ProcessorTestSuite(string location)
    {
        public string Location { get; set; } = location;

        public IEnumerable<OpcodeTestSuite> OpcodeTests()
        {
            foreach (var filename in Directory.EnumerateFiles(this.Location, "*.json"))
            {
                yield return new OpcodeTestSuite(filename);
            }
        }
    }
}
