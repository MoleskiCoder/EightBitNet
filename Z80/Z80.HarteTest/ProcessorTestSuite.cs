namespace Z80.HarteTest
{
    internal sealed class ProcessorTestSuite(string location)
    {
        public string Location { get; set; } = location;

        public IEnumerable<OpcodeTestSuite> OpcodeTests()
        {
            //var pattern = "dd 66.json";
            //var pattern = "06.json";
            var pattern = "*.json";
            foreach (var filename in Directory.EnumerateFiles(this.Location, pattern))
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
