﻿namespace M6502.HarteTest
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

        public bool Invalid => !this.Valid;

        public bool Unimplemented => this.Invalid && this.CycleCountMismatch && (this.Cycles == 1);

        public bool Implemented => !this.Unimplemented;

        public List<string> Messages { get; } = [];

        private List<Cycle> ActualCycles { get; } = [];

        public Checker(TestRunner runner)
        {
            this.Runner = runner;
            this.Disassembler = new(this.Runner, this.Runner.CPU, this.Symbols);
        }

        public void Check(Test test)
        {
            var cpu = this.Runner.CPU;

            this.Reset();

            this.Runner.RaisePOWER();
            this.InitialiseState(test);
            var pc = cpu.PC.Word;

            this.Cycles = cpu.Step();
            this.Runner.LowerPOWER();

            this.Valid = this.CheckState(test);

            if (this.Unimplemented)
            {
                this.Messages.Add("Unimplemented");
                return;
            }

            Debug.Assert(this.Implemented);
            if (this.Invalid)
            {
                this.AddDisassembly(pc);

                var final = test.Final ?? throw new InvalidOperationException("Final test state cannot be null");

                this.Raise("PC", final.PC, cpu.PC.Word);
                this.Raise("S", final.S, cpu.S);
                this.Raise("A", final.A, cpu.A);
                this.Raise("X", final.X, cpu.X);
                this.Raise("Y", final.Y, cpu.Y);
                this.Raise("P", final.P, cpu.P);

                if (test.Cycles is null)
                {
                    throw new InvalidOperationException("test cycles cannot be null");
                }

                this.Messages.Add($"Fixed page is: {cpu.FixedPage:X2}");

                this.Messages.Add($"Stepped cycles: {this.Cycles}, expected events: {test.Cycles.Count}, actual events: {this.ActualCycles.Count}");

                this.DumpCycles("-- Expected cycles", test.AvailableCycles());
                this.DumpCycles("-- Actual cycles", this.ActualCycles);
            }
        }

        private void Reset()
        {
            this.Messages.Clear();
            this.ActualCycles.Clear();

            this.CycleCountMismatch = false;
            this.Cycles = 0;
            this.Valid = false;
        }

        private bool Check(string what, ushort expected, ushort actual)
        {
            var success = actual == expected;
            if (!success)
            {
                this.Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, byte expected, byte actual)
        {
            var success = actual == expected;
            if (!success)
            {
                this.Raise(what, expected, actual);
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
                this.Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, ushort address, byte expected, byte actual)
        {
            var success = actual == expected;
            if (!success)
            {
                this.Raise($"{what}: {address}", expected, actual);
            }
            return success;
        }

        private void AddDisassembly(ushort address)
        {
            string message;
            try
            {
                message = this.Disassemble(address);
            }
            catch (InvalidOperationException error)
            {
                message = $"Disassembly problem: {error.Message}";
            }

            this.Messages.Add(message);
        }

        private string Disassemble(ushort address) => this.Disassembler.Disassemble(address);

        private bool CheckState(Test test)
        {
            var cpu = this.Runner.CPU;
            var ram = this.Runner.RAM;

            var expectedCycles = test.AvailableCycles();
            var actualCycles = this.ActualCycles;

            var actualIDX = 0;
            foreach (var expectedCycle in expectedCycles)
            {

                if (actualIDX >= actualCycles.Count)
                {
                    this.CycleCountMismatch = true;
                    return false; // more expected cycles than actual
                }

                var actualCycle = actualCycles[actualIDX++];

                var expectedAddress = expectedCycle.Address;
                var actualAddress = actualCycle.Address;
                _ = this.Check("Cycle address", expectedAddress, actualAddress);

                var expectedValue = expectedCycle.Value;
                var actualValue = actualCycle.Value;
                _ = this.Check("Cycle value", expectedValue, actualValue);

                var expectedAction = expectedCycle.Type;
                var actualAction = actualCycle.Type;
                _ = this.Check("Cycle action", expectedAction, actualAction);
            }

            if (actualIDX < actualCycles.Count)
            {
                this.CycleCountMismatch = true;
                return false; // less expected cycles than actual
            }

            if (this.Messages.Count > 0)
            {
                return false;
            }

            var final = test.Final ?? throw new InvalidOperationException("Final state cannot be null");
            var pc_good = this.Check("PC", final.PC, cpu.PC.Word);
            var s_good = this.Check("S", final.S, cpu.S);
            var a_good = this.Check("A", final.A, cpu.A);
            var x_good = this.Check("X", final.X, cpu.X);
            var y_good = this.Check("Y", final.Y, cpu.Y);
            var p_good = this.Check("P", final.P, cpu.P);

            if (!p_good)
            {
                this.Messages.Add($"Expected flags: {Disassembler.DumpFlags(final.P)}");
                this.Messages.Add($"Actual flags  : {Disassembler.DumpFlags(cpu.P)}");
            }

            if (final.RAM is null)
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

                var ramGood = this.Check("RAM", address, value, ram.Peek(address));
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

        private void Raise(string what, ushort expected, ushort actual) => this.Messages.Add($"{what}: expected: {expected:X4}, actual: {actual:X4}");

        private void Raise(string what, byte expected, byte actual) => this.Messages.Add($"{what}: expected: {expected:X2}, actual: {actual:X2}");

        private void Raise(string what, string expected, string actual) => this.Messages.Add($"{what}: expected: {expected}, actual: {actual}");

        public void Initialise()
        {
            this.Runner.ReadByte += this.Runner_ReadByte;
            this.Runner.WrittenByte += this.Runner_WrittenByte;
        }

        private void InitialiseState(Test test)
        {
            var initial = test.Initial ?? throw new InvalidOperationException("Test cannot have an invalid initial state");
            this.InitialiseState(initial);
        }

        private void InitialiseState(State state)
        {
            var cpu = this.Runner.CPU;
            var ram = this.Runner.RAM;

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

        private void Runner_ReadByte(object? sender, EventArgs e) => this.AddActualCycle(this.Runner.Address, this.Runner.Data, "read");

        private void Runner_WrittenByte(object? sender, EventArgs e) => this.AddActualCycle(this.Runner.Address, this.Runner.Data, "write");

        private void AddActualCycle(EightBit.Register16 address, byte value, string action) => this.AddActualCycle(address.Word, value, action);

        private void AddActualCycle(ushort address, byte value, string action) => this.ActualCycles.Add(new Cycle(address, value, action));

        private void DumpCycle(ushort address, byte value, string? action)
        {
            ArgumentNullException.ThrowIfNull(action);
            this.Messages.Add($"Address: {address:X4}, value: {value:X2}, action: {action}");
        }

        private void DumpCycle(Cycle cycle) => this.DumpCycle(cycle.Address, cycle.Value, cycle.Type);

        private void DumpCycles(IEnumerable<Cycle>? cycles)
        {
            ArgumentNullException.ThrowIfNull(cycles);
            foreach (var cycle in cycles)
            {
                this.DumpCycle(cycle);
            }
        }

        private void DumpCycles(string which, IEnumerable<Cycle>? events)
        {
            this.Messages.Add(which);
            this.DumpCycles(events);
        }
    }
}
