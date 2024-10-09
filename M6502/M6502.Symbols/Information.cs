namespace M6502.Symbols
{
    //info csym = 0, file = 3, lib = 0, line = 380, mod = 1, scope = 12, seg = 8, span = 356, sym = 61, type = 3
    [M6502.Symbols.Section("info")]
    public sealed class Information(Parser container) : Section(container)
    {
        [M6502.Symbols.SectionProperty("csym")]
        public int CSymbol { get; private set; }

        [M6502.Symbols.SectionProperty("file")]
        public int File { get; private set; }

        [M6502.Symbols.SectionProperty("lib")]
        public int Library { get; private set; }

        [M6502.Symbols.SectionProperty("line")]
        public int Line { get; private set; }

        [M6502.Symbols.SectionProperty("mod")]
        public int Module { get; private set; }

        [M6502.Symbols.SectionProperty("scope")]
        public int Scope { get; private set; }

        [M6502.Symbols.SectionProperty("seg")]
        public int Segment { get; private set; }

        [M6502.Symbols.SectionProperty("span")]
        public int Span { get; private set; }

        [M6502.Symbols.SectionProperty("sym")]
        public int Symbol { get; private set; }

        [M6502.Symbols.SectionProperty("type")]
        public int Type { get; private set; }

        public int Count(string key) => this.GetValueT<int>(key);
    }
}