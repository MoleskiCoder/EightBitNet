namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //scope id = 0, name = "", mod = 0, size = 1137, span = 355 + 354
            //scope id = 1, name = "stack", mod = 0, type = scope, size = 7, parent = 0, span = 15
            //scope id = 7, name = "print_box_break_vertical", mod = 0, type = scope, size = 6, parent = 0, sym = 33, span = 72
            public sealed class Scope(Parser container) : NamedSection(container)
            {
                [SectionReference("mod")]
                public Symbols.Module Module => this.TakeModuleReference();

                [SectionProperty("type")]
                public string? Type { get; private set; }

                [SectionProperty("size")]
                public int Size { get; private set; }

                [SectionReference("parent")]
                public Scope? Parent => this.MaybeTakeParentReference();

                private bool _symbolAvailable;
                private Symbols.Symbol? _symbol;

                [SectionReference("sym")]
                public Symbols.Symbol? Symbol
                {
                    get 
                    {
                        if (!this._symbolAvailable)
                        {
                            this._symbol = this.MaybeTakeSymbolReference();
                            this._symbolAvailable = true;
                        }
                        return this._symbol;
                    }
                }

                [SectionReferences("span")]
                public List<Span> Spans => this.TakeSpanReferences();
            }
        }
    }
}