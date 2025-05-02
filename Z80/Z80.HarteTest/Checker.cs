namespace Z80.HarteTest
{
    using EightBit;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class Checker
    {
        private TestRunner Runner { get; }

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
            this.Disassembler = new(this.Runner);
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

                this.Raise("PC", final.PC, cpu.PC);
                this.Raise("SP", final.SP, cpu.SP);

                this.Raise("A", final.A, cpu.A);
                this.Raise("F", final.F, cpu.F);
                this.Raise("B", final.B, cpu.B);
                this.Raise("C", final.C, cpu.C);
                this.Raise("D", final.D, cpu.D);
                this.Raise("E", final.E, cpu.E);
                this.Raise("H", final.H, cpu.H);
                this.Raise("L", final.L, cpu.L);

                cpu.Exx();
                cpu.ExxAF();

                this.Raise("'AF", final.AF_, cpu.AF);
                this.Raise("'BC", final.BC_, cpu.BC);
                this.Raise("'DE", final.DE_, cpu.DE);
                this.Raise("'HL", final.HL_, cpu.HL);

                this.Raise("I", final.I, cpu.IV);
                this.Raise("R", final.R, cpu.REFRESH);

                this.Raise("IM", final.IM, (ushort)cpu.IM);

                this.Raise("IFF1", final.IFF1, cpu.IFF1);
                this.Raise("IFF2", final.IFF2, cpu.IFF2);

                this.Raise("WZ", final.WZ, cpu.MEMPTR);

                this.Raise("IX", final.IX, cpu.IX);
                this.Raise("IY", final.IY, cpu.IY);

                if (test.Cycles is null)
                {
                    throw new InvalidOperationException("test cycles cannot be null");
                }

                this.Messages.Add($"Stepped cycles: {this.Cycles}, expected events: {test.Cycles.Count}, actual events: {this.ActualCycles.Count}");

                this.DumpCycles(test.AvailableCycles(), this.ActualCycles);
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

        private bool Check(string what, ushort expected, Register16 actual) => this.Check(what, expected, actual.Word);

        private bool Check(string what, ushort expected, ushort actual)
        {
            var success = actual == expected;
            if (!success)
            {
                this.Raise(what, expected, actual);
            }
            return success;
        }

        private bool Check(string what, byte expected, bool actual) => this.Check(what, expected, (byte)(actual ? 1 : 0));

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

        private string Disassemble(ushort address) => this.Disassembler.Disassemble(this.Runner.CPU, address);

        private bool CheckState(Test test)
        {
            var runner = this.Runner;
            var cpu = runner.CPU;

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

                var interestingCycleData = expectedCycle.Value is not null;
                if (interestingCycleData)
                {
                    var expectedAddress = expectedCycle.Address;
                    var actualAddress = actualCycle.Address;
                    _ = this.Check("Cycle address", expectedAddress, actualAddress);

                    var expectedValue = (byte)expectedCycle.Value;
                    var actualValue = (byte)actualCycle.Value;
                    _ = this.Check("Cycle value", expectedValue, actualValue);

                    var expectedAction = expectedCycle.Type;
                    var actualAction = actualCycle.Type;
                    _ = this.Check("Cycle action", expectedAction, actualAction);
                }
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
            var pc_good = this.Check("PC", final.PC, cpu.PC);
            var sp_good = this.Check("SP", final.SP, cpu.SP);

            var a_good = this.Check("A", final.A, cpu.A);
            var f_good = this.Check("F", final.F, cpu.F);
            var b_good = this.Check("B", final.B, cpu.B);
            var c_good = this.Check("C", final.C, cpu.C);
            var d_good = this.Check("D", final.D, cpu.D);
            var e_good = this.Check("E", final.E, cpu.E);
            var h_good = this.Check("H", final.H, cpu.H);
            var l_good = this.Check("L", final.L, cpu.L);

            cpu.Exx();
            cpu.ExxAF();

            var af_a_good = this.Check("'AF", final.AF_, cpu.AF);
            var bc_a_good = this.Check("'BC", final.BC_, cpu.BC);
            var de_a_good = this.Check("'DE", final.DE_, cpu.DE);
            var hl_a_good = this.Check("'HL", final.HL_, cpu.HL);

            var i_good = this.Check("I", final.I, cpu.IV);
            var r_good = this.Check("R", final.R, cpu.REFRESH);

            var im_good = this.Check("IM", final.IM, (byte)cpu.IM);

            var iff1_good = this.Check("IFF1", final.IFF1, cpu.IFF1);
            var iff2_good = this.Check("IFF2", final.IFF2, cpu.IFF2);

            var wz_good = this.Check("WZ", final.WZ, cpu.MEMPTR);

            var ix_good = this.Check("IX", final.IX, cpu.IX);
            var iy_good = this.Check("IY", final.IY, cpu.IY);

            if (!f_good)
            {
                this.Messages.Add($"Expected flags: {Disassembler.AsFlags(final.F)}");
                this.Messages.Add($"Actual flags  : {Disassembler.AsFlags(cpu.F)}");
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

                var ramGood = this.Check("RAM", address, value, runner.Peek(address));
                if (!ramGood && !ramProblem)
                {
                    ramProblem = true;
                }
            }

            return
                pc_good && sp_good
                && a_good && f_good
                && b_good && c_good
                && d_good && e_good
                && h_good && l_good
                && af_a_good
                && bc_a_good
                && de_a_good
                && hl_a_good
                && i_good && r_good
                && im_good
                && iff1_good && iff2_good
                && wz_good
                && ix_good && iy_good;
        }

        private void Raise(string what, byte expected, bool actual) => this.Raise(what, expected, (byte)(actual ? 1 : 0));

        private void Raise(string what, byte expected, byte actual) => this.Messages.Add($"{what}: expected: {expected:X2}, actual: {actual:X2}");

        private void Raise(string what, ushort expected, Register16 actual) => this.Raise(what, expected, actual.Word);

        private void Raise(string what, ushort expected, ushort actual) => this.Messages.Add($"{what}: expected: {expected:X4}, actual: {actual:X4}");

        private void Raise(string what, string expected, string actual) => this.Messages.Add($"{what}: expected: {expected}, actual: {actual}");

        public void Initialise()
        {
            this.Runner.CPU.Ticked += this.CPU_Ticked;
        }

        private void CPU_Ticked(object? sender, EventArgs e)
        {
            var read = this.Runner.CPU.RD == EightBit.PinLevel.Low ? "r" : "-";
            var write = this.Runner.CPU.WR == EightBit.PinLevel.Low ? "w" : "-";
            var memory = this.Runner.CPU.MREQ == EightBit.PinLevel.Low ? "m" : "-";
            var io = this.Runner.CPU.IORQ == EightBit.PinLevel.Low ? "i" : "-";
            this.AddActualCycle(this.Runner.Address, this.Runner.Data, $"{read}{write}{memory}{io}");
        }

        private void InitialiseState(Test test)
        {
            var initial = test.Initial ?? throw new InvalidOperationException("Test cannot have an invalid initial state");
            this.InitialiseState(initial);
        }

        private void InitialiseState(State state)
        {
            var runner = this.Runner;
            var cpu = runner.CPU;

            cpu.PC.Word = state.PC;
            cpu.SP.Word = state.SP;

            cpu.AF.Word = state.AF_;
            cpu.BC.Word = state.BC_;
            cpu.DE.Word = state.DE_;
            cpu.HL.Word = state.HL_;

            cpu.Exx();
            cpu.ExxAF();

            cpu.A = state.A;
            cpu.F = state.F;

            cpu.B = state.B;
            cpu.C = state.C;
            
            cpu.D = state.D;
            cpu.E = state.E;
            
            cpu.H = state.H;
            cpu.L = state.L;

            cpu.IV = state.I;
            cpu.REFRESH = state.R;

            cpu.IM = state.IM;

            cpu.IFF1 = state.IFF1 != 0;
            cpu.IFF2 = state.IFF2 != 0;

            cpu.MEMPTR.Word = state.WZ;

            cpu.IX.Word = state.IX;
            cpu.IY.Word = state.IY;

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
                runner.Poke(address, value);
            }
        }

        private void AddActualCycle(EightBit.Register16 address, byte value, string action) => this.AddActualCycle(address.Word, value, action);

        private void AddActualCycle(ushort address, byte value, string action) => this.ActualCycles.Add(new Cycle(address, value, action));

        private string ExpandCycle(string prefix, ushort address, byte? value, string? action)
        {
            ArgumentNullException.ThrowIfNull(action);
            return value is null
                ? $"{prefix}: Address: {address:X4},            action: {action}"
                : $"{prefix}: Address: {address:X4}, value: {value:X2}, action: {action}";
        }

        private string ExpandCycle(string prefix, Cycle cycle) => this.ExpandCycle(prefix, cycle.Address, cycle.Value, cycle.Type);

        private void DumpCycles(IEnumerable<Cycle> expected, IEnumerable<Cycle> actual)
        {
            List<Cycle> expectedCycles = [.. expected];
            List<Cycle> actualCycles = [.. actual];

            var until = Math.Max(expectedCycles.Count, actualCycles.Count);
            for (var i = 0; i < until; i++)
            {
                var expectedCycle = i < expectedCycles.Count ? expectedCycles[i] : null;
                var actualCycle = i < actualCycles.Count ? actualCycles[i] : null;
                var message = "";
                if (expectedCycle is not null)
                {
                    message += this.ExpandCycle("Expected", expectedCycle);
                    message += "    ";
                }
                if (actualCycle is not null)
                {
                    if ((expectedCycle is not null) && (expectedCycle.Value is null))
                    {
                        actualCycle.Value = null;
                    }
                    message += this.ExpandCycle("Actual  ", actualCycle);
                }
                Debug.Assert(!string.IsNullOrEmpty(message), "Message should not be empty");
                this.Messages.Add(message);
            }
        }   
    }
}
