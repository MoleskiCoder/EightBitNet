namespace EightBit.Files.Symbols
{
    //span id = 351, seg = 7, start = 0, size = 2, type = 2
    [Section("span", "Spans")]
    public sealed class Span(Parser container) : IdentifiableSection(container)
    {
        [SectionReference("seg")]
        public Symbols.Segment? Segment { get; private set; }

        [SectionProperty("start")]
        public int Start { get; private set; }

        [SectionProperty("size")]
        public int Size { get; private set; }

        [SectionReference("type")]
        public Symbols.Type? Type { get; private set; }
    }
}