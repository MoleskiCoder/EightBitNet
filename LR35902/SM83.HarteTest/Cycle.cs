using System.Diagnostics;

namespace SM83.HarteTest
{
    internal sealed class Cycle
    {
        public ushort Address { get; set; }

        public byte? Value { get; set; }

        public string Type { get; set; }

        public Cycle(ushort address, byte? value, string type)
        {
            this.Address = address;
            this.Value = value;
            this.Type = type;
        }

        public Cycle(List<object> input)
        {
            Debug.Assert(input is not null, "input cannot be null");
            ArgumentOutOfRangeException.ThrowIfNotEqual(input.Count, 3, nameof(input));
            this.Address = AsElement(input[0]).GetUInt16();
            this.Value = AsElement(input[1]).GetByte();

            var possibleType = AsElement(input[2]).GetString();
            Debug.Assert(possibleType != null);
            this.Type = possibleType;
        }

        private static System.Text.Json.JsonElement AsElement(object part) => (System.Text.Json.JsonElement)part;
    }
}
