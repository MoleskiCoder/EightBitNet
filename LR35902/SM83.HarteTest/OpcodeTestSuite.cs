namespace SM83.HarteTest
{
    using System.Runtime.CompilerServices;
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

        public string Path { get; } = path;

        private readonly FileStream _stream = File.Open(path, FileMode.Open);

        public ConfiguredCancelableAsyncEnumerable<Test?> TestsAsync => JsonSerializer.DeserializeAsyncEnumerable<Test>(this._stream, SerializerOptions).ConfigureAwait(false);

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this._stream.Dispose();
                }

                this._disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
