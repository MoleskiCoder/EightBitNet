namespace EightBit.Files.Symbols
{
    // line id = 268, file = 1, line = 60, type = 2, count = 1, span = 286 + 195
    [Section("line", "Lines")]
    public sealed class Line(Parser container) : IdentifiableSection(container)
    {
        [SectionReference("file")]
        public Symbols.File? File { get; private set; }

        [SectionProperty("line")]
        public int LineNumber { get; private set; }

        [SectionReference("type")]
        public Symbols.Type? Type { get; private set; }

        [SectionProperty("count")]
        public int? Count { get; private set; }

        [SectionReferences("span")]
        public List<Span>? Spans { get; private set; }
    }
}