namespace EightBit.Files.Symbols
{
    // type id = 0, val = "800920"
    [Section("type", "Types")]
    public sealed class Type(Parser container) : IdentifiableSection(container)
    {
        [SectionProperty("val")]
        public string? Value { get; private set; }
    }
}