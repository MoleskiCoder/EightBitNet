namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // line id = 268, file = 1, line = 60, type = 2, count = 1, span = 286 + 195
            public class Line(Parser container) : IdentifiableSection(container)
            {
                [SectionReference("file")]
                public Symbols.File File => this.TakeFileReference();

                [SectionProperty("line")]
                public int LineNumber { get; private set; }

                [SectionReference("type")]
                public Symbols.Type Type => this.TakeTypeReference();

                [SectionProperty("count")]
                public int? Count { get; private set; }

                [SectionReferences("span")]
                public List<Span> Spans => this.TakeSpanReferences();

                public override void Parse(IDictionary<string, string> entries)
                {
                    base.Parse(entries);
                    this.LineNumber = this.TakeInteger("line");
                    this.Count = this.MaybeTakeInteger("count");
                }
            }
        }
    }
}