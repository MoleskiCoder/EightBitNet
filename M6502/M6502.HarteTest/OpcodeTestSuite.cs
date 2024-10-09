namespace M6502.HarteTest
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal sealed class OpcodeTestSuite(string path) : IDisposable
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        private bool _disposed;

        public string Path { get; set; } = path;

        private readonly FileStream _stream = File.Open(path, FileMode.Open);

        public IAsyncEnumerable<Test?> TestsAsync => JsonSerializer.DeserializeAsyncEnumerable<Test>(_stream, SerializerOptions);

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _stream.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
