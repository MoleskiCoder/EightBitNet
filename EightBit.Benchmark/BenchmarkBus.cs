namespace EightBit.Benchmark
{
    using BenchmarkDotNet.Attributes;

    public class BenchmarkBus
    {
        private readonly SealedBus _contents = new();

        [GlobalSetup]
        public void GlobalSetup()
        {
            for (ushort i = 0; i < ushort.MaxValue; ++i)
            {
                this._contents.Poke(i, (byte)(i % 256));
            }
            this._contents.Address.Joined = 0;
            this._contents.Data = 123;
        }

        [Benchmark]
        public long Reading()
        {
            this._contents.Read();
            return this._contents.Data;
        }

        [Benchmark]
        public long Writing()
        {
            this._contents.Write();
            return this._contents.Data;
        }
    }
}