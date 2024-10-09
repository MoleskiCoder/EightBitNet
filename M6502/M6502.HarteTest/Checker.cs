namespace M6502.HarteTest
{
    using System.Diagnostics;

    internal sealed class Checker
    {
        private TestRunner Runner { get; }

        private Symbols.Parser Symbols { get; } = new();

        private Disassembler Disassembler { get; }

        private bool CycleCountMismatch { get; set; }

        public int Cycles { get; private set; }

        public bool Valid { get; private set; }

        public bool Invalid => !Valid;

        public bool Unimplemented => Invalid && CycleCountMismatch && (Cycles == 1);

        public bool Implemented => !Unimplemented;

        public List<string> Messages { get; } = [];

        private List<Cycle> ActualCycles { get; } = [];

        public Checker(TestRunner runner)
        {
            Runner = runner;
            Disassembler = new(Runner, (M6502.Core)Runner.CPU, Symbols);
        }

        public void Check(Test test)
        {
            var cpu = Runner.CPU;

            Reset();

            Runner.RaisePOWER();
            InitialiseState(test);
            var pc = cpu.PC.Word;

            Cycles = cpu.Step();
            Runner.LowerPOWER();

            Valid = CheckState(test);

            if (Unimplemented)
            {
                Messages.Add("Unimplemented");
                return;
            }

            Debug.Assert(Implemented);
            if (Invalid)
            {
                AddDisassembly(pc);

                var final = test.Final ?? throw new InvalidOperationException("Final test state cannot be null");

                Raise("PC", final.PC, cpu.PC.Word);
                Raise("S", final.S, cpu.S);
                Raise("A", final.A, cpu.A);
                Raise("X", final.X, cpu.X);
                Raise("Y", final.Y, cpu.Y);
                Raise("P", final.P, cpu.P);

                if (test.Cycles == null)
                {
                    throw new InvalidOperationException("test cycles cannot be null");
                }

                Messages.Add($"Fixed page is: {cpu.FixedPage:X2}");

                Messages.Add($"Stepped cycles: {Cycles}, expected events: {test.Cycles.Count}, actual events: {ActualCycles.Count}");

                DumpCycles("-- Expected cycles", test.AvailableCycles());
                DumpCycles("-- Actual cycles", ActualCycles);
            }
        }

        private void Reset()
        {
            Messages.Clear();
            ActualCycles.Clear();

            CycleCountMismatch = false;
            Cycles = 0;
            Valid = false;
        }

        private bool Check(string what, ushort expected, ushort actual)
        {
            var success = actual == expected;
            if (!success)
            {
                Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, byte expected, byte actual)
        {
            var success = actual == expected;
            if (!success)
            {
                Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, string? expected, string? actual)
        {
            ArgumentNullException.ThrowIfNull(expected);
            ArgumentNullException.ThrowIfNull(actual);
            var success = actual == expected;
            if (!success)
            {
                Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, ushort address, byte expected, byte actual)
        {
            var success = actual == expected;
            if (!success)
            {
                Raise($"{what}: {address}", expected, actual);
            }
            return success;
        }

        private void AddDisassembly(ushort address)
        {
            string message;
            try
            {
                message = Disassemble(address);
            }
            catch (InvalidOperationException error)
            {
                message = $"Disassembly problem: {error.Message}";
            }

            Messages.Add(message);
        }

        private string Disassemble(ushort address) => Disassembler.Disassemble(address);

        private bool CheckState(Test test)
        {
            var cpu = Runner.CPU;
            var ram = Runner.RAM;

            var expectedCycles = test.AvailableCycles();
            var actualCycles = ActualCycles;

            var actualIDX = 0;
            foreach (var expectedCycle in expectedCycles) {

                if (actualIDX >= actualCycles.Count)
                {
                    CycleCountMismatch = true;
                    return false; // more expected cycles than actual
                }

                var actualCycle = actualCycles[actualIDX++];

                var expectedAddress = expectedCycle.Address;
                var actualAddress = actualCycle.Address;
                _ = Check("Cycle address", expectedAddress, actualAddress);

                var expectedValue = expectedCycle.Value;
                var actualValue = actualCycle.Value;
                _ = Check("Cycle value", expectedValue, actualValue);

                var expectedAction = expectedCycle.Type;
                var actualAction = actualCycle.Type;
                _ = Check("Cycle action", expectedAction, actualAction);
            }

            if (actualIDX < actualCycles.Count)
            {
                CycleCountMismatch = true;
                return false; // less expected cycles than actual
            }

            if (Messages.Count > 0)
            {
                return false;
            }

            var final = test.Final ?? throw new InvalidOperationException("Final state cannot be null");
            var pc_good = Check("PC", final.PC, cpu.PC.Word);
            var s_good = Check("S", final.S, cpu.S);
            var a_good = Check("A", final.A, cpu.A);
            var x_good = Check("X", final.X, cpu.X);
            var y_good = Check("Y", final.Y, cpu.Y);
            var p_good = Check("P", final.P, cpu.P);

            if (!p_good)
            {
                Messages.Add($"Expected flags: {Disassembler.DumpFlags(final.P)}");
                Messages.Add($"Actual flags  : {Disassembler.DumpFlags(cpu.P)}");
            }

            if (final.RAM == null)
            {
                throw new InvalidOperationException("Expected RAM cannot be null");
            }

            var ramProblem = false;
            foreach (var entry in final.RAM)
            {
                if (entry.Length != 2)
                {
                    throw new InvalidOperationException("RAM entry length must be 2");
                }

                var address = (ushort)entry[0];
                var value = (byte)entry[1];

                var ramGood = Check("RAM", address, value, ram.Peek(address));
                if (!ramGood && !ramProblem)
                {
                    ramProblem = true;
                }
            }

            return
                pc_good && s_good
                && a_good && x_good && y_good && p_good
                && !ramProblem;
        }

        private void Raise(string what, ushort expected, ushort actual) => Messages.Add($"{what}: expected: {expected:X4}, actual: {actual:X4}");

        private void Raise(string what, byte expected, byte actual) => Messages.Add($"{what}: expected: {expected:X2}, actual: {actual:X2}");

        private void Raise(string what, string expected, string actual) => Messages.Add($"{what}: expected: {expected}, actual: {actual}");

        public void Initialise()
        {
            Runner.ReadByte += Runner_ReadByte;
            Runner.WrittenByte += Runner_WrittenByte;
        }

        private void InitialiseState(Test test)
        {
            var initial = test.Initial ?? throw new InvalidOperationException("Test cannot have an invalid initial state");
            InitialiseState(initial);
        }

        private void InitialiseState(State state)
        {
            var cpu = Runner.CPU;
            var ram = Runner.RAM;

            cpu.PC.Word = state.PC;
            cpu.S = state.S;
            cpu.A = state.A;
            cpu.X = state.X;
            cpu.Y = state.Y;
            cpu.P = state.P;

            var initialRAM = state.RAM ?? throw new InvalidOperationException("Initial test state cannot have invalid RAM");
            foreach (var entry in initialRAM)
            {
                var count = entry.Length;
                if (count != 2)
                {
                    throw new InvalidOperationException("RAM entry length must be 2");
                }

                var address = (ushort)entry[0];
                var value = (byte)entry[1];
                ram.Poke(address, value);
            }
        }

        private void Runner_ReadByte(object? sender, EventArgs e) => AddActualReadCycle(Runner.Address, Runner.Data);

        private void Runner_WrittenByte(object? sender, EventArgs e) => AddActualWriteCycle(Runner.Address, Runner.Data);

        private void AddActualReadCycle(EightBit.Register16 address, byte value) => AddActualCycle(address, value, "read");

        private void AddActualWriteCycle(EightBit.Register16 address, byte value) => AddActualCycle(address, value, "write");

        private void AddActualCycle(EightBit.Register16 address, byte value, string action) => AddActualCycle(address.Word, value, action);

        private void AddActualCycle(ushort address, byte value, string action) => ActualCycles.Add(new Cycle(address, value, action));

        private void DumpCycle(ushort address, byte value, string? action)
        {
            ArgumentNullException.ThrowIfNull(action);
            Messages.Add($"Address: {address:X4}, value: {value:X2}, action: {action}");
        }

        private void DumpCycle(Cycle cycle) => DumpCycle(cycle.Address, cycle.Value, cycle.Type);

        private void DumpCycles(IEnumerable<Cycle>? cycles)
        {
            ArgumentNullException.ThrowIfNull(cycles);
            foreach (var cycle in cycles)
            {
                DumpCycle(cycle);
            }
        }

        private void DumpCycles(string which, IEnumerable<Cycle>? events)
        {
            Messages.Add(which);
            DumpCycles(events);
        }
    }
}
