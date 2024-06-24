namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //span id = 351, seg = 7, start = 0, size = 2, type = 2
            public class Span(Parser container) : IdentifiableSection(container)
            {
                [SectionReference("seg")]
                public Symbols.Segment Segment => this.TakeSegmentReference();

                [SectionProperty("start")]
                public int Start { get; private set; }

                [SectionProperty("size")]
                public int Size { get; private set; }

                [SectionReference("type")]
                public Symbols.Type Type => this.TakeTypeReference();

                public override void Parse(IDictionary<string, string> entries)
                {
                    base.Parse(entries);
                    this.Start = this.TakeInteger("start");
                    this.Size = this.TakeInteger("size");
                }
            }
        }
    }
}