﻿namespace M6502.HarteTest
{
    internal class Checker
    {
        private TestRunner Runner { get; }

        private EightBit.Symbols Symbols { get; } = new();

        private EightBit.Disassembler Disassembler { get; }

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
            this.Disassembler = new EightBit.Disassembler(this.Runner, this.Runner.CPU, this.Symbols);
        }

        public void Check(Test test)
        {
            var cpu = this.Runner.CPU;

            this.Messages.Clear();
            this.ActualCycles.Clear();

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

            if (this.Invalid && this.Implemented)
            {
                this.AddDisassembly(pc);

                var final = test.Final ?? throw new InvalidOperationException("Final test state cannot be null");

                this.Raise("PC", final.PC, cpu.PC.Word);
                this.Raise("S", final.S, cpu.S);
                this.Raise("A", final.A, cpu.A);
                this.Raise("X", final.X, cpu.X);
                this.Raise("Y", final.Y, cpu.Y);
                this.Raise("P", final.P, cpu.P);

                if (test.Cycles == null)
                {
                    throw new InvalidOperationException("test cycles cannot be null");
                }

                this.Messages.Add($"Stepped cycles: {this.Cycles}, expected events: {test.Cycles.Count}, actual events: {this.ActualCycles.Count}");

                this.DumpCycles("-- Expected cycles", test.AvailableCycles());
                this.DumpCycles("-- Actual cycles", this.ActualCycles);
            }
        }

        private bool Check<T>(string what, T expected, T actual)
        {
            ArgumentNullException.ThrowIfNull(expected);
            ArgumentNullException.ThrowIfNull(actual);
            var success = actual.Equals(expected);
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

        private void AddDisassembly(ushort address) => this.Messages.Add(this.Disassemble(address));

        private string Disassemble(ushort address) => this.Disassembler.Disassemble(address);

        private bool CheckState(Test test)
        {
            var cpu = this.Runner.CPU;
            var ram = this.Runner.RAM;

            var expected_cycles = test.AvailableCycles() ?? throw new InvalidOperationException("Expected cycles cannot be null");
            var actual_cycles = this.ActualCycles;

            var actual_idx = 0;
            foreach (var expected_cycle in expected_cycles) {

                if (actual_idx >= actual_cycles.Count)
                {
                    this.CycleCountMismatch = true;
                    return false; // more expected cycles than actual
                }

                var actual_cycle = actual_cycles[actual_idx++];

                var expected_address = expected_cycle.Address;
                var actual_address = actual_cycle.Address;
                this.Check("Cycle address", expected_address, actual_address);

                var expected_value = expected_cycle.Value;
                var actual_value = actual_cycle.Value;
                this.Check("Cycle value", expected_value, actual_value);

                var expected_action = expected_cycle.Type;
                var actual_action = actual_cycle.Type;
                this.Check("Cycle action", expected_action, actual_action);
            }

            if (actual_idx < actual_cycles.Count)
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

            if (final.RAM == null)
            {
                throw new InvalidOperationException("Expected RAM cannot be null");
            }

            var ram_problem = false;
            foreach (var entry in final.RAM)
            {

                var count = entry.Length;
                if (count != 2)
                {
                    throw new InvalidOperationException("RAM entry length must be 2");
                }

                var address = (ushort)entry[0];
                var value = (byte)entry[1];

                var ram_good = this.Check("RAM", address, value, ram.Peek(address));
                if (!ram_good && !ram_problem)
                {
                    ram_problem = true;
                }
            }

            return
                pc_good && s_good
                && a_good && x_good && y_good && p_good
                && !ram_problem;
        }

        private void Raise<T>(string what, T expected, T actual) => this.Messages.Add($"{what}: expected: {expected}, actual: {actual}");

        public void Initialise()
        {
            this.Runner.ReadByte += this.Runner_ReadByte;
            this.Runner.WrittenByte += this.Runner_WrittenByte;
        }

        private void InitialiseState(Test test)
        {
            var cpu = this.Runner.CPU;
            var ram = this.Runner.RAM;

            var initial = test.Initial ?? throw new InvalidOperationException("Test cannot have an invalid initial state");
            cpu.PC.Word = initial.PC;
            cpu.S = initial.S;
            cpu.A = initial.A;
            cpu.X = initial.X;
            cpu.Y = initial.Y;
            cpu.P = initial.P;

            var initialRAM = initial.RAM ?? throw new InvalidOperationException("Initial test state cannot have invalid RAM");
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

        private void Runner_ReadByte(object? sender, EventArgs e) => this.AddActualReadCycle(this.Runner.Address, this.Runner.Data);

        private void Runner_WrittenByte(object? sender, EventArgs e) => this.AddActualWriteCycle(this.Runner.Address, this.Runner.Data);

        private void AddActualReadCycle(EightBit.Register16 address, byte value) => this.AddActualCycle(address, value, "read");

        private void AddActualWriteCycle(EightBit.Register16 address, byte value) => this.AddActualCycle(address, value, "write");

        private void AddActualCycle(EightBit.Register16 address, byte value, string action) => this.AddActualCycle(address.Word, value, action);

        private void AddActualCycle(ushort address, byte value, string action) => this.ActualCycles.Add(new Cycle(address, value, action));

        private void DumpCycle(ushort address, byte value, string? action)
        {
            ArgumentNullException.ThrowIfNull(action);
            this.Messages.Add($"Address: {address}, value: {value}, action: {action}");
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
