#define FAST_SYMBOL_LOOKUP
#define FAST_SIZE_LOOKUP

namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //scope id = 0, name = "", mod = 0, size = 1137, span = 355 + 354
            //scope id = 1, name = "stack", mod = 0, type = scope, size = 7, parent = 0, span = 15
            //scope id = 7, name = "print_box_break_vertical", mod = 0, type = scope, size = 6, parent = 0, sym = 33, span = 72
            public class Scope : NamedSection
            {
                public Symbols.Module Module => this.TakeModuleReference();

                public string? Type => this.MaybeTakeString("type");

#if FAST_SIZE_LOOKUP
                public int Size;
#else
                public int Size => this.TakeInteger("size");
#endif

                public Scope? Parent => this.MaybeTakeParentReference();

#if FAST_SYMBOL_LOOKUP
                private bool _symbolAvailable;
                private Symbols.Symbol? _symbol;
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
#else
                public Symbols.Symbol? Symbol => this.MaybeTakeSymbolReference();
#endif
                public List<Span> Spans => this.TakeSpanReferences();

                public Scope()
                {
                    _ = this._integer_keys.Add("mod");
                    _ = this._enumeration_keys.Add("type");
                    _ = this._integer_keys.Add("size");
                    _ = this._integer_keys.Add("parent");
                    _ = this._integer_keys.Add("sym");
                    _ = this._multiple_keys.Add("span");
                }

#if FAST_SIZE_LOOKUP
                public override void Parse(Parser parent, Dictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.Size = this.TakeInteger("size");
                }
#endif

            }
        }
    }
}