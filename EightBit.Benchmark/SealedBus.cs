using BenchmarkDotNet.Attributes;

namespace EightBit.Benchmark
{
    class SealedBus : Bus
    {
        private readonly Ram _ram = new(0x10000);
        private readonly MemoryMapping _mapping;

        public SealedBus()
        {
            this._mapping = new(this._ram, 0, Mask.Fifteen, AccessLevel.ReadWrite);
        }

        public override MemoryMapping Mapping(ushort absolute)
        {
            return this._mapping;
        }

        public override void Initialize()
        {
        }
    }
}