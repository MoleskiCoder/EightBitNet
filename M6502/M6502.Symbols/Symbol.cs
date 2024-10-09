namespace M6502.Symbols
{
    using System.Collections.Generic;

    // sym id = 16, name = "solve", addrsize = absolute, size = 274, scope = 0, def = 94,ref=144+17+351,val=0xF314,seg=6,type=lab
    [Section("symbol", "Symbols")]
    public sealed class Symbol(Parser container) : NamedSection(container)
    {
        [SectionEnumeration("addrsize")]
        public string? AddressSize { get; private set; }

        [SectionProperty("size")]
        public int? Size { get; private set; }

        [SectionReference("scope")]
        public Scope? Scope { get; private set; }

        [SectionReferences("def")]
        public List<Line>? Definitions { get; private set; }

        [SectionReferences("ref")]
        public List<Line>? References { get; private set; }

        [SectionProperty("val", hexadecimal: true)]
        public int Value { get; private set; }

        [SectionReference("seg")]
        public Symbols.Segment? Segment { get; private set; }

        [SectionEnumeration("type")]
        public string? Type { get; private set; }
    }
}