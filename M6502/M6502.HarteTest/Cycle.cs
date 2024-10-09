namespace M6502.HarteTest
{
    public sealed class Cycle
    {
        public ushort Address { get; set; }

        public byte Value { get; set; }

        public string? Type { get; set; }

        public Cycle(ushort address, byte value, string type)
        {
            Address = address;
            Value = value;
            Type = type;
        }

        public Cycle(List<object> input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Cycles can only have three elements");
            }

            Address = AsElement(input[0]).GetUInt16();
            Value = AsElement(input[1]).GetByte();
            Type = AsElement(input[2]).GetString();
        }

        private static System.Text.Json.JsonElement AsElement(object part) => (System.Text.Json.JsonElement)part;
    }
}
