namespace M6502.HarteTest
{
    public class Cycle
    {
        public ushort Address { get; set; }

        public byte Value { get; set; }

        public string? Type { get; set; }

        public Cycle(ushort address, byte value, string type)
        {
            this.Address = address;
            this.Value = value;
            this.Type = type;
        }

        public Cycle(List<object> input)
        {
            if (input.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, "Cycles can only have three elements");
            }

            this.Address = ((System.Text.Json.JsonElement)input[0]).GetUInt16();
            this.Value = ((System.Text.Json.JsonElement)input[1]).GetByte();
            this.Type = ((System.Text.Json.JsonElement)input[2]).GetString();
        }
    }
}
