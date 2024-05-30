namespace M6502.HarteTest
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal class OpcodeTestSuite(string path) : IDisposable
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        private bool disposed;

        public string Path { get; set; } = path;

        private readonly FileStream stream = File.Open(path, FileMode.Open);

        public IAsyncEnumerable<Test?> TestsAsync => JsonSerializer.DeserializeAsyncEnumerable<Test>(this.stream, SerializerOptions);

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.stream.Dispose();
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
