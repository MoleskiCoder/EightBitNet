namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //scope id = 0, name = "", mod = 0, size = 1137, span = 355 + 354
            //scope id = 1, name = "stack", mod = 0, type = scope, size = 7, parent = 0, span = 15
            //scope id = 7, name = "print_box_break_vertical", mod = 0, type = scope, size = 6, parent = 0, sym = 33, span = 72
            [Section("scope", "Scopes")]
            public sealed class Scope(Parser container) : NamedSection(container)
            {
                [SectionReference("mod")]
                public Symbols.Module? Module { get; private set; }

                [SectionProperty("type")]
                public string? Type { get; private set; }

                [SectionProperty("size")]
                public int Size { get; private set; }

                [SectionReference("parent", optional: true)]
                public Scope? Parent { get; private set; }

                [SectionReference("sym", optional: true)]
                public Symbols.Symbol? Symbol { get; private set; }

                [SectionReferences("span")]
                public List<Span>? Spans { get; private set; }
            }
        }
    }
}