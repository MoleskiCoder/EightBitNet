namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // line id = 268, file = 1, line = 60, type = 2, count = 1, span = 286 + 195
            public sealed class Line(Parser container) : IdentifiableSection(container)
            {
                [SectionReference("file")]
                public Symbols.File File => this.TakeFileReference();

                [SectionProperty("line")]
                public int LineNumber => this.TakeInteger("line");

                [SectionReference("type")]
                public Symbols.Type Type => this.TakeTypeReference();

                [SectionProperty("count")]
                public int? Count => this.MaybeTakeInteger("count");

                [SectionReferences("span")]
                public List<Span> Spans => this.TakeSpanReferences();
            }
        }
    }
}