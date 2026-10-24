namespace EightBit.Benchmark
{
    using BenchmarkDotNet.Attributes;

    internal class SealedLittleEndianProcessor : LittleEndianProcessor
    {
        private readonly Bus _contents;

        public SealedLittleEndianProcessor(Bus contents)
        : base(contents)
        {
            _contents = contents;
        }

        public override void PoweredStep()
        {
            this.Execute(this.FetchInstruction());
        }

        public override void Execute()
        {
            this.Tick();
            this.MemoryWrite(this.PC, 1);
        }

        protected override void Push(byte value)
        {
        }

        protected override void Pop()
        {
        }

        protected override void MemoryRead()
        {
            this.OnReadingMemory();
            base.MemoryRead();
            this.OnReadMemory();
        }

        protected override void MemoryWrite()
        {
            this.OnWritingMemory();
            base.MemoryWrite();
            this.OnWrittenMemory();
        }
    }

    public class BenchmarkLittleEndianProcessor
    {
        private readonly SealedBus _bus = new();
        private readonly SealedLittleEndianProcessor _processor;

        public BenchmarkLittleEndianProcessor()
        {
            this._processor = new(this._bus);
        }

        ////////////////////////////////////////////

        [Benchmark]
        public long Step()
        {
            this._processor.Step();
            return this._processor.Cycles;
        }

        ////////////////////////////////////////////

        [GlobalSetup(Target = nameof(StepWithMemoryEvents))]
        public void GlobalSetupStepWithMemoryEvents() => this.AddMemoryEvents();

        [GlobalCleanup(Target = nameof(StepWithMemoryEvents))]
        public void GlobalCleanupStepWithMemoryEvents() => this.RemoveMemoryEvents();

        [Benchmark]
        public long StepWithMemoryEvents() => this.Step();

        ////////////////////////////////////////////

        [GlobalSetup(Target = nameof(StepWithExecutionEvents))]
        public void GlobalSetupStepWithExecutionEvents() => this.AddExecutionEvents();

        [GlobalCleanup(Target = nameof(StepWithExecutionEvents))]
        public void GlobalCleanupStepWithExecutionEvents() => this.RemoveExecutionEvents();

        [Benchmark]
        public long StepWithExecutionEvents() => this.Step();

        ////////////////////////////////////////////

        [GlobalSetup(Target = nameof(StepWithEvents))]
        public void GlobalSetupStepWithEvents() => this.AddEvents();

        [GlobalCleanup(Target = nameof(StepWithEvents))]
        public void GlobalCleanupStepWithEvents() => this.RemoveEvents();

        [Benchmark]
        public long StepWithEvents() => this.Step();

        ///////////////////////////////////////////////////

        private void AddEvents()
        {
            this.AddMemoryEvents();
            this.AddExecutionEvents();
        }

        private void RemoveEvents()
        {
            this.RemoveMemoryEvents();
            this.RemoveExecutionEvents();
        }

        ///////////////////////////////////////////////////

        private int _preExecute = 0;
        private int _postExecute = 0;

        private void RemoveExecutionEvents()
        {
            this._processor.ExecutingInstruction -= Processor_ExecutingInstruction;
            this._processor.ExecutedInstruction -= Processor_ExecutedInstruction;
        }

        private void AddExecutionEvents()
        {
            this._processor.ExecutingInstruction += Processor_ExecutingInstruction;
            this._processor.ExecutedInstruction += Processor_ExecutedInstruction;
        }

        private void Processor_ExecutedInstruction(object? sender, EventArgs e) =>this._postExecute++;

        private void Processor_ExecutingInstruction(object? sender, EventArgs e) => this._preExecute++;

        ///////////////////////////////////////////////////

        private int _preReads = 0;
        private int _postReads = 0;

        private int _preWrites = 0;
        private int _postWrites = 0;

        private void RemoveMemoryEvents()
        {
            this._processor.ReadingMemory -= Processor_ReadingMemory;
            this._processor.ReadMemory -= Processor_ReadMemory;
            this._processor.WritingMemory -= Processor_WritingMemory;
            this._processor.WrittenMemory -= Processor_WrittenMemory;
        }

        private void AddMemoryEvents()
        {
            this._processor.ReadingMemory += Processor_ReadingMemory;
            this._processor.ReadMemory += Processor_ReadMemory;
            this._processor.WritingMemory += Processor_WritingMemory;
            this._processor.WrittenMemory += Processor_WrittenMemory;
        }

        private void Processor_ReadingMemory(object? sender, EventArgs e) => this._preReads++;

        private void Processor_ReadMemory(object? sender, EventArgs e) => this._postReads++;

        private void Processor_WrittenMemory(object? sender, EventArgs e) =>this._postWrites++;

        private void Processor_WritingMemory(object? sender, EventArgs e) =>this._preWrites++;
    }
}