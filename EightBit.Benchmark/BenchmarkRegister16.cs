using BenchmarkDotNet.Attributes;

namespace EightBit.Benchmark
{
    public class BenchmarkRegister16
    {
        private readonly Register16 _contents = new(0xde, 0xad);
        private readonly Register16 _source = new(0xbe, 0xef);

        [Benchmark]
        public ushort Increment()
        {
            this._contents.Increment();
            return this._contents.Joined;
        }

        [Benchmark]
        public ushort AssignViaRegister()
        {
            this._source.Increment();
            this._contents.Assign(this._source);
            return this._contents.Joined;
        }

        [Benchmark]
        public ushort AssignViaValues()
        {
            this._contents.Assign(0x01, 0x02);
            return this._contents.Joined;
        }
    }
}