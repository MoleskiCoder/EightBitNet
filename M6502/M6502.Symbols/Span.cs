namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //span id = 351, seg = 7, start = 0, size = 2, type = 2
            public sealed class Span(Parser container) : IdentifiableSection(container)
            {
                [SectionReference("seg")]
                public Symbols.Segment Segment => this.TakeSegmentReference();

                [SectionProperty("start")]
                public int Start => this.TakeInteger("start");

                [SectionProperty("size")]
                public int Size => this.TakeInteger("size");

                [SectionReference("type")]
                public Symbols.Type Type => this.TakeTypeReference();
            }
        }
    }
}