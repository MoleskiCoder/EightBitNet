namespace Z80.HarteTest
{
    using System.Diagnostics;

    // Cycle-by-cycle breakdown of bus activity
    [DebuggerDisplay("Cycle = Address:{Address}, Value:{Value}, Type:{Type}")]
    internal sealed class Cycle
    {
        public ushort Address { get; set; }

        public byte? Value { get; set; }

        // Type is a combination of "r(ead)", "w(rite)", "(m)emory" or "(i)o"
        public string Type { get; set; } = string.Empty;

        public Cycle(ushort address, byte? value, string type)
        {
            this.Address = address;
            this.Value = value;
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Cycle(List<object> input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Cycles can only have three elements");
            }

            this.Address = AsElement(input[0]).GetUInt16();
            if (input[1] is not null)
            {
                this.Value = AsElement(input[1]).GetByte();
            }

            this.Type = AsElement(input[2]).GetString() ?? throw new InvalidOperationException("Type cannot be null");
        }

        private static System.Text.Json.JsonElement AsElement(object part) => (System.Text.Json.JsonElement)part;
    }
}
