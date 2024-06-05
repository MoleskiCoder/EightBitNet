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

                public int Size => this.TakeInteger("size");

                public Scope? Parent => this.MaybeTakeParentReference();

                public Symbols.Symbol? Symbol => this.MaybeTakeSymbolReference();

                public List<Span> Spans => this.TakeSpanReferences();

                public Scope()
                {
                    _ = this._integer_keys.Add("mod");
                    _ = this._enumeration_keys.Add("type");
                    _ = this._integer_keys.Add("size");
                    _ = this._integer_keys.Add("parent");
                    _ = this._multiple_keys.Add("sym");
                    _ = this._multiple_keys.Add("span");
                }
            }
        }
    }
}