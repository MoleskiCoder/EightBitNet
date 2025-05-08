namespace Z80.HarteTest
{
    // Cycle-by-cycle breakdown of bus activity
    internal sealed class Port
    {
        public ushort Address { get; set; }

        public byte Value { get; set; }

        // Type can be one of "r(ead)" or "w(rite)"
        public string Type { get; set; } = string.Empty;

        public Port(ushort address, byte value, string type)
        {
            this.Address = address;
            this.Value = value;
            this.Type = type ?? string.Empty;
        }

        public Port(List<object> input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Ports can only have three elements");
            }

            this.Address = AsElement(input[0]).GetUInt16();
            this.Value = AsElement(input[1]).GetByte();
            this.Type = AsElement(input[2]).GetString() ?? string.Empty;
        }

        private static System.Text.Json.JsonElement AsElement(object part) => (System.Text.Json.JsonElement)part;
    }
}
