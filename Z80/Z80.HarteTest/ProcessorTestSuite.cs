namespace Z80.HarteTest
{
    internal sealed class ProcessorTestSuite(string location)
    {
        public string Location { get; set; } = location;

        public IEnumerable<OpcodeTestSuite> OpcodeTests()
        {
            foreach (var filename in Directory.EnumerateFiles(this.Location, "*.json"))
            {
                var fileInformation = new FileInfo(filename);
                if (fileInformation.Length > 0)
                {
                    yield return new OpcodeTestSuite(filename);
                }
            }
        }
    }
}
