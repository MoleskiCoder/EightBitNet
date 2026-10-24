namespace EightBit.Benchmark
{
    using BenchmarkDotNet.Attributes;

    class ClockingChip : ClockedChip
    {
        public void Reset() => this.ResetCycles();

        public void Bump() => this.Cycles++;
    }

    public class BenchmarkClockedChip
    {
        private readonly ClockingChip _contents = new();

        ////////////////////////////////////////////

        [IterationSetup]
        public void IterationSetup() => this._contents.Reset();

        [IterationCleanup]
        public void IterationCleanup() => this._contents.Reset();

        ////////////////////////////////////////////

        [GlobalSetup(Target = nameof(TickWithSubscribers))]
        public void GlobalSetupSingle() => this._contents.Ticked += this.Contents_Ticked;

        [GlobalCleanup(Target = nameof(TickWithSubscribers))]
        public void GlobalCleanupSingle() => this._contents.Ticked -= this.Contents_Ticked;

        [Benchmark]
        public int TickWithSubscribers()
        {
            this._contents.Tick();
            return this._contents.Cycles;
        }

        ////////////////////////////////////////////

        [GlobalSetup(Target = nameof(TickMultipleWithSubscribers))]
        public void GlobalSetupMultiple() => this._contents.Ticked += this.Contents_Ticked;

        [GlobalCleanup(Target = nameof(TickMultipleWithSubscribers))]
        public void GlobalCleanupMultiple() => this._contents.Ticked -= this.Contents_Ticked;

        [Benchmark]
        public int TickMultipleWithSubscribers()
        {
            this._contents.Tick(4);
            return this._contents.Cycles;
        }

        ////////////////////////////////////////////

        [Benchmark]
        public int TickWithoutSubscribers()
        {
            this._contents.Tick();
            return this._contents.Cycles;
        }

        [Benchmark]
        public int TickMultipleWithoutSubscribers()
        {
            this._contents.Tick(4);
            return this._contents.Cycles;
        }

        private void Contents_Ticked(object? sender, EventArgs e)
        {
            this._contents.Bump();
        }
    }
}